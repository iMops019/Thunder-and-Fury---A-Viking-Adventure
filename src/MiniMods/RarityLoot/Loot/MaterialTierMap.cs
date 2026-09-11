using System.Collections.Generic;

namespace ThunderFury.RarityLoot.Loot
{
    // ---- Raw-material name -> the biome tier that material belongs to ----
    //
    // Deliberately NOT a list of which WEAPONS/ARMOR belong to which
    // biome -- this session already got burned twice guessing item names
    // directly (Hatchet/Knife/MushroomYellow). A weapon/armor item's own
    // tier is instead DERIVED (see ItemTierClassifier) from the real
    // materials its own crafting recipe requires, which is a much
    // smaller, more stable surface: there are only a couple dozen raw
    // materials in the whole game, and "Wood"/"Copper"/"Bronze"/"Flint"/
    // "Stone" are already confirmed working prefab names elsewhere in
    // this codebase (VoltunsSet.cs, StonePickaxe.cs, SkinningKnife.cs).
    //
    // Fail-safe by construction: ItemTierClassifier excludes any item
    // whose recipe references a material NOT in this map, rather than
    // guessing a tier for it. A missing/misspelled entry here means one
    // fewer item shows up in the ambient loot pool, never a
    // wrong-biome power spike -- the safe direction to fail in, given the
    // exact exploit (Rare gear in Meadows) this whole feature exists to
    // prevent. Entries past Mountain (Plains onward) are best-effort and
    // not yet confirmed against a real live recipe the way the earlier
    // tiers' materials are -- same caveat, same safe failure mode.
    public static class MaterialTierMap
    {
        public static readonly Dictionary<string, int> Tiers = new Dictionary<string, int>
        {
            // Tier 0 -- Meadows
            { "Wood", 0 },
            { "Stone", 0 },
            { "Flint", 0 },
            { "LeatherScraps", 0 },
            { "DeerHide", 0 },
            { "Resin", 0 },
            { "BoneFragments", 0 },
            { "Feathers", 0 },

            // Tier 1 -- Black Forest
            { "Copper", 1 },
            { "Tin", 1 },
            { "Bronze", 1 },
            { "Coal", 1 },
            { "CopperOre", 1 },
            { "TinOre", 1 },

            // Tier 2 -- Swamp
            { "Iron", 2 },
            { "IronScrap", 2 },
            { "ElderBark", 2 },
            { "Guck", 2 },

            // Tier 3 -- Mountain
            { "Silver", 3 },
            { "SilverOre", 3 },
            { "WolfPelt", 3 },
            { "WolfFang", 3 },
            { "FreezeGland", 3 },
            { "Crystal", 3 },

            // Tier 4 -- Plains (best-effort, see class remarks)
            { "BlackMetal", 4 },
            { "BlackMetalScrap", 4 },
            { "LinenThread", 4 },
            { "Needle", 4 },

            // Tier 5 -- Mistlands (best-effort)
            { "Carapace", 5 },
            { "SoftTissue", 5 },
            { "Eitr", 5 },

            // Tier 6 -- Ashlands / Deep North (best-effort)
            { "Flametal", 6 },
            { "FlametalNew", 6 },
        };
    }
}
