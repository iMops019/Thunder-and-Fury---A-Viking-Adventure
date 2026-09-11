namespace ThunderFury.RarityLoot.Loot
{
    // ---- The material-tier ladder, matched to the game's own biome order ----
    //
    // User's own framing (2026-09-10): "a Greybeard in Meadows can drop up
    // to the highest tier able to be made in Meadows. You can't mine
    // copper till Black Forest, so being able to get Rare Copper gear in
    // low level Meadows sounds super OP." This enum is that ceiling, one
    // step per real progression gate. AshLands and DeepNorth share the
    // top tier for now -- deliberately conservative until their real
    // material list gets the same scrutiny as the earlier tiers.
    public enum BiomeTier
    {
        Meadows = 0,
        BlackForest = 1,
        Swamp = 2,
        Mountain = 3,
        Plains = 4,
        Mistlands = 5,
        AshLands = 6,
        DeepNorth = 6,
    }

    public static class BiomeTierMap
    {
        // Heightmap.Biome is a [Flags] enum (confirmed via decompile,
        // same conclusion DevZoneSettingsRegistry already reached) so a
        // location can nominally carry more than one bit, but
        // WorldGenerator.GetBiome(Vector3) -- the call site this feeds --
        // always returns a single concrete biome for a world position, so
        // a plain switch is safe here without needing flag-mask logic.
        public static int MaxTierFor(Heightmap.Biome biome)
        {
            switch (biome)
            {
                case Heightmap.Biome.Meadows: return (int)BiomeTier.Meadows;
                case Heightmap.Biome.BlackForest: return (int)BiomeTier.BlackForest;
                case Heightmap.Biome.Swamp: return (int)BiomeTier.Swamp;
                case Heightmap.Biome.Mountain: return (int)BiomeTier.Mountain;
                case Heightmap.Biome.Plains: return (int)BiomeTier.Plains;
                case Heightmap.Biome.Mistlands: return (int)BiomeTier.Mistlands;
                case Heightmap.Biome.AshLands: return (int)BiomeTier.AshLands;
                case Heightmap.Biome.DeepNorth: return (int)BiomeTier.DeepNorth;
                default: return (int)BiomeTier.Meadows;
            }
        }
    }
}
