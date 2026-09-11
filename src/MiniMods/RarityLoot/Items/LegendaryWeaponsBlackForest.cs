using System.Collections.Generic;
using BepInEx.Configuration;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using ThunderFury.Core.Combat;
using ThunderFury.RarityLoot.Affixes;
using ThunderFury.RarityLoot.Patches;

namespace ThunderFury.RarityLoot.Items
{
    // ---- 11 Legendary weapons for Black Forest (2026-09-11) ----
    //
    // Same shape as LegendaryWeaponsBatch.cs (Meadows) -- read that file's
    // header for the full "why a data table, why Config.Bind per number"
    // rationale. New this round: drops are split across THREE creatures
    // by theme (user's own request) instead of one:
    //  - Troll: primitive/crushing, no element -- "1-2" weapons per the
    //    user's own framing, landed on 2.
    //  - Greydwarf Shaman: elemental/poison-flavored (fits its own
    //    vanilla nature-magic identity).
    //  - Greydwarf Brute: pure melee/berserker, no element -- brute
    //    strength, not magic.
    //  - Skeleton: deliberately excluded entirely, per the user's own
    //    explicit call.
    // Bases are all Bronze/Copper tier (Black Forest's own real
    // material ceiling, confirmed real via the same live ObjectDB dump
    // used for the Meadows batch) -- consistent with the Lightning Sword
    // lesson of not handing out a base weapon stronger than the biome it
    // drops in normally offers.
    //
    // Creature name caveat: "Troll" is Black Forest's own well-known
    // creature and near-certainly correct. "Greydwarf_Elite" (Brute) and
    // "Greydwarf_Shaman" are standard, well-established Valheim modding
    // names but NOT independently confirmed via decompile this session
    // the way item base names were -- safe failure mode if either is
    // wrong: CreatureDropRegistry-style resolution just logs a warning
    // and skips that one drop source, the item itself still exists and
    // is spawnable via the Dev Tool's Spawn tab regardless.
    public static class LegendaryWeaponsBlackForest
    {
        const string CreatureTroll = "Troll";
        const string CreatureBrute = "Greydwarf_Elite";
        const string CreatureShaman = "Greydwarf_Shaman";
        const float DefaultBonusDamage = 6f;
        const float DefaultDropChance = 0.02f;

        class Def
        {
            public string PrefabName;
            public string DisplayName;
            public string Description;
            public string BaseItem;
            public string Element; // "fire", "frost", "poison", "lightning", "physical"
            public string DropCreature;
            public string SpecialEffect;
        }

        static readonly Def[] Defs =
        {
            // Troll -- primitive, crushing, no element
            new Def { PrefabName = "SkogtrollsWarclub", DisplayName = "Skogtroll's Warclub", BaseItem = "MaceBronze", Element = "physical", DropCreature = CreatureTroll,
                Description = "Heavy enough to end a fight before it starts." },
            new Def { PrefabName = "BergrisisTusk", DisplayName = "Bergrisi's Tusk", BaseItem = "SpearBronze", Element = "physical", DropCreature = CreatureTroll,
                Description = "Carved from something that used to be much, much larger." },

            // Greydwarf Shaman -- elemental / poison
            new Def { PrefabName = "SkuggasynirsBlight", DisplayName = "Skuggasynir's Blight", BaseItem = "KnifeCopper", Element = "poison", DropCreature = CreatureShaman,
                Description = "A shaman's parting gift -- the wound it leaves never quite closes." },
            new Def { PrefabName = "VolvasFrostcall", DisplayName = "Volva's Frostcall", BaseItem = "SwordBronze", Element = "frost", DropCreature = CreatureShaman,
                Description = "A seeress's blade, cold enough to still the blood before it spills." },
            new Def { PrefabName = "EmberweaversAtgeir", DisplayName = "Emberweaver's Atgeir", BaseItem = "AtgeirBronze", Element = "fire", DropCreature = CreatureShaman,
                Description = "Wreathed in a flame that never quite goes out." },
            new Def { PrefabName = "StormcallersBow", DisplayName = "Stormcaller's Bow", BaseItem = "BowFineWood", Element = "lightning", DropCreature = CreatureShaman,
                Description = "Its string hums even when no arrow is drawn." },
            new Def { PrefabName = "NaturesReckoning", DisplayName = "Nature's Reckoning", BaseItem = "AxeBronze", Element = "poison", DropCreature = CreatureShaman,
                Description = "The forest's own answer to those who take too much from it." },

            // Greydwarf Brute -- pure melee / berserker, no element
            new Def { PrefabName = "BrutesReckoning", DisplayName = "Brute's Reckoning", BaseItem = "AxeBronze", Element = "physical", DropCreature = CreatureBrute,
                Description = "Swung, not aimed." },
            new Def { PrefabName = "BerserkrsFury", DisplayName = "Berserkr's Fury", BaseItem = "SwordBronze", Element = "physical", DropCreature = CreatureBrute,
                Description = "It rewards the reckless." },
            new Def { PrefabName = "IronhideRender", DisplayName = "Ironhide Render", BaseItem = "MaceBronze", Element = "physical", DropCreature = CreatureBrute,
                Description = "Built to crack what armor can't stop." },
            new Def { PrefabName = "WarlordsReach", DisplayName = "Warlord's Reach", BaseItem = "AtgeirBronze", Element = "physical", DropCreature = CreatureBrute,
                Description = "Keeps the fight at a distance the wielder chooses, not the enemy." },
        };

        static readonly Dictionary<string, ConfigEntry<float>> BonusDamage = new Dictionary<string, ConfigEntry<float>>();
        static readonly Dictionary<string, ConfigEntry<float>> DropChance = new Dictionary<string, ConfigEntry<float>>();

        public static void BindConfig(ConfigFile config)
        {
            foreach (Def def in Defs)
            {
                string label = def.Element == "physical" ? "physical" : def.Element;
                BonusDamage[def.PrefabName] = config.Bind(
                    "LegendaryWeaponsBlackForest", $"{def.PrefabName}_BonusDamage", DefaultBonusDamage,
                    $"Flat {label} damage bonus for {def.DisplayName} (base: {def.BaseItem}).");

                DropChance[def.PrefabName] = config.Bind(
                    "LegendaryWeaponsBlackForest", $"{def.PrefabName}_DropChance", DefaultDropChance,
                    $"Chance per {def.DropCreature} kill for {def.DisplayName} to drop. {DefaultDropChance * 100f:0.#}% default.");
            }
        }

        public static void Register()
        {
            foreach (Def def in Defs)
            {
                var config = new ItemConfig
                {
                    Name = def.DisplayName,
                    Description = def.Description,
                };

                var item = new CustomItem(def.PrefabName, def.BaseItem, config);
                if (!item.IsValid())
                {
                    Jotunn.Logger.LogError($"LegendaryWeaponsBlackForest: '{def.PrefabName}' (base '{def.BaseItem}') is not valid, skipping");
                    continue;
                }

                item.Recipe = null;

                ItemDrop.ItemData itemData = item.ItemDrop.m_itemData;
                ItemDrop.ItemData.SharedData shared = itemData.m_shared;

                ApplyElementBonus(ref shared.m_damages, def.Element, BonusDamage[def.PrefabName].Value);

                if (!string.IsNullOrEmpty(def.SpecialEffect))
                {
                    if (itemData.m_customData == null) itemData.m_customData = new Dictionary<string, string>();
                    itemData.m_customData[WeaponSpecialEffectPatch.EffectKey] = def.SpecialEffect;
                }

                ItemManager.Instance.AddItem(item);
                ItemRollTrigger.Register(shared, RarityTier.Legendary, RarityLootPlugin.LegendaryAffixCount.Value);

                RegisterDropOn(def.DropCreature, def.PrefabName, DropChance[def.PrefabName].Value);
            }
        }

        static void ApplyElementBonus(ref HitData.DamageTypes damages, string element, float bonus)
        {
            switch (element)
            {
                case "fire": damages.m_fire += bonus; break;
                case "frost": damages.m_frost += bonus; break;
                case "poison": damages.m_poison += bonus; break;
                case "lightning": damages.m_lightning += bonus; break;
                case "physical": damages.m_damage += bonus; break;
            }
        }

        static void RegisterDropOn(string creaturePrefabName, string itemPrefabName, float chance)
        {
            GameObject creaturePrefab = PrefabManager.Instance.GetPrefab(creaturePrefabName);
            CharacterDrop drop = creaturePrefab != null ? creaturePrefab.GetComponent<CharacterDrop>() : null;
            if (drop == null)
            {
                Jotunn.Logger.LogWarning($"LegendaryWeaponsBlackForest: could not find CharacterDrop on '{creaturePrefabName}', skipping drop source for '{itemPrefabName}'");
                return;
            }

            GameObject itemPrefab = PrefabManager.Instance.GetPrefab(itemPrefabName);
            if (itemPrefab == null) return;

            drop.m_drops.Add(new CharacterDrop.Drop
            {
                m_prefab = itemPrefab,
                m_amountMin = 1,
                m_amountMax = 1,
                m_chance = chance,
                m_onePerPlayer = true,
            });
        }
    }
}
