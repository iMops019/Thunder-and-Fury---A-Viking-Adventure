using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace ValheimQoL.Patches
{
    // ---- Auto-pickup range / Auto-sort ----
    //
    // Confirmed against the real 1.0 decompile: vanilla already HAS
    // auto-pickup (Player.AutoPickup, toggled by the "AutoPickup" input
    // action, sweeping Player.m_autoPickupRange each frame). There's
    // nothing to build there — just widen the range vanilla already uses,
    // the same constructor-Postfix trick as BuildingSnap's placement
    // distance.
    //
    // Auto-SORT has no vanilla equivalent (InventoryGui only sorts the
    // crafting recipe list, not the inventory grid), so that part is a
    // real implementation: merge partial stacks, then re-lay the grid out
    // by item type/name/quality.

    // Widens how far away the vanilla auto-pickup sweep reaches.
    [HarmonyPatch(typeof(Player), MethodType.Constructor)]
    public static class AutoPickupRangePatch
    {
        static void Postfix(Player __instance)
        {
            __instance.m_autoPickupRange *= ValheimQoLPlugin.AutoPickupRangeMultiplier.Value;
        }
    }

    // Hotkey (only while the inventory screen is open) that sorts and
    // stack-merges the local player's inventory grid.
    [HarmonyPatch(typeof(Player), nameof(Player.Update))]
    public static class AutoSortHotkeyPatch
    {
        static void Postfix(Player __instance)
        {
            if (__instance != Player.m_localPlayer) return;
            if (!InventoryGui.IsVisible()) return;
            if (!ValheimQoLPlugin.SortInventoryKey.Value.IsDown()) return;

            InventorySorter.Sort(__instance.GetInventory());
        }
    }

    public static class InventorySorter
    {
        public static void Sort(Inventory inventory)
        {
            MergeStacks(inventory);

            // Inventory.m_inventory (the raw list) and Inventory.Changed()
            // are both private on the real assembly -- compiled fine
            // against the local publicized reference but threw at
            // runtime (confirmed in-game 2026-09-10). GetAllItems()
            // returns the SAME live list (confirmed via decompile: it's
            // a plain `return m_inventory;`), so mutating its contents
            // still mutates the real inventory. m_onChanged is the real
            // public hook Changed() itself invokes internally to notify
            // the UI -- calling it directly gets the same refresh without
            // needing the private method.
            var items = inventory.GetAllItems()
                .OrderBy(i => (int)i.m_shared.m_itemType)
                .ThenBy(i => i.m_shared.m_name)
                .ThenByDescending(i => i.m_quality)
                .ToList();

            int width = inventory.GetWidth();
            for (int idx = 0; idx < items.Count; idx++)
            {
                items[idx].m_gridPos = new Vector2i(idx % width, idx / width);
            }

            inventory.m_onChanged?.Invoke();
        }

        // Same-name/quality/worldLevel stacks get combined into as few
        // slots as possible, up to each item's own max stack size.
        static void MergeStacks(Inventory inventory)
        {
            var groups = inventory.GetAllItems()
                .GroupBy(i => (i.m_shared.m_name, i.m_quality, i.m_worldLevel));

            foreach (var group in groups)
            {
                var stacks = group.ToList();
                if (stacks.Count < 2) continue;

                int maxStack = stacks[0].m_shared.m_maxStackSize;
                int remaining = stacks.Sum(i => i.m_stack);

                foreach (var itemData in stacks)
                {
                    if (remaining <= 0)
                    {
                        inventory.GetAllItems().Remove(itemData);
                        continue;
                    }

                    int give = Math.Min(remaining, maxStack);
                    itemData.m_stack = give;
                    remaining -= give;
                }
            }
        }
    }
}
