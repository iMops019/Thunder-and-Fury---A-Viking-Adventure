using UnityEngine;
using HarmonyLib;

namespace ValheimQoL.Patches
{
    // ---- Quick Slots for gear/potions ----
    //
    // Confirmed against the real 1.0 decompile:
    //  - Humanoid.UseItem(Inventory, ItemDrop.ItemData, bool fromInventoryGui)
    //    is the same dispatch vanilla itself uses when you click an
    //    inventory item — it already routes to ConsumeItem for
    //    potions/food or ToggleEquipped for gear/tools, so quick slots
    //    don't need to reimplement equip-vs-consume logic, just call it.
    //  - Inventory.GetItem(string name, ...) looks an item up by its
    //    shared name, so a slot only needs to remember a name string, not
    //    a live item reference.
    //  - Player.m_customData (public Dictionary<string,string>) is already
    //    saved/loaded with the character, so it's a ready-made place to
    //    persist slot assignments without inventing new save data.
    //  - InventoryGui.m_dragItem holds whatever item the player currently
    //    has picked up in the inventory screen (set by SetupDragItem, the
    //    same method vanilla's own drag-and-drop uses) — used here as
    //    "the item to assign" when binding a slot. Private on the real
    //    assembly (confirmed via decompile 2026-09-10, after catching it
    //    live as a FieldAccessException) — read via Harmony's Traverse
    //    below, not a direct field access.
    //
    // Workflow: in the inventory screen, left-click an item to pick it up
    // (vanilla's own drag state), then press Ctrl+<slot key> to assign it,
    // then click it back down to put it away. Press the slot key alone,
    // any time, to use/equip whatever's assigned to it.
    //
    // First cut — the assign UX (drag-then-Ctrl+key) is a reasonable read
    // of the vanilla drag state but hasn't been felt out in-game yet;
    // revisit once actually tested Friday.
    [HarmonyPatch(typeof(Player), nameof(Player.Update))]
    public static class QuickSlotsHotkeyPatch
    {
        const string DataKeyPrefix = "QuickSlot_";

        static void Postfix(Player __instance)
        {
            if (__instance != Player.m_localPlayer) return;

            var slots = ValheimQoLPlugin.QuickSlotKeys;
            bool assigning = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

            for (int i = 0; i < slots.Length; i++)
            {
                if (!slots[i].Value.IsDown()) continue;

                string dataKey = DataKeyPrefix + i;

                if (assigning)
                {
                    // InventoryGui.m_dragItem is private on the real (non-
                    // publicized) game assembly -- confirmed via decompile
                    // 2026-09-10, found live in-game as a FieldAccessException
                    // spamming every frame. No public getter exists for it,
                    // so read it via Harmony's own Traverse instead of a
                    // direct field access.
                    ItemDrop.ItemData dragItem = InventoryGui.instance != null
                        ? Traverse.Create(InventoryGui.instance).Field<ItemDrop.ItemData>("m_dragItem").Value
                        : null;
                    if (dragItem == null) continue;

                    __instance.m_customData[dataKey] = dragItem.m_shared.m_name;
                    __instance.Message(MessageHud.MessageType.Center, "Quick slot " + (i + 1) + ": " + dragItem.m_shared.m_name);
                    continue;
                }

                if (!__instance.m_customData.TryGetValue(dataKey, out var itemName) || string.IsNullOrEmpty(itemName))
                    continue;

                // __instance.m_inventory is protected on Humanoid --
                // compiled fine against the local publicized reference
                // but threw FieldAccessException at runtime (confirmed
                // in-game 2026-09-10). GetInventory() is the real public
                // accessor for the same object.
                Inventory inventory = __instance.GetInventory();
                var item = inventory.GetItem(itemName);
                if (item == null) continue;

                __instance.UseItem(inventory, item, fromInventoryGui: true);
            }
        }
    }
}
