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
    // ---- 11 Legendary weapons for Mistlands (2026-09-11) ----
    //
    // Same shape as LegendaryWeaponsPlains.cs -- read that file's header
    // for the full rationale. Same three-way split: Gjall gets the
    // unique/lore-arc set (2, physical -- the biome's own large, iconic
    // flying creature), Dverger Mage gets the elemental set (5, mixing
    // fire AND frost this time -- Dverger mages canonically cast both in
    // vanilla, the first biome where that split actually fits one
    // creature instead of forcing a single element), Seeker gets the
    // melee/no-element set (4, the ubiquitous Mistlands bug). "Gold" is
    // confirmed via the live dump to be Mistlands' own real weapon-tier
    // branding (SwordGold/AxeGold/etc., with elemental sub-variants like
    // "Gold_FrostFire" already built into vanilla, not a Black-Metal- or
    // material-named tier the way earlier biomes were) -- Carapace used
    // for the one weapon (spear) where a Mistlands-material-named option
    // exists instead.
    //
    // Creature name caveat: "Seeker" and "Gjall" are Mistlands' own
    // well-known creatures. "Dverger_Mage" is a reasonable guess (the
    // "Dverger" spelling itself confirmed real via the live dump's own
    // item names -- DvergerHairMale, DvergerSuitFire, etc. -- but the
    // hostile mage creature's exact prefab name wasn't independently
    // confirmed this session). Same safe-failure fallback as every other
    // biome's unverified creature name.
    public static class LegendaryWeaponsMistlands
    {
        const string CreatureGjall = "Gjall";
        const string CreatureDvergerMage = "Dverger_Mage";
        const string CreatureSeeker = "Seeker";
        const float DefaultBonusDamage = 12f;
        const float DefaultDropChance = 0.02f;

        class Def
        {
            public string PrefabName;
            public string DisplayName;
            public string Description;
            public string BaseItem;
            public string Element; // "fire", "frost", "physical"
            public string DropCreature;
        }

        static readonly Def[] Defs =
        {
            // Gjall -- large, physical, lore-arc
            new Def { PrefabName = "GjallarhornsCry", DisplayName = "Gjallarhorn's Cry", BaseItem = "SpearCarapace", Element = "physical", DropCreature = CreatureGjall,
                Description = "Named for the horn that signals the end of everything -- it announces itself just as loudly." },
            new Def { PrefabName = "MistveilRender", DisplayName = "Mistveil Render", BaseItem = "BattleaxeGold", Element = "physical", DropCreature = CreatureGjall,
                Description = "Heavy enough to bring down something that was never meant to land." },

            // Dverger Mage -- fire and frost
            new Def { PrefabName = "NidavellirsFlame", DisplayName = "Nidavellir's Flame", BaseItem = "SwordGold", Element = "fire", DropCreature = CreatureDvergerMage,
                Description = "Forged in the deep places where the dwarves still remember fire." },
            new Def { PrefabName = "NidavellirsFrost", DisplayName = "Nidavellir's Frost", BaseItem = "AxeGold", Element = "frost", DropCreature = CreatureDvergerMage,
                Description = "The colder half of the same old forge." },
            new Def { PrefabName = "ForgewardensBlaze", DisplayName = "Forgewarden's Blaze", BaseItem = "MaceGold", Element = "fire", DropCreature = CreatureDvergerMage,
                Description = "Never quite stops glowing, even at rest." },
            new Def { PrefabName = "DeepforgeChill", DisplayName = "Deepforge Chill", BaseItem = "AtgeirGold", Element = "frost", DropCreature = CreatureDvergerMage,
                Description = "Draws the warmth out of a room before the blade even lands." },
            new Def { PrefabName = "EmberforgedFang", DisplayName = "Emberforged Fang", BaseItem = "KnifeGold", Element = "fire", DropCreature = CreatureDvergerMage,
                Description = "Small enough to conceal, hot enough to matter." },

            // Seeker -- regular, physical/melee
            new Def { PrefabName = "Chitinbane", DisplayName = "Chitinbane", BaseItem = "SwordGold", Element = "physical", DropCreature = CreatureSeeker,
                Description = "Finds the seams in a carapace better than most." },
            new Def { PrefabName = "CarapaceRender", DisplayName = "Carapace Render", BaseItem = "AxeGold", Element = "physical", DropCreature = CreatureSeeker,
                Description = "Built specifically for cracking what shouldn't crack easily." },
            new Def { PrefabName = "Swarmbreaker", DisplayName = "Swarmbreaker", BaseItem = "AtgeirGold", Element = "physical", DropCreature = CreatureSeeker,
                Description = "Keeps more than one set of mandibles at a comfortable distance." },
            new Def { PrefabName = "Hivecrusher", DisplayName = "Hivecrusher", BaseItem = "MaceGold", Element = "physical", DropCreature = CreatureSeeker,
                Description = "Unsubtle, and unbothered by that fact." },
        };

        static readonly Dictionary<string, ConfigEntry<float>> BonusDamage = new Dictionary<string, ConfigEntry<float>>();
        static readonly Dictionary<string, ConfigEntry<float>> DropChance = new Dictionary<string, ConfigEntry<float>>();

        public static void BindConfig(ConfigFile config)
        {
            foreach (Def def in Defs)
            {
                BonusDamage[def.PrefabName] = config.Bind(
                    "LegendaryWeaponsMistlands", $"{def.PrefabName}_BonusDamage", DefaultBonusDamage,
                    $"Flat {def.Element} damage bonus for {def.DisplayName} (base: {def.BaseItem}).");

                DropChance[def.PrefabName] = config.Bind(
                    "LegendaryWeaponsMistlands", $"{def.PrefabName}_DropChance", DefaultDropChance,
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
                    Jotunn.Logger.LogError($"LegendaryWeaponsMistlands: '{def.PrefabName}' (base '{def.BaseItem}') is not valid, skipping");
                    continue;
                }

                item.Recipe = null;

                ItemDrop.ItemData.SharedData shared = item.ItemDrop.m_itemData.m_shared;
                ApplyElementBonus(ref shared.m_damages, def.Element, BonusDamage[def.PrefabName].Value);

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
                case "physical": damages.m_damage += bonus; break;
            }
        }

        static void RegisterDropOn(string creaturePrefabName, string itemPrefabName, float chance)
        {
            GameObject creaturePrefab = PrefabManager.Instance.GetPrefab(creaturePrefabName);
            CharacterDrop drop = creaturePrefab != null ? creaturePrefab.GetComponent<CharacterDrop>() : null;
            if (drop == null)
            {
                Jotunn.Logger.LogWarning($"LegendaryWeaponsMistlands: could not find CharacterDrop on '{creaturePrefabName}', skipping drop source for '{itemPrefabName}'");
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
