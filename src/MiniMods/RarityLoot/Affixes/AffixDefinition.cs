using System;

namespace VikingAdventure.RarityLoot.Affixes
{
    // Which item types an affix is eligible to roll on. Armor-only affixes
    // won't show up on a weapon and vice versa; Any can roll on anything.
    public enum AffixTarget
    {
        Weapon,
        Armor,
        Any,
    }

    // One rollable stat line, e.g. "+2 to 8 Armor". The pool of these
    // (see AffixPool) is what every Magic/Rare/Legendary item rolls from
    // -- same system regardless of rarity tier, just fewer rolls at lower
    // tiers.
    public class AffixDefinition
    {
        public string Id;
        public string DisplayFormat; // e.g. "+{0} Armor", "+{0}% Damage"
        public float Min;
        public float Max;
        public bool WholeNumber = true;
        public AffixTarget Target = AffixTarget.Any;

        public float Roll(Random rng)
        {
            float value = Min + (float)rng.NextDouble() * (Max - Min);
            return WholeNumber ? (float)Math.Round(value) : value;
        }
    }
}
