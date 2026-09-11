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
    // ---- 11 Legendary armor pieces for Mountain (2026-09-11) ----
    //
    // Same shape as LegendaryArmorSwamp.cs -- read that file's header,
    // and LegendaryWeaponsMountain.cs in this same folder, for the full
    // rationale. Slot coverage: Helmet x2 (Golem, Drake), Chest x3
    // (Golem, Drake, Wolf), Legs x2 (Drake, Wolf), Shoulder x2 (Drake,
    // Wolf), Shield x2 (Drake, Wolf). Wolf's pieces deliberately use the
    // real vanilla Wolf-pelt armor bases (ArmorWolfChest/Legs) rather
    // than a reused Iron/Silver base -- one case this biome actually has
    // a thematically perfect real match instead of needing a stand-in.
    public static class LegendaryArmorMountain
    {
        const string CreatureGolem = "Golem";
        const string CreatureDrake = "Drake";
        const string CreatureWolf = "Wolf";
        const float DefaultArmorBonus = 6f;
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
            // Golem
            new Def { PrefabName = "HrungnirsFaceplate", DisplayName = "Hrungnir's Faceplate", BaseItem = "HelmetIron", DropCreature = CreatureGolem,
                Description = "Cracked from the same stone as the giant it's named for." },
            new Def { PrefabName = "StoneheartPlate", DisplayName = "Stoneheart Plate", BaseItem = "ArmorIronChest", DropCreature = CreatureGolem,
                Description = "Heavier than it looks. Sturdier too." },

            // Drake -- frost
            new Def { PrefabName = "CrownOfTheWinterWyrm", DisplayName = "Crown of the Winter-Wyrm", BaseItem = "HelmetDrake", DropCreature = CreatureDrake,
                Description = "Taken from a wyrm that ruled the high peaks longer than any king." },
            new Def { PrefabName = "ScaleOfTheFrostDrake", DisplayName = "Scale of the Frost Drake", BaseItem = "ArmorIronChest", DropCreature = CreatureDrake,
                Description = "Cold to the touch, always -- even by a fire." },
            new Def { PrefabName = "FrostboundGreaves", DisplayName = "Frostbound Greaves", BaseItem = "ArmorIronLegs", DropCreature = CreatureDrake,
                Description = "Frost forms on them even in a warm room." },
            new Def { PrefabName = "WyrmscaleMantle", DisplayName = "Wyrmscale Mantle", BaseItem = "CapeLinen", DropCreature = CreatureDrake,
                Description = "Lined with something that doesn't quite feel like cloth." },
            new Def { PrefabName = "FrostguardAegis", DisplayName = "Frostguard Aegis", BaseItem = "ShieldSilver", DropCreature = CreatureDrake,
                Description = "Never once let the cold through -- or anything else, for that matter." },

            // Wolf -- regular
            new Def { PrefabName = "PeltOfThePacklord", DisplayName = "Pelt of the Packlord", BaseItem = "ArmorWolfChest", DropCreature = CreatureWolf,
                Description = "Taken from the wolf that led all the others." },
            new Def { PrefabName = "LegwrapsOfThePacklord", DisplayName = "Legwraps of the Packlord", BaseItem = "ArmorWolfLegs", DropCreature = CreatureWolf,
                Description = "Built for running down anything foolish enough to flee uphill." },
            new Def { PrefabName = "CloakOfHati", DisplayName = "Cloak of Hati", BaseItem = "CapeWolf", DropCreature = CreatureWolf,
                Description = "Named for the wolf that hunts the moon across the sky." },
            new Def { PrefabName = "FenrirsWard", DisplayName = "Fenrir's Ward", BaseItem = "ShieldSilver", DropCreature = CreatureWolf,
                Description = "Bears a bite mark that shouldn't have missed anything vital -- and somehow didn't." },
        };

        static readonly Dictionary<string, ConfigEntry<float>> ArmorBonus = new Dictionary<string, ConfigEntry<float>>();
        static readonly Dictionary<string, ConfigEntry<float>> DropChance = new Dictionary<string, ConfigEntry<float>>();

        public static void BindConfig(ConfigFile config)
        {
            foreach (Def def in Defs)
            {
                ArmorBonus[def.PrefabName] = config.Bind(
                    "LegendaryArmorMountain", $"{def.PrefabName}_ArmorBonus", DefaultArmorBonus,
                    $"Flat armor bonus for {def.DisplayName} (base: {def.BaseItem}).");

                DropChance[def.PrefabName] = config.Bind(
                    "LegendaryArmorMountain", $"{def.PrefabName}_DropChance", DefaultDropChance,
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
                    Jotunn.Logger.LogError($"LegendaryArmorMountain: '{def.PrefabName}' (base '{def.BaseItem}') is not valid, skipping");
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
                Jotunn.Logger.LogWarning($"LegendaryArmorMountain: could not find CharacterDrop on '{creaturePrefabName}', skipping drop source for '{itemPrefabName}'");
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
