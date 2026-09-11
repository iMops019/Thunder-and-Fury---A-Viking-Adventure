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
    // ---- 11 Legendary weapons for Plains (2026-09-11) ----
    //
    // Same shape as LegendaryWeaponsMountain.cs -- read that file's
    // header for the full rationale. Same three-way split: Lox gets the
    // unique/lore-arc set (2, physical -- the biome's own giant-tank
    // creature), Fuling Shaman gets the elemental set (5, all fire --
    // matches its own vanilla identity as the fireball-throwing caster),
    // Fuling gets the melee/no-element set (4, regular raider). Bases
    // are Black Metal/Needle tier -- Plains' own real material ceiling.
    // No native Plains-tier Spear exists in vanilla, so that weapon type
    // is skipped this biome rather than reused from a lower tier for no
    // reason.
    public static class LegendaryWeaponsPlains
    {
        const string CreatureLox = "Lox";
        const string CreatureFulingShaman = "Fuling_Shaman";
        const string CreatureFuling = "Fuling";
        const float DefaultBonusDamage = 10f;
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
            // Lox -- giant, physical, lore-arc
            new Def { PrefabName = "LoxheartCrusher", DisplayName = "Loxheart Crusher", BaseItem = "MaceNeedle", Element = "physical", DropCreature = CreatureLox,
                Description = "Swung with the weight of something that used to be much slower to anger." },
            new Def { PrefabName = "Groundshaker", DisplayName = "Groundshaker", BaseItem = "BattleaxeBlackmetal", Element = "physical", DropCreature = CreatureLox,
                Description = "Every swing lands like the ground itself is annoyed." },

            // Fuling Shaman -- fire
            new Def { PrefabName = "SunscorchBlade", DisplayName = "Sunscorch Blade", BaseItem = "SwordBlackmetal", Element = "fire", DropCreature = CreatureFulingShaman,
                Description = "Still smoldering from whatever ritual last touched it." },
            new Def { PrefabName = "Cinderfang", DisplayName = "Cinderfang", BaseItem = "KnifeBlackMetal", Element = "fire", DropCreature = CreatureFulingShaman,
                Description = "Small enough to hide, hot enough to matter." },
            new Def { PrefabName = "Emberreach", DisplayName = "Emberreach", BaseItem = "AtgeirBlackmetal", Element = "fire", DropCreature = CreatureFulingShaman,
                Description = "Trails a thin line of smoke with every strike." },
            new Def { PrefabName = "WildfiresCall", DisplayName = "Wildfire's Call", BaseItem = "BowHuntsman", Element = "fire", DropCreature = CreatureFulingShaman,
                Description = "Its arrows arrive already burning." },
            new Def { PrefabName = "Scorchedge", DisplayName = "Scorchedge", BaseItem = "AxeBlackMetal", Element = "fire", DropCreature = CreatureFulingShaman,
                Description = "The head never fully cools, even left out overnight." },

            // Fuling -- regular, physical/melee
            new Def { PrefabName = "RaidleadersEdge", DisplayName = "Raidleader's Edge", BaseItem = "SwordBlackmetal", Element = "physical", DropCreature = CreatureFuling,
                Description = "Carried by whoever's shouting the loudest at the front of the raid." },
            new Def { PrefabName = "GoblinKingsCleaver", DisplayName = "Goblin-King's Cleaver", BaseItem = "AxeBlackMetal", Element = "physical", DropCreature = CreatureFuling,
                Description = "Too big for most Fulings. That's rather the point." },
            new Def { PrefabName = "WarcampRender", DisplayName = "Warcamp Render", BaseItem = "AtgeirBlackmetal", Element = "physical", DropCreature = CreatureFuling,
                Description = "Keeps the totem's guards at a distance they don't appreciate." },
            new Def { PrefabName = "TotemBreaker", DisplayName = "Totem-Breaker", BaseItem = "MaceNeedle", Element = "physical", DropCreature = CreatureFuling,
                Description = "Built for smashing things that are supposed to be sacred." },
        };

        static readonly Dictionary<string, ConfigEntry<float>> BonusDamage = new Dictionary<string, ConfigEntry<float>>();
        static readonly Dictionary<string, ConfigEntry<float>> DropChance = new Dictionary<string, ConfigEntry<float>>();

        public static void BindConfig(ConfigFile config)
        {
            foreach (Def def in Defs)
            {
                BonusDamage[def.PrefabName] = config.Bind(
                    "LegendaryWeaponsPlains", $"{def.PrefabName}_BonusDamage", DefaultBonusDamage,
                    $"Flat {def.Element} damage bonus for {def.DisplayName} (base: {def.BaseItem}).");

                DropChance[def.PrefabName] = config.Bind(
                    "LegendaryWeaponsPlains", $"{def.PrefabName}_DropChance", DefaultDropChance,
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
                    Jotunn.Logger.LogError($"LegendaryWeaponsPlains: '{def.PrefabName}' (base '{def.BaseItem}') is not valid, skipping");
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
                Jotunn.Logger.LogWarning($"LegendaryWeaponsPlains: could not find CharacterDrop on '{creaturePrefabName}', skipping drop source for '{itemPrefabName}'");
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
