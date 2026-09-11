using System;
using System.Collections.Generic;
using HarmonyLib;
using ThunderFury.RarityLoot.Affixes;

namespace ThunderFury.RarityLoot.Patches
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

    // ---- Ordinary vanilla gear: random Magic/Rare rolls ----
    //
    // Closes vision.md's own long-open question ("which items are
    // eligible to roll Magic/Rare, and at what odds") without inventing a
    // second mechanism: same Clone()-Postfix hook as the fixed-tier path
    // above (crafting, looting, and dropping all clone through
    // ItemData.Clone(), confirmed when the affix framework was first
    // built), same ItemRoller.Roll() call, same shared AffixPool. Only
    // new part is deciding eligibility and odds, both content/balance
    // calls vision.md deliberately left open:
    //  - Eligibility: any weapon or armor-slot item, full stop -- reuses
    //    ItemRoller's own Weapon/Armor classification (item.IsWeapon(),
    //    IsArmorSlot) instead of a hand-maintained item-name list, so
    //    this covers every vanilla weapon/armor piece in the game (and
    //    any future one) automatically, matching how the rest of this
    //    codebase prefers a generic type check over a name list wherever
    //    one exists.
    //  - Odds: flat, config-tunable chance per eligible item creation,
    //    checked once (rarer tier first so its odds aren't shadowed by
    //    the more common one). Legendary is deliberately excluded from
    //    this random pool -- vision.md frames Legendary as
    //    named/hand-crafted (Voltun's Set), never randomly rolled onto an
    //    ordinary item, so only Magic and Rare are reachable here.
    //  - Deliberately NOT built this pass, flagged as a good follow-up:
    //    scaling craft-time odds by the crafting player's Smithing level
    //    (a natural fit for vision.md's "gate rewards" progression
    //    principle) -- doing that correctly needs a second, earlier hook
    //    point than this generic Clone() Postfix (to know who's crafting
    //    and prevent a double-roll), which is real additional work, not
    //    a one-line addition.
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.Clone))]
    public static class ItemRollTriggerPatch
    {
        static readonly Random Rng = new Random();

        static void Postfix(ItemDrop.ItemData __result)
        {
            if (__result?.m_shared == null) return;
            if (ItemRoller.IsRolled(__result)) return;

            if (ItemRollTrigger.TryGetRollSpec(__result.m_shared, out RarityTier fixedTier, out int fixedAffixCount))
            {
                ItemRoller.Roll(__result, fixedTier, fixedAffixCount);
                return;
            }

            if (TryRollOrdinaryGear(__result, out RarityTier randomTier, out int randomAffixCount))
            {
                ItemRoller.Roll(__result, randomTier, randomAffixCount);
            }
        }

        static bool TryRollOrdinaryGear(ItemDrop.ItemData item, out RarityTier tier, out int affixCount)
        {
            tier = RarityTier.Normal;
            affixCount = 0;

            bool eligible = item.IsWeapon() || ItemRoller.IsArmorSlot(item.m_shared.m_itemType);
            if (!eligible) return false;

            double roll = Rng.NextDouble();
            double rareChance = RarityLootPlugin.OrdinaryRareChance.Value;
            double magicChance = RarityLootPlugin.OrdinaryMagicChance.Value;

            if (roll < rareChance)
            {
                tier = RarityTier.Rare;
                affixCount = RarityLootPlugin.OrdinaryRareAffixCount.Value;
                return true;
            }

            if (roll < rareChance + magicChance)
            {
                tier = RarityTier.Magic;
                affixCount = RarityLootPlugin.OrdinaryMagicAffixCount.Value;
                return true;
            }

            return false;
        }
    }
}
