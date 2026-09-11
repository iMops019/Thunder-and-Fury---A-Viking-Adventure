using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using ThunderFury.Quests.UI;

namespace ThunderFury.Quests
{
    // ---- Adventure Board (buildable piece) ----
    //
    // Replaces the original "quest-giver NPC" idea (this session's
    // design conversation) -- a placeable object on the Hammer's own
    // piece table instead of a cloned/AI-stripped creature, avoiding the
    // real technical/visual risk of turning a boss-only creature into a
    // passive dialogue NPC. Once built, interacting with it starts the
    // quest chain automatically (QuestPlayerState.EnsureChainStarted) --
    // no separate "accept quest" step.
    //
    // "piece_sign" turned out to be wrong -- confirmed live in-game
    // 2026-09-10 (Jotunn: "can not find base prefab with name: piece_sign",
    // this piece failing registration entirely). A live ZNetScene dump
    // found the real name is just "sign" (no "piece_" prefix; there's
    // also a blank "sign_notext" variant, not used here since a readable
    // board is the whole point).
    public static class AdventureBoard
    {
        public const string PrefabName = "AdventureBoard";

        public static void Register()
        {
            var config = new PieceConfig
            {
                Name = "Adventure Board",
                Description = "A weathered board where word travels of deeds worth doing.",
                PieceTable = "_HammerPieceTable",
                Category = "Misc",
                Requirements = new[]
                {
                    new RequirementConfig("Wood", QuestsPlugin.AdventureBoardWoodCost.Value),
                },
            };

            var customPiece = new CustomPiece(PrefabName, "sign", config);
            if (!customPiece.IsValid())
            {
                Jotunn.Logger.LogError("AdventureBoard piece is not valid, skipping registration");
                return;
            }

            customPiece.PiecePrefab.AddComponent<AdventureBoardInteractable>();

            PieceManager.Instance.AddPiece(customPiece);
        }
    }

    public class AdventureBoardInteractable : MonoBehaviour, Interactable, Hoverable
    {
        public string GetHoverText() => "[E] Read the Adventure Board";
        public string GetHoverName() => "Adventure Board";

        // Hoverable gained this in a post-1.0 patch (confirmed 2026-09-10
        // against a freshly regenerated publicized assembly) -- 0 means
        // no extra vertical offset on the hover tooltip, same as vanilla
        // Pickable-style objects with nothing special about their hover
        // anchor point.
        public float GetHoverOffset() => 0f;

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold) return false;
            if (!(user is Player player)) return false;

            QuestBoardPanel.Open(player);
            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;
    }
}
