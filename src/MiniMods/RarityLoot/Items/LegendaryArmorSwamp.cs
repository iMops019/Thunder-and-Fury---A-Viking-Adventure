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
    // ---- 11 Legendary armor pieces for Swamp (2026-09-11) ----
    //
    // Same shape as LegendaryArmorBlackForest.cs -- read that file's
    // header, and LegendaryWeaponsSwamp.cs in this same folder, for the
    // full rationale. Slot coverage: Helmet x2 (Draugr Elite, Blob),
    // Chest x3 (Draugr Elite, Blob, Draugr), Legs x2 (Blob, Draugr),
    // Shoulder x2 (Blob, Draugr), Shield x2 (Blob, Draugr).
    public static class LegendaryArmorSwamp
    {
        const string CreatureDraugrElite = "Draugr_Elite";
        const string CreatureBlob = "Blob";
        const string CreatureDraugr = "Draugr";
        const float DefaultArmorBonus = 5f;
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
            // Draugr Elite
            new Def { PrefabName = "CrownOfTheBarrowKing", DisplayName = "Crown of the Barrow-King", BaseItem = "HelmetIron", DropCreature = CreatureDraugrElite,
                Description = "A chieftain's crown, none the worse for centuries underground." },
            new Def { PrefabName = "MailOfTheDrownedChieftain", DisplayName = "Mail of the Drowned Chieftain", BaseItem = "ArmorIronChest", DropCreature = CreatureDraugrElite,
                Description = "Rusted just enough to look like it should've failed by now." },

            // Blob -- poison / ooze
            new Def { PrefabName = "MiasmicHood", DisplayName = "Miasmic Hood", BaseItem = "HelmetIron", DropCreature = CreatureBlob,
                Description = "Filters out most of the smell. Most." },
            new Def { PrefabName = "RobesOfTheBogwitch", DisplayName = "Robes of the Bogwitch", BaseItem = "ArmorIronChest", DropCreature = CreatureBlob,
                Description = "She doesn't need it anymore. You might." },
            new Def { PrefabName = "RotrootLeggings", DisplayName = "Rotroot Leggings", BaseItem = "ArmorIronLegs", DropCreature = CreatureBlob,
                Description = "Something in the weave keeps growing. It hasn't caused problems yet." },
            new Def { PrefabName = "CloakOfCreepingRot", DisplayName = "Cloak of Creeping Rot", BaseItem = "CapeLinen", DropCreature = CreatureBlob,
                Description = "The stains spread slowly enough not to worry about." },
            new Def { PrefabName = "OozeWardedBulwark", DisplayName = "Ooze-Warded Bulwark", BaseItem = "ShieldIronBuckler", DropCreature = CreatureBlob,
                Description = "Nothing sticks to it, which is more useful here than it sounds." },

            // Draugr -- regular
            new Def { PrefabName = "WightsHauberk", DisplayName = "Wight's Hauberk", BaseItem = "ArmorIronChest", DropCreature = CreatureDraugr,
                Description = "Still fits, somehow." },
            new Def { PrefabName = "GraveMarkedGreaves", DisplayName = "Grave-Marked Greaves", BaseItem = "ArmorIronLegs", DropCreature = CreatureDraugr,
                Description = "Worn by something that walked long after it should have stopped." },
            new Def { PrefabName = "ShroudOfTheFallen", DisplayName = "Shroud of the Fallen", BaseItem = "CapeTrollHide", DropCreature = CreatureDraugr,
                Description = "Once a burial shroud. Now just a cloak that's seen better centuries." },
            new Def { PrefabName = "BarrowWall", DisplayName = "Barrow Wall", BaseItem = "ShieldIronSquare", DropCreature = CreatureDraugr,
                Description = "Less a shield than a door that decided to fight back." },
        };

        static readonly Dictionary<string, ConfigEntry<float>> ArmorBonus = new Dictionary<string, ConfigEntry<float>>();
        static readonly Dictionary<string, ConfigEntry<float>> DropChance = new Dictionary<string, ConfigEntry<float>>();

        public static void BindConfig(ConfigFile config)
        {
            foreach (Def def in Defs)
            {
                ArmorBonus[def.PrefabName] = config.Bind(
                    "LegendaryArmorSwamp", $"{def.PrefabName}_ArmorBonus", DefaultArmorBonus,
                    $"Flat armor bonus for {def.DisplayName} (base: {def.BaseItem}).");

                DropChance[def.PrefabName] = config.Bind(
                    "LegendaryArmorSwamp", $"{def.PrefabName}_DropChance", DefaultDropChance,
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
                    Jotunn.Logger.LogError($"LegendaryArmorSwamp: '{def.PrefabName}' (base '{def.BaseItem}') is not valid, skipping");
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
                Jotunn.Logger.LogWarning($"LegendaryArmorSwamp: could not find CharacterDrop on '{creaturePrefabName}', skipping drop source for '{itemPrefabName}'");
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
