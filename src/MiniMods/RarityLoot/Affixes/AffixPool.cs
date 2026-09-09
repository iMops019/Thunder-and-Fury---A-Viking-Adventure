using System.Collections.Generic;
using System.Linq;

namespace VikingAdventure.RarityLoot.Affixes
{
    // The shared pool every Magic/Rare/Legendary item rolls from. Starter
    // set for the first pass -- three affixes covering the stats most
    // players feel immediately (armor, health, weapon damage). Adding a
    // new affix later is just adding an entry here; nothing else needs to
    // change since the roller, application patches, and tooltip all read
    // this list generically.
    public static class AffixPool
    {
        public const string ArmorAffixId = "armor";
        public const string LifeAffixId = "life";
        public const string DamageAffixId = "damage_pct";

        public static readonly List<AffixDefinition> All = new List<AffixDefinition>
        {
            new AffixDefinition
            {
                Id = ArmorAffixId,
                DisplayFormat = "+{0} Armor",
                Min = 2,
                Max = 8,
                Target = AffixTarget.Armor,
            },
            new AffixDefinition
            {
                Id = LifeAffixId,
                DisplayFormat = "+{0} Max Health",
                Min = 10,
                Max = 40,
                Target = AffixTarget.Any,
            },
            new AffixDefinition
            {
                Id = DamageAffixId,
                DisplayFormat = "+{0}% Damage",
                Min = 5,
                Max = 20,
                Target = AffixTarget.Weapon,
            },
        };

        public static List<AffixDefinition> For(AffixTarget target)
        {
            return All.Where(a => a.Target == AffixTarget.Any || a.Target == target).ToList();
        }

        public static AffixDefinition Get(string id)
        {
            return All.FirstOrDefault(a => a.Id == id);
        }
    }
}
