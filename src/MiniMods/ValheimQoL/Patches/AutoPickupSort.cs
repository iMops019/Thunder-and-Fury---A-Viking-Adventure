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

            InventorySorter.Sort(__instance.m_inventory);
        }
    }

    public static class InventorySorter
    {
        public static void Sort(Inventory inventory)
        {
            MergeStacks(inventory);

            var items = inventory.m_inventory
                .OrderBy(i => (int)i.m_shared.m_itemType)
                .ThenBy(i => i.m_shared.m_name)
                .ThenByDescending(i => i.m_quality)
                .ToList();

            int width = inventory.m_width;
            for (int idx = 0; idx < items.Count; idx++)
            {
                items[idx].m_gridPos = new Vector2i(idx % width, idx / width);
            }

            inventory.Changed();
        }

        // Same-name/quality/worldLevel stacks get combined into as few
        // slots as possible, up to each item's own max stack size.
        static void MergeStacks(Inventory inventory)
        {
            var groups = inventory.m_inventory
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
                        inventory.m_inventory.Remove(itemData);
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
