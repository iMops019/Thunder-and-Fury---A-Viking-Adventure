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
    // ---- 11 Legendary armor pieces for Plains (2026-09-11) ----
    //
    // Same shape as LegendaryArmorMountain.cs -- read that file's
    // header, and LegendaryWeaponsPlains.cs in this same folder, for the
    // full rationale. Slot coverage: Helmet x2 (Lox, Shaman), Chest x3
    // (Lox, Shaman, Fuling), Legs x2 (Shaman, Fuling), Shoulder x2
    // (Shaman, Fuling), Shield x2 (Shaman, Fuling). Padded-tier bases
    // (Linen Thread + Black Metal, the real vanilla Plains armor set)
    // throughout, plus `CapeLox` for Fuling's cloak -- a real vanilla
    // cape whose own name happens to fit neither its actual source
    // material nor this drop mapping perfectly, but it's the closest
    // real Plains-flavored cape available.
    public static class LegendaryArmorPlains
    {
        const string CreatureLox = "Lox";
        const string CreatureFulingShaman = "Fuling_Shaman";
        const string CreatureFuling = "Fuling";
        const float DefaultArmorBonus = 7f;
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
            // Lox
            new Def { PrefabName = "LoxhideCrown", DisplayName = "Loxhide Crown", BaseItem = "HelmetPadded", DropCreature = CreatureLox,
                Description = "Thick enough that most things bounce off before they land." },
            new Def { PrefabName = "LoxhidePlate", DisplayName = "Loxhide Plate", BaseItem = "ArmorPaddedCuirass", DropCreature = CreatureLox,
                Description = "Padded with hide from something that didn't feel most of what hit it." },

            // Fuling Shaman -- fire
            new Def { PrefabName = "SunscorchCirclet", DisplayName = "Sunscorch Circlet", BaseItem = "HelmetPadded", DropCreature = CreatureFulingShaman,
                Description = "Worn by a caster who liked to be seen coming." },
            new Def { PrefabName = "RobeOfTheFirecaller", DisplayName = "Robe of the Firecaller", BaseItem = "ArmorPaddedCuirass", DropCreature = CreatureFulingShaman,
                Description = "Singed at the edges, from the inside." },
            new Def { PrefabName = "CinderwrapGreaves", DisplayName = "Cinderwrap Greaves", BaseItem = "ArmorPaddedGreaves", DropCreature = CreatureFulingShaman,
                Description = "Warm to the touch, always." },
            new Def { PrefabName = "MantleOfEmbers", DisplayName = "Mantle of Embers", BaseItem = "CapeLinen", DropCreature = CreatureFulingShaman,
                Description = "Doesn't burn. Nobody's quite sure why." },
            new Def { PrefabName = "SunfireWard", DisplayName = "Sunfire Ward", BaseItem = "ShieldBlackmetal", DropCreature = CreatureFulingShaman,
                Description = "Reflects firelight strangely, like it's remembering something." },

            // Fuling -- regular
            new Def { PrefabName = "RaidersWarplate", DisplayName = "Raider's Warplate", BaseItem = "ArmorPaddedCuirass", DropCreature = CreatureFuling,
                Description = "Stitched together from whatever the warband could carry off." },
            new Def { PrefabName = "WarcampGreaves", DisplayName = "Warcamp Greaves", BaseItem = "ArmorPaddedGreaves", DropCreature = CreatureFuling,
                Description = "Kicked up more dust than most, once." },
            new Def { PrefabName = "CloakOfTheGoblinKing", DisplayName = "Cloak of the Goblin-King", BaseItem = "CapeLox", DropCreature = CreatureFuling,
                Description = "Claimed from something well above its original owner's pay grade." },
            new Def { PrefabName = "TotemBulwark", DisplayName = "Totem Bulwark", BaseItem = "ShieldBlackmetal", DropCreature = CreatureFuling,
                Description = "Carved with symbols nobody outside the warband can read." },
        };

        static readonly Dictionary<string, ConfigEntry<float>> ArmorBonus = new Dictionary<string, ConfigEntry<float>>();
        static readonly Dictionary<string, ConfigEntry<float>> DropChance = new Dictionary<string, ConfigEntry<float>>();

        public static void BindConfig(ConfigFile config)
        {
            foreach (Def def in Defs)
            {
                ArmorBonus[def.PrefabName] = config.Bind(
                    "LegendaryArmorPlains", $"{def.PrefabName}_ArmorBonus", DefaultArmorBonus,
                    $"Flat armor bonus for {def.DisplayName} (base: {def.BaseItem}).");

                DropChance[def.PrefabName] = config.Bind(
                    "LegendaryArmorPlains", $"{def.PrefabName}_DropChance", DefaultDropChance,
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
                    Jotunn.Logger.LogError($"LegendaryArmorPlains: '{def.PrefabName}' (base '{def.BaseItem}') is not valid, skipping");
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
                Jotunn.Logger.LogWarning($"LegendaryArmorPlains: could not find CharacterDrop on '{creaturePrefabName}', skipping drop source for '{itemPrefabName}'");
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
