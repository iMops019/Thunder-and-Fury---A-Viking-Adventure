using System.Collections.Generic;
using HarmonyLib;
using VikingAdventure.RarityLoot.Affixes;

namespace VikingAdventure.RarityLoot.Patches
{
    // Confirmed against the real 1.0 decompile: Inventory.AddItem clones
    // its source ItemData (ItemDrop.ItemData.Clone()) to create the
    // instance that actually lands in an inventory -- true whether that
    // item came from crafting, looting a container, or a ground pickup.
    // Patching Clone() itself means one hook covers every acquisition
    // method a tracked item could use, present or future, without
    // duplicating roll logic per acquisition path.
    //
    // Tracked by SharedData reference rather than prefab/display name --
    // SharedData is the same object shared by every instance of one item
    // type, so reference equality is a robust, string-mismatch-proof way
    // to ask "is this one of ours" without needing the prefab's internal
    // GameObject name at roll time.
    public static class ItemRollTrigger
    {
        static readonly Dictionary<ItemDrop.ItemData.SharedData, (RarityTier Tier, int AffixCount)> TrackedItems =
            new Dictionary<ItemDrop.ItemData.SharedData, (RarityTier, int)>();

        public static void Register(ItemDrop.ItemData.SharedData shared, RarityTier tier, int affixCount)
        {
            TrackedItems[shared] = (tier, affixCount);
        }

        public static bool TryGetRollSpec(ItemDrop.ItemData.SharedData shared, out RarityTier tier, out int affixCount)
        {
            if (TrackedItems.TryGetValue(shared, out (RarityTier Tier, int AffixCount) entry))
            {
                tier = entry.Tier;
                affixCount = entry.AffixCount;
                return true;
            }

            tier = RarityTier.Normal;
            affixCount = 0;
            return false;
        }
    }

    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.Clone))]
    public static class ItemRollTriggerPatch
    {
        static void Postfix(ItemDrop.ItemData __result)
        {
            if (__result?.m_shared == null) return;
            if (ItemRoller.IsRolled(__result)) return;
            if (!ItemRollTrigger.TryGetRollSpec(__result.m_shared, out RarityTier tier, out int affixCount)) return;

            ItemRoller.Roll(__result, tier, affixCount);
        }
    }
}
