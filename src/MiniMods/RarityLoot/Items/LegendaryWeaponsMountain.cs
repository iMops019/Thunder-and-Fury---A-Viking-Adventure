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
    // ---- 11 Legendary weapons for Mountain (2026-09-11) ----
    //
    // Same shape as LegendaryWeaponsSwamp.cs -- read that file's header
    // for the full rationale. Same three-way split, this biome's own
    // creatures: Golem gets the unique/lore-arc set (2, physical --
    // ancient stone giant, matches the Troll/Draugr Elite "big and
    // primitive" role), Drake gets the elemental set (5, all frost --
    // its own vanilla identity as a frost-breathing wyrm), Wolf gets the
    // melee/no-element set (4, regular pack predator). Bases are Silver/
    // Wolf-Fang tier -- Mountain's own real material ceiling. Real
    // vanilla weapon variety at this tier is narrower than lower biomes
    // (no Silver Atgeir/Axe/Sledge/Bow exist) -- Iron-tier bases are
    // reused where Silver has no native option, same accepted pattern as
    // reusing Bronze/Iron bases across earlier biomes.
    public static class LegendaryWeaponsMountain
    {
        const string CreatureGolem = "Golem";
        const string CreatureDrake = "Drake";
        const string CreatureWolf = "Wolf";
        const float DefaultBonusDamage = 8f;
        const float DefaultDropChance = 0.02f;

        class Def
        {
            public string PrefabName;
            public string DisplayName;
            public string Description;
            public string BaseItem;
            public string Element; // "frost" or "physical"
            public string DropCreature;
        }

        static readonly Def[] Defs =
        {
            // Golem -- ancient stone giant, physical, lore-arc
            new Def { PrefabName = "HrungnirsHeart", DisplayName = "Hrungnir's Heart", BaseItem = "MaceSilver", Element = "physical", DropCreature = CreatureGolem,
                Description = "Named for the stone giant of old -- it hits like the mountain remembers him." },
            new Def { PrefabName = "StoneboundWarhammer", DisplayName = "Stonebound Warhammer", BaseItem = "SledgeIron", Element = "physical", DropCreature = CreatureGolem,
                Description = "Cracked from a golem's own fist and never quite cooled." },

            // Drake -- frost
            new Def { PrefabName = "UllrsChill", DisplayName = "Ullr's Chill", BaseItem = "SwordSilver", Element = "frost", DropCreature = CreatureDrake,
                Description = "Named for the winter god of the hunt -- it never quite warms in the hand." },
            new Def { PrefabName = "WinterWyrmsBreath", DisplayName = "Winter-Wyrm's Breath", BaseItem = "KnifeSilver", Element = "frost", DropCreature = CreatureDrake,
                Description = "Carries a chill that shouldn't survive being drawn from its sheath." },
            new Def { PrefabName = "FrostboundReach", DisplayName = "Frostbound Reach", BaseItem = "AtgeirIron", Element = "frost", DropCreature = CreatureDrake,
                Description = "Frost creeps up the shaft with every strike." },
            new Def { PrefabName = "RimeCaller", DisplayName = "Rime-Caller", BaseItem = "BowFineWood", Element = "frost", DropCreature = CreatureDrake,
                Description = "Its arrows leave frost on the wind long after they land." },
            new Def { PrefabName = "GlaciersMaw", DisplayName = "Glacier's Maw", BaseItem = "MaceSilver", Element = "frost", DropCreature = CreatureDrake,
                Description = "Strikes with the weight of something that's been cold for a very long time." },

            // Wolf -- regular, physical/melee
            new Def { PrefabName = "HatisFang", DisplayName = "Hati's Fang", BaseItem = "SpearWolfFang", Element = "physical", DropCreature = CreatureWolf,
                Description = "Named for the wolf that chases the moon -- fitting, from a wolf that chased something else." },
            new Def { PrefabName = "FenrirsClaw", DisplayName = "Fenrir's Claw", BaseItem = "AxeIron", Element = "physical", DropCreature = CreatureWolf,
                Description = "Carved to look like it, at least. Close enough in a fight." },
            new Def { PrefabName = "MoonhowlBlade", DisplayName = "Moonhowl Blade", BaseItem = "SwordSilver", Element = "physical", DropCreature = CreatureWolf,
                Description = "Said to sing faintly under a full moon. Mostly it just cuts well." },
            new Def { PrefabName = "PackleadersBite", DisplayName = "Packleader's Bite", BaseItem = "KnifeSilver", Element = "physical", DropCreature = CreatureWolf,
                Description = "Small, fast, and used to finishing what the pack started." },
        };

        static readonly Dictionary<string, ConfigEntry<float>> BonusDamage = new Dictionary<string, ConfigEntry<float>>();
        static readonly Dictionary<string, ConfigEntry<float>> DropChance = new Dictionary<string, ConfigEntry<float>>();

        public static void BindConfig(ConfigFile config)
        {
            foreach (Def def in Defs)
            {
                BonusDamage[def.PrefabName] = config.Bind(
                    "LegendaryWeaponsMountain", $"{def.PrefabName}_BonusDamage", DefaultBonusDamage,
                    $"Flat {def.Element} damage bonus for {def.DisplayName} (base: {def.BaseItem}).");

                DropChance[def.PrefabName] = config.Bind(
                    "LegendaryWeaponsMountain", $"{def.PrefabName}_DropChance", DefaultDropChance,
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
                    Jotunn.Logger.LogError($"LegendaryWeaponsMountain: '{def.PrefabName}' (base '{def.BaseItem}') is not valid, skipping");
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
                Jotunn.Logger.LogWarning($"LegendaryWeaponsMountain: could not find CharacterDrop on '{creaturePrefabName}', skipping drop source for '{itemPrefabName}'");
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
