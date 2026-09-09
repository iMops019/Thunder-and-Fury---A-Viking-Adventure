namespace VikingAdventure.RarityLoot.Affixes
{
    // Normal isn't used by the roller (nothing rolls itself down to
    // Normal), but it's here so "what tier is this item" always has a
    // real answer, including for vanilla items RarityLoot hasn't touched.
    public enum RarityTier
    {
        Normal,
        Magic,
        Rare,
        Legendary,
    }
}
