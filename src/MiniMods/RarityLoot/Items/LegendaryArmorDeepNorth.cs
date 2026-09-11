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
    // ---- 11 Legendary armor pieces for Deep North (2026-09-11) ----
    //
    // Same shape as LegendaryArmorAshlands.cs -- read that file's
    // header, and LegendaryWeaponsDeepNorth.cs in this same folder, for
    // the full rationale (including the stronger creature-name
    // confidence caveat for this specific biome). Unlike the weapons
    // file, real Deep-North-SPECIFIC armor tiers do exist and are used
    // here, confirmed via the live dump: Heavy (44 armor) for Fenring's
    // lore-arc set, Mage (22 armor) for Volture's elemental set, Medium
    // (34 armor) for Urchin's regular set -- three genuinely distinct
    // real tiers rather than one reused base three times. Shield uses
    // `ShieldSerpentscale` (300 armor, the single highest-armor shield
    // in the live dump), the most plausible real endgame shield for the
    // final biome.
    public static class LegendaryArmorDeepNorth
    {
        const string CreatureFenring = "Fenring";
        const string CreatureVolture = "Volture";
        const string CreatureUrchin = "Urchin";
        const float DefaultArmorBonus = 13f;
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
            // Fenring
            new Def { PrefabName = "CrownOfTheFenring", DisplayName = "Crown of the Fenring", BaseItem = "HelmetDNHeavy", DropCreature = CreatureFenring,
                Description = "Worn by the last of a line that outlasted the cold itself." },
            new Def { PrefabName = "HideOfTheFenring", DisplayName = "Hide of the Fenring", BaseItem = "ArmorDeepNorthHeavyChest", DropCreature = CreatureFenring,
                Description = "Thick enough that the cold never once got through." },

            // Volture -- frost
            new Def { PrefabName = "SkywardHood", DisplayName = "Skyward Hood", BaseItem = "HelmetDNMage", DropCreature = CreatureVolture,
                Description = "Light enough to forget you're wearing it, right up until the wind picks up." },
            new Def { PrefabName = "WindridersRobe", DisplayName = "Windrider's Robe", BaseItem = "ArmorDeepNorthMageChest", DropCreature = CreatureVolture,
                Description = "Moves like it's still catching a wind that isn't there anymore." },
            new Def { PrefabName = "WindridersLeggings", DisplayName = "Windrider's Leggings", BaseItem = "ArmorDeepNorthMagelegs", DropCreature = CreatureVolture,
                Description = "Frost never quite settles on them for long." },
            new Def { PrefabName = "MantleOfTheNorthWind", DisplayName = "Mantle of the North Wind", BaseItem = "CapeDeepNorthMage", DropCreature = CreatureVolture,
                Description = "Snaps in a wind that isn't blowing anywhere else." },
            new Def { PrefabName = "FrostscaleWard", DisplayName = "Frostscale Ward", BaseItem = "ShieldSerpentscale", DropCreature = CreatureVolture,
                Description = "Cold enough to sting bare skin, sturdy enough not to care." },

            // Urchin -- regular
            new Def { PrefabName = "SpinehidePlate", DisplayName = "Spinehide Plate", BaseItem = "ArmorDeepNorthMediumChest", DropCreature = CreatureUrchin,
                Description = "Every spine on it used to be pointed the other way." },
            new Def { PrefabName = "SpinehideGreaves", DisplayName = "Spinehide Greaves", BaseItem = "ArmorDeepNorthMediumlegs", DropCreature = CreatureUrchin,
                Description = "Built for wading through terrain that fights back." },
            new Def { PrefabName = "CloakOfTheDeepFrost", DisplayName = "Cloak of the Deep Frost", BaseItem = "CapeDeepNorth", DropCreature = CreatureUrchin,
                Description = "Never quite thaws, even by a fire." },
            new Def { PrefabName = "BulwarkOfTheNorth", DisplayName = "Bulwark of the North", BaseItem = "ShieldSerpentscale", DropCreature = CreatureUrchin,
                Description = "The last thing between its owner and the worst the ice has to offer." },
        };

        static readonly Dictionary<string, ConfigEntry<float>> ArmorBonus = new Dictionary<string, ConfigEntry<float>>();
        static readonly Dictionary<string, ConfigEntry<float>> DropChance = new Dictionary<string, ConfigEntry<float>>();

        public static void BindConfig(ConfigFile config)
        {
            foreach (Def def in Defs)
            {
                ArmorBonus[def.PrefabName] = config.Bind(
                    "LegendaryArmorDeepNorth", $"{def.PrefabName}_ArmorBonus", DefaultArmorBonus,
                    $"Flat armor bonus for {def.DisplayName} (base: {def.BaseItem}).");

                DropChance[def.PrefabName] = config.Bind(
                    "LegendaryArmorDeepNorth", $"{def.PrefabName}_DropChance", DefaultDropChance,
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
                    Jotunn.Logger.LogError($"LegendaryArmorDeepNorth: '{def.PrefabName}' (base '{def.BaseItem}') is not valid, skipping");
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
                Jotunn.Logger.LogWarning($"LegendaryArmorDeepNorth: could not find CharacterDrop on '{creaturePrefabName}', skipping drop source for '{itemPrefabName}'");
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
