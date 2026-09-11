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
    // ---- 11 Legendary armor pieces for Mistlands (2026-09-11) ----
    //
    // Same shape as LegendaryArmorPlains.cs -- read that file's header,
    // and LegendaryWeaponsMistlands.cs in this same folder, for the
    // full rationale. Slot coverage: Helmet x2 (Gjall, Dverger Mage),
    // Chest x3 (Gjall, Dverger Mage, Seeker), Legs x2 (Dverger Mage,
    // Seeker), Shoulder x2 (Dverger Mage, Seeker), Shield x2 (Dverger
    // Mage, Seeker). Carapace-tier bases throughout -- no
    // Mistlands-specific cape exists in vanilla, so Linen/TrollHide
    // capes are reused for the two Shoulder slots, same accepted pattern
    // as earlier biomes.
    public static class LegendaryArmorMistlands
    {
        const string CreatureGjall = "Gjall";
        const string CreatureDvergerMage = "Dverger_Mage";
        const string CreatureSeeker = "Seeker";
        const float DefaultArmorBonus = 9f;
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
            // Gjall
            new Def { PrefabName = "MistveilCrown", DisplayName = "Mistveil Crown", BaseItem = "HelmetCarapace", DropCreature = CreatureGjall,
                Description = "Taken from something that watched the mist from above longer than anyone below." },
            new Def { PrefabName = "GjallsHide", DisplayName = "Gjall's Hide", BaseItem = "ArmorCarapaceChest", DropCreature = CreatureGjall,
                Description = "Thicker than it has any right to be for something that flies." },

            // Dverger Mage -- fire and frost
            new Def { PrefabName = "NidavellirsCirclet", DisplayName = "Nidavellir's Circlet", BaseItem = "HelmetCarapace", DropCreature = CreatureDvergerMage,
                Description = "Worn by a smith who never quite left the forge behind." },
            new Def { PrefabName = "RobeOfTheDeepForge", DisplayName = "Robe of the Deep Forge", BaseItem = "ArmorCarapaceChest", DropCreature = CreatureDvergerMage,
                Description = "Warm on one side, cold on the other, same as the one who wore it." },
            new Def { PrefabName = "EmberfrostLeggings", DisplayName = "Emberfrost Leggings", BaseItem = "ArmorCarapaceLegs", DropCreature = CreatureDvergerMage,
                Description = "Steams faintly in the cold, and frosts over faintly near a fire." },
            new Def { PrefabName = "MantleOfTheDvergr", DisplayName = "Mantle of the Dvergr", BaseItem = "CapeLinen", DropCreature = CreatureDvergerMage,
                Description = "Smells of ash and old stone." },
            new Def { PrefabName = "WardplateOfNidavellir", DisplayName = "Wardplate of Nidavellir", BaseItem = "ShieldCarapaceBuckler", DropCreature = CreatureDvergerMage,
                Description = "Etched with forge-marks from a place that doesn't exist on any surface map." },

            // Seeker -- regular
            new Def { PrefabName = "ChitinweaveArmor", DisplayName = "Chitinweave Armor", BaseItem = "ArmorCarapaceChest", DropCreature = CreatureSeeker,
                Description = "Woven from plates that used to move on their own." },
            new Def { PrefabName = "SwarmwalkerGreaves", DisplayName = "Swarmwalker Greaves", BaseItem = "ArmorCarapaceLegs", DropCreature = CreatureSeeker,
                Description = "Light enough to keep pace with something that skitters." },
            new Def { PrefabName = "CloakOfTheSwarm", DisplayName = "Cloak of the Swarm", BaseItem = "CapeTrollHide", DropCreature = CreatureSeeker,
                Description = "Doesn't smell great. Nobody's complained twice." },
            new Def { PrefabName = "HivewallBulwark", DisplayName = "Hivewall Bulwark", BaseItem = "ShieldCarapace", DropCreature = CreatureSeeker,
                Description = "Plated the same way the things guarding it were." },
        };

        static readonly Dictionary<string, ConfigEntry<float>> ArmorBonus = new Dictionary<string, ConfigEntry<float>>();
        static readonly Dictionary<string, ConfigEntry<float>> DropChance = new Dictionary<string, ConfigEntry<float>>();

        public static void BindConfig(ConfigFile config)
        {
            foreach (Def def in Defs)
            {
                ArmorBonus[def.PrefabName] = config.Bind(
                    "LegendaryArmorMistlands", $"{def.PrefabName}_ArmorBonus", DefaultArmorBonus,
                    $"Flat armor bonus for {def.DisplayName} (base: {def.BaseItem}).");

                DropChance[def.PrefabName] = config.Bind(
                    "LegendaryArmorMistlands", $"{def.PrefabName}_DropChance", DefaultDropChance,
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
                    Jotunn.Logger.LogError($"LegendaryArmorMistlands: '{def.PrefabName}' (base '{def.BaseItem}') is not valid, skipping");
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
                Jotunn.Logger.LogWarning($"LegendaryArmorMistlands: could not find CharacterDrop on '{creaturePrefabName}', skipping drop source for '{itemPrefabName}'");
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
