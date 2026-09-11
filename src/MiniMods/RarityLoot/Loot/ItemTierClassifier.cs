namespace ThunderFury.RarityLoot.Loot
{
    // ---- A weapon/armor item's tier = the highest-tier material its own recipe needs ----
    //
    // Confirmed via the real 1.0 decompile: Recipe.m_resources is a
    // public Piece.Requirement[], each with a public ItemDrop m_resItem
    // and int m_amount. Reading a recipe's own real requirement list
    // instead of hand-typing "this item belongs to that biome" is the
    // same self-verifying-over-guessing principle VanillaItemCatalog
    // already uses for its own live-catalog approach.
    public static class ItemTierClassifier
    {
        // False means "don't know, exclude this item from every biome
        // pool" -- either it has no crafting recipe (drop-only items like
        // LightningSword aren't meant to be picked as an ambient base
        // item anyway) or one of its materials isn't in MaterialTierMap.
        public static bool TryGetItemTier(Recipe recipe, out int tier)
        {
            tier = 0;

            if (recipe?.m_resources == null || recipe.m_resources.Length == 0) return false;

            int maxTier = -1;
            foreach (Piece.Requirement requirement in recipe.m_resources)
            {
                if (requirement?.m_resItem == null) continue;

                string materialName = requirement.m_resItem.gameObject.name;
                if (!MaterialTierMap.Tiers.TryGetValue(materialName, out int materialTier))
                {
                    return false;
                }

                if (materialTier > maxTier) maxTier = materialTier;
            }

            if (maxTier < 0) return false;

            tier = maxTier;
            return true;
        }
    }
}
