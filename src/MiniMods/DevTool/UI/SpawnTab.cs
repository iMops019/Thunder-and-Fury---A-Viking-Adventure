using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;
using static ThunderFury.DevTool.UI.DevToolUiHelpers;

namespace ThunderFury.DevTool.UI
{
    // ---- Spawn tab: give yourself any item straight to your inventory ----
    //
    // User's own request (2026-09-10): testing a specific item (a Knife,
    // the Lightning Sword) shouldn't require finding/crafting/killing for
    // it first. Same searchable item picker every other tab already uses
    // (works for vanilla items AND anything DevTool/RarityLoot registers
    // via ItemManager.AddItem, since Jotunn adds those into the same
    // ObjectDB.m_items list VanillaItemCatalog reads) -- so "Lightning
    // Sword" shows up here exactly like any vanilla item, no special
    // case needed.
    //
    // Inventory.AddItem(GameObject, int) is the same confirmed-public
    // overload DungeonLootPatch.cs already uses to add loot straight to a
    // container's inventory -- same call, just targeting the local
    // player's own inventory instead.
    public class SpawnTab : DevToolOverlay.ITab
    {
        public string Title => "Spawn";

        InputField _itemField;
        InputField _amountField;

        public void Build(Transform contentParent)
        {
            GUIManager gui = GUIManager.Instance;

            TextRow(gui, contentParent, "Spawn Item", true);
            TextRow(gui, contentParent,
                "Gives the item straight to your inventory -- no crafting, no drop chance. Works for vanilla items and anything " +
                "this mod has registered (Lightning Sword, Voltun's Set, etc).",
                false, 40f);

            _itemField = FieldRowWithPicker(gui, contentParent, "Item", "");
            _amountField = FieldRow(gui, contentParent, "Amount", "1");

            GameObject spawnButton = Button(gui, contentParent, "Spawn");
            spawnButton.GetComponent<Button>().onClick.AddListener(SpawnItem);
        }

        void SpawnItem()
        {
            string itemName = _itemField.text.Trim();
            if (string.IsNullOrEmpty(itemName))
            {
                Jotunn.Logger.LogWarning("DevTool Spawn: an Item is required");
                return;
            }

            if (Player.m_localPlayer == null)
            {
                Jotunn.Logger.LogWarning("DevTool Spawn: no local player (are you in a world?)");
                return;
            }

            GameObject prefab = PrefabManager.Instance.GetPrefab(itemName);
            if (prefab == null)
            {
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, $"'{itemName}' not found -- pick one from the list above.");
                return;
            }

            int amount = ParseInt(_amountField.text, 1);
            if (amount < 1) amount = 1;

            bool added = Player.m_localPlayer.GetInventory().AddItem(prefab, amount);
            Player.m_localPlayer.Message(MessageHud.MessageType.Center,
                added ? $"Spawned {amount}x {itemName}." : $"Inventory full -- couldn't spawn {itemName}.");
        }
    }
}
