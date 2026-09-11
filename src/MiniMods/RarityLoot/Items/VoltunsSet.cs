using System.Collections.Generic;
using System.Globalization;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using ThunderFury.RarityLoot.Affixes;
using ThunderFury.RarityLoot.Patches;

namespace ThunderFury.RarityLoot.Items
{
    // ---- Voltun's Set (Hatchet + Pickaxe) ----
    //
    // docs/valheim-mod-vision.md: a named-hero Legendary set, findable in
    // Black Forest, stacking multiple bonus effects rather than one flat
    // stat bump. One thing about the design doc's exact bonus list
    // ("+50% log yield, extra tree damage, faster chop, double XP") is
    // still scoped down, flagged rather than faked: double XP needs
    // Core's custom skill system, which doesn't exist yet (still a stub
    // plugin) -- deferred, pairs with that work.
    // What's implemented: extra chop/mining damage, faster swing speed,
    // and now log yield (LogYieldPatch.cs -- researched this session:
    // TreeLog.Destroy is where fallen logs turn into Wood items). All
    // scaled relative to whatever the real cloned base item's values
    // turn out to be at runtime (same pattern as StonePickaxe). Both
    // items also roll Legendary-tier affixes from the shared AffixPool on
    // top of their fixed identity bonuses, so no two Voltun's items are
    // identical.
    //
    // Acquisition: crafted from Wood + Copper + Bronze at the Forge --
    // spans Meadows (Wood) and Black Forest (Copper, Bronze -- Bronze
    // itself already gates on Copper+Tin+Coal, so it's a meaningfully
    // "grind for it" ingredient) per this session's direction. Vision.md
    // had left Voltun's Set's acquisition method as an open question
    // (recipe vs. drop chance) -- this picks recipe-based for now since
    // that's what got the concrete discussion; swapping to a boss-drop
    // model later doesn't require touching the affix system, just where
    // ItemRollTrigger.Register happens.
    //
    // "Hatchet" turned out to be wrong post-1.0 -- confirmed live in-game
    // 2026-09-10 (Jotunn: "can not find base prefab with name: Hatchet",
    // this item failing registration entirely). 1.0 replaced the old
    // single starting axe with a two-tier Stone Age progression
    // (AxeStone -> AxeFlint, confirmed via a live ObjectDB dump plus the
    // player's own inventory tooltip reading "Stone Axe" for their
    // starting weapon) -- fixed to clone AxeStone, the actual modern
    // equivalent of the old free starting hatchet.
    public static class VoltunsSet
    {
        public static void Register()
        {
            RegisterHatchet();
            RegisterPickaxe();
        }

        static void RegisterHatchet()
        {
            var config = new ItemConfig
            {
                Name = "Voltun's Hatchet",
                Description = "A woodsman's legend given a blade. Hits harder and faster than any axe of its tier.",
                CraftingStation = CraftingStations.Forge,
                Requirements = new[]
                {
                    new RequirementConfig("Wood", RarityLootPlugin.VoltunHatchetWoodCost.Value),
                    new RequirementConfig("Copper", RarityLootPlugin.VoltunHatchetCopperCost.Value),
                    new RequirementConfig("Bronze", RarityLootPlugin.VoltunHatchetBronzeCost.Value),
                },
            };

            var hatchet = new CustomItem("VoltunsHatchet", "AxeStone", config);
            if (!hatchet.IsValid())
            {
                Jotunn.Logger.LogError("VoltunsHatchet item is not valid, skipping registration");
                return;
            }

            var shared = hatchet.ItemDrop.m_itemData.m_shared;
            // Baked directly into this item's own cloned SharedData, not
            // per-instance m_customData -- safe here because "VoltunsHatchet"
            // is its own dedicated clone, not shared with any unrelated
            // item, and every Voltun's Hatchet should have this same fixed
            // identity profile (only the rolled affixes below vary per copy).
            shared.m_damages.m_chop *= RarityLootPlugin.VoltunDamageMultiplier.Value;
            shared.m_attack.m_speedFactor *= RarityLootPlugin.VoltunSpeedMultiplier.Value;

            // Fixed identity bonus, not a rolled affix -- baked into the
            // template ItemData's own m_customData so every clone (every
            // actual Hatchet a player holds) inherits it automatically,
            // since Clone() deep-copies m_customData. LogYieldPatch reads
            // this same key generically, so any future item could grant
            // the same bonus the same way.
            var hatchetData = hatchet.ItemDrop.m_itemData;
            if (hatchetData.m_customData == null) hatchetData.m_customData = new Dictionary<string, string>();
            hatchetData.m_customData[LogYieldPatch.LogYieldBonusKey] =
                RarityLootPlugin.VoltunLogYieldBonusPercent.Value.ToString(CultureInfo.InvariantCulture);

            ItemManager.Instance.AddItem(hatchet);
            ItemRollTrigger.Register(shared, RarityTier.Legendary, RarityLootPlugin.LegendaryAffixCount.Value);
        }

        static void RegisterPickaxe()
        {
            var config = new ItemConfig
            {
                Name = "Voltun's Pickaxe",
                Description = "Mirrors the Hatchet's legend in stone-breaking form. Bites deeper and swings faster than an Antler Pickaxe.",
                CraftingStation = CraftingStations.Forge,
                Requirements = new[]
                {
                    new RequirementConfig("Wood", RarityLootPlugin.VoltunPickaxeWoodCost.Value),
                    new RequirementConfig("Copper", RarityLootPlugin.VoltunPickaxeCopperCost.Value),
                    new RequirementConfig("Bronze", RarityLootPlugin.VoltunPickaxeBronzeCost.Value),
                },
            };

            var pickaxe = new CustomItem("VoltunsPickaxe", "PickaxeAntler", config);
            if (!pickaxe.IsValid())
            {
                Jotunn.Logger.LogError("VoltunsPickaxe item is not valid, skipping registration");
                return;
            }

            var shared = pickaxe.ItemDrop.m_itemData.m_shared;
            shared.m_damages.m_pickaxe *= RarityLootPlugin.VoltunDamageMultiplier.Value;
            shared.m_attack.m_speedFactor *= RarityLootPlugin.VoltunSpeedMultiplier.Value;

            ItemManager.Instance.AddItem(pickaxe);
            ItemRollTrigger.Register(shared, RarityTier.Legendary, RarityLootPlugin.LegendaryAffixCount.Value);
        }
    }
}
