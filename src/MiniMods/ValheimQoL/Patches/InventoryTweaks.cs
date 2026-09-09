using HarmonyLib;

namespace ValheimQoL.Patches
{
    // Applies weight and stack-size multipliers to every item's shared
    // data once the game's item database has loaded.
    [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
    public static class InventoryTweaksPatch
    {
        static void Postfix(ObjectDB __instance)
        {
            if (__instance.m_items == null || __instance.m_items.Count == 0)
                return;

            foreach (var prefab in __instance.m_items)
            {
                var itemDrop = prefab.GetComponent<ItemDrop>();
                if (itemDrop == null) continue;

                var shared = itemDrop.m_itemData.m_shared;

                // Only touch raw materials, not weapons/armor/food — rough
                // heuristic using item type. Worth double-checking the
                // m_itemType enum values still mean the same thing in 1.0.
                if (shared.m_itemType == ItemDrop.ItemData.ItemType.Material)
                {
                    shared.m_weight *= ValheimQoLPlugin.MaterialWeightMultiplier.Value;
                }

                shared.m_maxStackSize *= ValheimQoLPlugin.StackSizeMultiplier.Value;
            }
        }
    }
}
