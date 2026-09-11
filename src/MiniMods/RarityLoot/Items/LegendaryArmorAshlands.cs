using System.Collections.Generic;
using BepInEx.Configuration;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using ThunderFury.RarityLoot.Affixes;
using ThunderFury.RarityLoot.Patches;

namespace ThunderFury.RarityLoot.Items
{
    // ---- 11 Legendary armor pieces for Ashlands (2026-09-11) ----
    //
    // Same shape as LegendaryArmorMistlands.cs -- read that file's
    // header, and LegendaryWeaponsAshlands.cs in this same folder, for
    // the full rationale. Slot coverage: Helmet x2 (Morgen, Twitcher),
    // Chest x3 (Morgen, Twitcher, Charred), Legs x2 (Twitcher, Charred),
    // Shoulder x2 (Twitcher, Charred), Shield x2 (Twitcher, Charred).
    // Flametal-tier bases throughout -- Ashlands' own real armor
    // material, confirmed real via the live dump -- plus `CapeAsh`, a
    // genuine Ashlands-specific cape (unlike several earlier biomes that
    // had to reuse a lower-tier one).
    public static class LegendaryArmorAshlands
    {
        const string CreatureMorgen = "Morgen";
        const string CreatureTwitcher = "Twitcher";
        const string CreatureCharred = "Charred";
        const float DefaultArmorBonus = 11f;
        const float DefaultDropChance = 0.02f;

        class Def
        {
            public string PrefabName;
            public string DisplayName;
            public string Description;
            public string BaseItem;
            public string DropCreature;
        }

        static readonly Def[] Defs =
        {
            // Morgen
            new Def { PrefabName = "MorgensCrown", DisplayName = "Morgen's Crown", BaseItem = "HelmetFlametal", DropCreature = CreatureMorgen,
                Description = "Fused from stone and something hotter than stone should allow." },
            new Def { PrefabName = "HideOfTheMorgen", DisplayName = "Hide of the Morgen", BaseItem = "ArmorFlametalChest", DropCreature = CreatureMorgen,
                Description = "Still radiates a little heat, long after the fight." },

            // Twitcher -- fire
            new Def { PrefabName = "Cinderhood", DisplayName = "Cinderhood", BaseItem = "HelmetAshlandsMediumHood", DropCreature = CreatureTwitcher,
                Description = "Singed at the edges from something small and fast." },
            new Def { PrefabName = "EmberwovenPlate", DisplayName = "Emberwoven Plate", BaseItem = "ArmorFlametalChest", DropCreature = CreatureTwitcher,
                Description = "Doesn't catch fire. Mostly because it's already used to it." },
            new Def { PrefabName = "AshwalkerGreaves", DisplayName = "Ashwalker Greaves", BaseItem = "ArmorFlametalLegs", DropCreature = CreatureTwitcher,
                Description = "Leaves faint scorch marks with every step, for a little while." },
            new Def { PrefabName = "MantleOfCinders", DisplayName = "Mantle of Cinders", BaseItem = "CapeAsh", DropCreature = CreatureTwitcher,
                Description = "Ash drifts off it even when nothing nearby is burning." },
            new Def { PrefabName = "FlametalWard", DisplayName = "Flametal Ward", BaseItem = "ShieldFlametal", DropCreature = CreatureTwitcher,
                Description = "Runs hot to the touch, always." },

            // Charred -- regular
            new Def { PrefabName = "CharredLegionPlate", DisplayName = "Charred Legion Plate", BaseItem = "ArmorFlametalChest", DropCreature = CreatureCharred,
                Description = "One suit among many that marched out of the ash together." },
            new Def { PrefabName = "WarfrontGreaves", DisplayName = "Warfront Greaves", BaseItem = "ArmorFlametalLegs", DropCreature = CreatureCharred,
                Description = "Kept moving long after they should have given out." },
            new Def { PrefabName = "CloakOfTheFallenLegion", DisplayName = "Cloak of the Fallen Legion", BaseItem = "CapeAsh", DropCreature = CreatureCharred,
                Description = "Passed down the line until there was no one left to pass it to." },
            new Def { PrefabName = "BastionOfTheCharred", DisplayName = "Bastion of the Charred", BaseItem = "ShieldFlametalTower", DropCreature = CreatureCharred,
                Description = "Held the line longer than the soldier behind it, in the end." },
        };

        static readonly Dictionary<string, ConfigEntry<float>> ArmorBonus = new Dictionary<string, ConfigEntry<float>>();
        static readonly Dictionary<string, ConfigEntry<float>> DropChance = new Dictionary<string, ConfigEntry<float>>();

        public static void BindConfig(ConfigFile config)
        {
            foreach (Def def in Defs)
            {
                ArmorBonus[def.PrefabName] = config.Bind(
                    "LegendaryArmorAshlands", $"{def.PrefabName}_ArmorBonus", DefaultArmorBonus,
                    $"Flat armor bonus for {def.DisplayName} (base: {def.BaseItem}).");

                DropChance[def.PrefabName] = config.Bind(
                    "LegendaryArmorAshlands", $"{def.PrefabName}_DropChance", DefaultDropChance,
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
                    Jotunn.Logger.LogError($"LegendaryArmorAshlands: '{def.PrefabName}' (base '{def.BaseItem}') is not valid, skipping");
                    continue;
                }

                item.Recipe = null;

                ItemDrop.ItemData.SharedData shared = item.ItemDrop.m_itemData.m_shared;
                shared.m_armor += ArmorBonus[def.PrefabName].Value;

                ItemManager.Instance.AddItem(item);
                ItemRollTrigger.Register(shared, RarityTier.Legendary, RarityLootPlugin.LegendaryAffixCount.Value);

                RegisterDropOn(def.DropCreature, def.PrefabName, DropChance[def.PrefabName].Value);
            }
        }

        static void RegisterDropOn(string creaturePrefabName, string itemPrefabName, float chance)
        {
            GameObject creaturePrefab = PrefabManager.Instance.GetPrefab(creaturePrefabName);
            CharacterDrop drop = creaturePrefab != null ? creaturePrefab.GetComponent<CharacterDrop>() : null;
            if (drop == null)
            {
                Jotunn.Logger.LogWarning($"LegendaryArmorAshlands: could not find CharacterDrop on '{creaturePrefabName}', skipping drop source for '{itemPrefabName}'");
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
