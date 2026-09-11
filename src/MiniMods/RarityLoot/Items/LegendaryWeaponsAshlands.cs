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
    // ---- 11 Legendary weapons for Ashlands (2026-09-11) ----
    //
    // Same shape as LegendaryWeaponsMistlands.cs -- read that file's
    // header for the full rationale. Same three-way split: Morgen gets
    // the unique/lore-arc set (2, physical -- Ashlands' own large rock
    // creature), Twitcher gets the elemental set (5, all fire -- the
    // small explosive fire bug, this biome's own answer to Mistlands'
    // Dverger Mage), Charred gets the melee/no-element set (4, the
    // biome's basic soldier). Base items confirmed real via the live
    // dump: SwordDyrnwyn, AxeJotunBane, MaceEldner, SpearSplitner,
    // AxeBerzerkr, BowAshlands -- vanilla's own already-named Ashlands
    // weapon set (cloned in their base, un-suffixed form; the "_Blood"/
    // "_Lightning"/"_Nature" variants seen in the dump are vanilla's own
    // separate pre-built magic items, not touched here).
    //
    // Creature name caveat: "Morgen" and "Twitcher" are Ashlands' own
    // creatures, confirmed as real prefab-name fragments via the dump
    // (`charred_twitcher_throw`). "Charred" itself may actually be split
    // into distinct melee/ranged variants in the real game (the dump's
    // own `charred_bow`/`charred_greatsword` entries are attack hitboxes,
    // not proof either way) -- used here as the plain creature name,
    // same safe-failure fallback as every other biome if it's wrong.
    public static class LegendaryWeaponsAshlands
    {
        const string CreatureMorgen = "Morgen";
        const string CreatureTwitcher = "Twitcher";
        const string CreatureCharred = "Charred";
        const float DefaultBonusDamage = 14f;
        const float DefaultDropChance = 0.02f;

        class Def
        {
            public string PrefabName;
            public string DisplayName;
            public string Description;
            public string BaseItem;
            public string Element; // "fire" or "physical"
            public string DropCreature;
        }

        static readonly Def[] Defs =
        {
            // Morgen -- physical, lore-arc
            new Def { PrefabName = "MorgensMaw", DisplayName = "Morgen's Maw", BaseItem = "MaceEldner", Element = "physical", DropCreature = CreatureMorgen,
                Description = "Torn from something that used to be part of the ground itself." },
            new Def { PrefabName = "AshwokenCleaver", DisplayName = "Ashwoken Cleaver", BaseItem = "AxeJotunBane", Element = "physical", DropCreature = CreatureMorgen,
                Description = "Heavy enough to remind you the ground can fight back." },

            // Twitcher -- fire
            new Def { PrefabName = "Cinderjaw", DisplayName = "Cinderjaw", BaseItem = "SwordDyrnwyn", Element = "fire", DropCreature = CreatureTwitcher,
                Description = "Named for an old blade that was never meant to stop burning." },
            new Def { PrefabName = "SplitnersEmber", DisplayName = "Splitner's Ember", BaseItem = "SpearSplitner", Element = "fire", DropCreature = CreatureTwitcher,
                Description = "The tip glows long after the throw." },
            new Def { PrefabName = "BerzerkrsBlaze", DisplayName = "Berzerkr's Blaze", BaseItem = "AxeBerzerkr", Element = "fire", DropCreature = CreatureTwitcher,
                Description = "Burns hotter the angrier its wielder gets. Allegedly." },
            new Def { PrefabName = "AshwindBow", DisplayName = "Ashwind Bow", BaseItem = "BowAshlands", Element = "fire", DropCreature = CreatureTwitcher,
                Description = "Its arrows arrive already smoldering." },
            new Def { PrefabName = "Emberheart", DisplayName = "Emberheart", BaseItem = "MaceEldner", Element = "fire", DropCreature = CreatureTwitcher,
                Description = "Feels warm even when nothing nearby is burning." },

            // Charred -- regular, physical/melee
            new Def { PrefabName = "CharredLegionsBlade", DisplayName = "Charred Legion's Blade", BaseItem = "SwordDyrnwyn", Element = "physical", DropCreature = CreatureCharred,
                Description = "Carried by one of many. The rest didn't make it out of the ash." },
            new Def { PrefabName = "CinderguardsAxe", DisplayName = "Cinderguard's Axe", BaseItem = "AxeJotunBane", Element = "physical", DropCreature = CreatureCharred,
                Description = "Meant for guarding something. Not especially well, in the end." },
            new Def { PrefabName = "AshfrontSpear", DisplayName = "Ashfront Spear", BaseItem = "SpearSplitner", Element = "physical", DropCreature = CreatureCharred,
                Description = "Held the line as long as anything could, out there." },
            new Def { PrefabName = "WarbrandOfTheCharred", DisplayName = "Warbrand of the Charred", BaseItem = "AxeBerzerkr", Element = "physical", DropCreature = CreatureCharred,
                Description = "Scorched black long before it ever left its owner's hand." },
        };

        static readonly Dictionary<string, ConfigEntry<float>> BonusDamage = new Dictionary<string, ConfigEntry<float>>();
        static readonly Dictionary<string, ConfigEntry<float>> DropChance = new Dictionary<string, ConfigEntry<float>>();

        public static void BindConfig(ConfigFile config)
        {
            foreach (Def def in Defs)
            {
                BonusDamage[def.PrefabName] = config.Bind(
                    "LegendaryWeaponsAshlands", $"{def.PrefabName}_BonusDamage", DefaultBonusDamage,
                    $"Flat {def.Element} damage bonus for {def.DisplayName} (base: {def.BaseItem}).");

                DropChance[def.PrefabName] = config.Bind(
                    "LegendaryWeaponsAshlands", $"{def.PrefabName}_DropChance", DefaultDropChance,
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
                    Jotunn.Logger.LogError($"LegendaryWeaponsAshlands: '{def.PrefabName}' (base '{def.BaseItem}') is not valid, skipping");
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
                case "physical": damages.m_damage += bonus; break;
            }
        }

        static void RegisterDropOn(string creaturePrefabName, string itemPrefabName, float chance)
        {
            GameObject creaturePrefab = PrefabManager.Instance.GetPrefab(creaturePrefabName);
            CharacterDrop drop = creaturePrefab != null ? creaturePrefab.GetComponent<CharacterDrop>() : null;
            if (drop == null)
            {
                Jotunn.Logger.LogWarning($"LegendaryWeaponsAshlands: could not find CharacterDrop on '{creaturePrefabName}', skipping drop source for '{itemPrefabName}'");
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
