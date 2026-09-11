using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;
using ThunderFury.Core.Quests;
using ThunderFury.Quests.Data;
using ThunderFury.RarityLoot.Affixes;

namespace ThunderFury.Quests.UI
{
    // ---- Adventure Board's quest panel ----
    //
    // Built on Jotunn's GUIManager, same Valheim-style toolkit the Dev
    // Tool overlay uses -- can't reuse DevTool's own UI helpers directly
    // (Quests doesn't depend on DevTool, and shouldn't: DevTool is a
    // player-facing content EDITOR, unrelated to what a quest board shows
    // a player), so this is its own small, self-contained panel rather
    // than a shared library extracted for one caller.
    //
    // Deliberately simple: no Accept/Decline buttons. The chain
    // auto-starts on first interact and auto-advances on completion
    // (matches "once built, players start getting quests" -- no manual
    // step to fake or skip).
    public static class QuestBoardPanel
    {
        static GameObject _panel;
        static Text _titleText;
        static Text _descriptionText;
        static Text _progressText;

        public static void Open(Player player)
        {
            QuestPlayerState.EnsureChainStarted(player);
            CheckLegendaryObjective(player);

            if (_panel == null)
            {
                if (GUIManager.CustomGUIFront == null) return;
                Build();
            }

            Refresh(player);
            _panel.SetActive(true);
            GUIManager.BlockInput(true);
        }

        static void Close()
        {
            if (_panel == null) return;
            _panel.SetActive(false);
            GUIManager.BlockInput(false);
        }

        static void Build()
        {
            GUIManager gui = GUIManager.Instance;
            Transform root = GUIManager.CustomGUIFront.transform;

            _panel = gui.CreateWoodpanel(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, 500f, 320f);
            _panel.SetActive(false);

            _titleText = gui.CreateText("", _panel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -30f), gui.AveriaSerifBold, 22, gui.ValheimOrange, true, Color.black,
                440f, 30f, false).GetComponent<Text>();

            _descriptionText = gui.CreateText("", _panel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -80f), gui.AveriaSerif, 16, Color.white, true, Color.black,
                440f, 100f, false).GetComponent<Text>();
            _descriptionText.verticalOverflow = VerticalWrapMode.Overflow;

            _progressText = gui.CreateText("", _panel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -200f), gui.AveriaSerifBold, 16, gui.ValheimYellow, true, Color.black,
                440f, 30f, false).GetComponent<Text>();

            GameObject closeButton = gui.CreateButton("Close", _panel.transform,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), 140f, 36f);
            closeButton.GetComponent<Button>().onClick.AddListener(Close);
        }

        static void Refresh(Player player)
        {
            QuestDefinition current = FindCurrent(player);
            if (current == null)
            {
                _titleText.text = "Adventure Board";
                _descriptionText.text = "No further word of deeds worth doing -- for now.";
                _progressText.text = "";
                return;
            }

            _titleText.text = current.Title;
            _descriptionText.text = current.Description;

            switch (current.ObjectiveType)
            {
                case QuestObjectiveType.KillCreature:
                    _progressText.text = $"Progress: {QuestPlayerState.GetProgress(player, current.Id)}/{current.TargetCount}";
                    break;
                case QuestObjectiveType.DiscoverBiome:
                    _progressText.text = "Not yet discovered.";
                    break;
                case QuestObjectiveType.OwnLegendaryItem:
                    _progressText.text = "Not yet fulfilled.";
                    break;
            }
        }

        static QuestDefinition FindCurrent(Player player)
        {
            foreach (QuestDefinition quest in QuestRegistry.All)
            {
                if (QuestPlayerState.GetStatus(player, quest.Id) == QuestStatus.Active)
                {
                    return quest;
                }
            }
            return null;
        }

        // OwnLegendaryItem has no live event to hook -- it's a standing
        // question about current inventory state, so it's checked
        // whenever the board is opened rather than via a Harmony patch.
        static void CheckLegendaryObjective(Player player)
        {
            // player.m_inventory is protected on Humanoid -- compiles
            // against the local publicized reference but throws
            // FieldAccessException at runtime against the real assembly
            // (confirmed in-game 2026-09-10). GetInventory() is the real
            // public accessor.
            foreach (ItemDrop.ItemData item in player.GetInventory().GetAllItems())
            {
                if (ItemRoller.GetTier(item) != RarityTier.Legendary) continue;

                foreach (QuestDefinition quest in QuestRegistry.All)
                {
                    if (quest.ObjectiveType != QuestObjectiveType.OwnLegendaryItem) continue;
                    if (QuestPlayerState.GetStatus(player, quest.Id) != QuestStatus.Active) continue;

                    QuestPlayerState.CompleteAndAdvance(player, quest);
                }
                return;
            }
        }
    }
}
