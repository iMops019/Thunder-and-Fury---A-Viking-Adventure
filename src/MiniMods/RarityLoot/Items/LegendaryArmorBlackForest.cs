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
    // ---- 11 Legendary armor pieces for Black Forest (2026-09-11) ----
    //
    // Same shape as LegendaryArmorBatch.cs (Meadows) and
    // LegendaryWeaponsBlackForest.cs (this biome's weapons) -- read
    // either file's header for the full rationale. Slot coverage across
    // the three creature themes: Helmet x2 (Troll, Shaman), Chest x3
    // (Troll, Shaman, Brute), Legs x2 (Shaman, Brute), Shoulder x2
    // (Shaman, Brute), Shield x2 (Shaman, Brute) -- every slot covered
    // at least twice. Bases are Bronze/TrollLeather tier, matching the
    // weapons file's own biome-appropriate ceiling.
    public static class LegendaryArmorBlackForest
    {
        const string CreatureTroll = "Troll";
        const string CreatureBrute = "Greydwarf_Elite";
        const string CreatureShaman = "Greydwarf_Shaman";
        const float DefaultArmorBonus = 4f;
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
            // Troll
            new Def { PrefabName = "BonecapOfTheBergrisi", DisplayName = "Bonecap of the Bergrisi", BaseItem = "HelmetTrollLeather", DropCreature = CreatureTroll,
                Description = "Fitted from a skull too large to have come from anything sane." },
            new Def { PrefabName = "HideOfTheSkogtroll", DisplayName = "Hide of the Skogtroll", BaseItem = "ArmorTrollLeatherChest", DropCreature = CreatureTroll,
                Description = "Thick, crude, and stops more than it looks like it should." },

            // Greydwarf Shaman -- mystic/elemental warding
            new Def { PrefabName = "VolvasCirclet", DisplayName = "Volva's Circlet", BaseItem = "HelmetBronze", DropCreature = CreatureShaman,
                Description = "Worn by a seeress who saw more than she should have." },
            new Def { PrefabName = "RobeOfTheGrovewarden", DisplayName = "Robe of the Grovewarden", BaseItem = "ArmorBronzeChest", DropCreature = CreatureShaman,
                Description = "Smells faintly of moss and old magic." },
            new Def { PrefabName = "GrovewardensLegwraps", DisplayName = "Grovewarden's Legwraps", BaseItem = "ArmorBronzeLegs", DropCreature = CreatureShaman,
                Description = "Wrapped in roots that never quite finished growing." },
            new Def { PrefabName = "MantleOfEmbersAndFrost", DisplayName = "Mantle of Embers and Frost", BaseItem = "CapeLinen", DropCreature = CreatureShaman,
                Description = "Warm on one side, cold on the other -- never quite comfortable." },
            new Def { PrefabName = "WardstoneAegis", DisplayName = "Wardstone Aegis", BaseItem = "ShieldBronzeBuckler", DropCreature = CreatureShaman,
                Description = "Etched with wards that hum faintly when struck." },

            // Greydwarf Brute -- heavy warrior
            new Def { PrefabName = "WarplateOfTheBrute", DisplayName = "Warplate of the Brute", BaseItem = "ArmorBronzeChest", DropCreature = CreatureBrute,
                Description = "Dented, scarred, and still standing." },
            new Def { PrefabName = "GreavesOfTheBerserkr", DisplayName = "Greaves of the Berserkr", BaseItem = "ArmorBronzeLegs", DropCreature = CreatureBrute,
                Description = "Made for closing distance, not for standing still." },
            new Def { PrefabName = "CloakOfTheBloodrage", DisplayName = "Cloak of the Bloodrage", BaseItem = "CapeTrollHide", DropCreature = CreatureBrute,
                Description = "Stained a color that doesn't wash out anymore." },
            new Def { PrefabName = "BrutesBulwark", DisplayName = "Brute's Bulwark", BaseItem = "ShieldBronzeBuckler", DropCreature = CreatureBrute,
                Description = "Less a shield than a wall that happens to move." },
        };

        static readonly Dictionary<string, ConfigEntry<float>> ArmorBonus = new Dictionary<string, ConfigEntry<float>>();
        static readonly Dictionary<string, ConfigEntry<float>> DropChance = new Dictionary<string, ConfigEntry<float>>();

        public static void BindConfig(ConfigFile config)
        {
            foreach (Def def in Defs)
            {
                ArmorBonus[def.PrefabName] = config.Bind(
                    "LegendaryArmorBlackForest", $"{def.PrefabName}_ArmorBonus", DefaultArmorBonus,
                    $"Flat armor bonus for {def.DisplayName} (base: {def.BaseItem}).");

                DropChance[def.PrefabName] = config.Bind(
                    "LegendaryArmorBlackForest", $"{def.PrefabName}_DropChance", DefaultDropChance,
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
                    Jotunn.Logger.LogError($"LegendaryArmorBlackForest: '{def.PrefabName}' (base '{def.BaseItem}') is not valid, skipping");
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
                Jotunn.Logger.LogWarning($"LegendaryArmorBlackForest: could not find CharacterDrop on '{creaturePrefabName}', skipping drop source for '{itemPrefabName}'");
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
