using Jotunn.Managers;
using UnityEngine;
using ThunderFury.Core.Quests;
using ThunderFury.Core.SkillSystem;

namespace ThunderFury.Quests.Data
{
    // ---- Per-player quest status/progress ----
    //
    // Stored in Player.m_customData -- the same already-saved,
    // already-synced field QuickSlots (ValheimQoL) and Voltun's Hatchet's
    // fixed identity bonus both already use for per-instance/per-player
    // data, so this doesn't invent new save data.
    public enum QuestStatus
    {
        Locked,
        Active,
        Complete,
    }

    public static class QuestPlayerState
    {
        const string StatusKeyPrefix = "Quest_Status_";
        const string ProgressKeyPrefix = "Quest_Progress_";
        const string VisitedBiomeKeyPrefix = "Quest_VisitedBiome_";

        // Own lightweight "has this player ever stood in this biome"
        // tracking, deliberately NOT using vanilla's own
        // Player.IsBiomeKnown/AddKnownBiome -- confirmed in-game
        // (2026-09-10) those are mid-churn on the real (post-1.0-hotfix)
        // game build (AddKnownBiome went private and switched to a new
        // per-zone BiomeSector type, not the plain Heightmap.Biome enum
        // this was first written against a few hours earlier the same
        // session). Player.GetCurrentBiome() is a small, stable, public
        // surface that hasn't moved, so building on that directly avoids
        // riding vanilla's own internal churn for something this
        // quest-only feature doesn't need the full fidelity of.
        public static bool HasVisitedBiome(Player player, string biomeName)
        {
            return player.m_customData.TryGetValue(VisitedBiomeKeyPrefix + biomeName, out string raw) && raw == "1";
        }

        public static void MarkBiomeVisited(Player player, string biomeName)
        {
            player.m_customData[VisitedBiomeKeyPrefix + biomeName] = "1";
        }

        public static QuestStatus GetStatus(Player player, string questId)
        {
            if (player.m_customData.TryGetValue(StatusKeyPrefix + questId, out string raw) &&
                System.Enum.TryParse(raw, out QuestStatus status))
            {
                return status;
            }
            return QuestStatus.Locked;
        }

        public static void SetStatus(Player player, string questId, QuestStatus status)
        {
            player.m_customData[StatusKeyPrefix + questId] = status.ToString();
        }

        public static int GetProgress(Player player, string questId)
        {
            if (player.m_customData.TryGetValue(ProgressKeyPrefix + questId, out string raw) &&
                int.TryParse(raw, out int value))
            {
                return value;
            }
            return 0;
        }

        public static void SetProgress(Player player, string questId, int value)
        {
            player.m_customData[ProgressKeyPrefix + questId] = value.ToString();
        }

        // Called once, the first time a player interacts with a built
        // Adventure Board -- starts the chain from its first quest, no
        // manual "accept" step (matches "once built, players start
        // getting quests").
        public static void EnsureChainStarted(Player player)
        {
            if (QuestRegistry.All.Count == 0) return;

            QuestDefinition first = QuestRegistry.All[0];
            if (GetStatus(player, first.Id) == QuestStatus.Locked)
            {
                SetStatus(player, first.Id, QuestStatus.Active);
            }
        }

        public static void CompleteAndAdvance(Player player, QuestDefinition quest)
        {
            SetStatus(player, quest.Id, QuestStatus.Complete);
            GrantReward(player, quest);

            player.Message(MessageHud.MessageType.Center, $"Quest complete: {quest.Title}");

            if (!string.IsNullOrEmpty(quest.NextQuestId))
            {
                SetStatus(player, quest.NextQuestId, QuestStatus.Active);
                QuestDefinition next = QuestRegistry.Get(quest.NextQuestId);
                if (next != null)
                {
                    player.Message(MessageHud.MessageType.Center, $"New quest: {next.Title}");
                }
            }
        }

        static void GrantReward(Player player, QuestDefinition quest)
        {
            if (!string.IsNullOrEmpty(quest.RewardItemName) && quest.RewardItemAmount > 0)
            {
                GameObject prefab = PrefabManager.Instance.GetPrefab(quest.RewardItemName);
                if (prefab != null)
                {
                    player.GetInventory().AddItem(prefab, quest.RewardItemAmount);
                }
                else
                {
                    Jotunn.Logger.LogWarning($"Quests: could not resolve reward item prefab '{quest.RewardItemName}' for quest '{quest.Id}'");
                }
            }

            if (!string.IsNullOrEmpty(quest.RewardSkillName) && quest.RewardSkillXp > 0 &&
                SkillRegistry.TryGet(quest.RewardSkillName, out global::Skills.SkillType type))
            {
                player.GetSkills().RaiseSkill(type, quest.RewardSkillXp);
            }
        }

        // Whether this quest is currently reachable at all -- Active
        // already, or Locked but its area-level gate (RequiredBiome) and
        // prerequisite are satisfied. Used by the board UI to decide what
        // to show/offer.
        public static bool CanShowAsCurrent(Player player, QuestDefinition quest)
        {
            QuestStatus status = GetStatus(player, quest.Id);
            if (status == QuestStatus.Complete) return false;
            if (status == QuestStatus.Active) return true;

            if (string.IsNullOrEmpty(quest.RequiredBiome)) return true;
            return HasVisitedBiome(player, quest.RequiredBiome);
        }
    }
}
