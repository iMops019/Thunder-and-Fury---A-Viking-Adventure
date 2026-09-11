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
    // ---- 11 Legendary weapons for Deep North (2026-09-11) ----
    //
    // Same shape as LegendaryWeaponsAshlands.cs -- read that file's
    // header for the full rationale. Same three-way split: Fenring gets
    // the unique/lore-arc set (2, physical -- and nicely closes out the
    // Fenrir/Hati/Sköll wolf-god thread running through Meadows'
    // "Fenrir's Howl" and Mountain's "Hati's Fang"/"Cloak of Hati"),
    // Volture gets the elemental set (5, all frost -- Deep North's own
    // biome-wide theme), Urchin gets the melee/no-element set (4,
    // regular ground threat). No Deep-North-specific WEAPON tier was
    // found in the live dump the way DeepNorth ARMOR bases were
    // (ArmorDeepNorthHeavy/Medium/Mage, confirmed real) -- "Gold" tier
    // (already used for Mistlands) is reused for weapons here, the same
    // accepted "no better native option" pattern used for Iron across
    // several earlier biomes.
    //
    // Confidence caveat (stronger than earlier biomes): Deep North is
    // the newest Valheim content this project has touched, and "Fenring"/
    // "Volture"/"Urchin" as this biome's own creature names are a
    // reasonable best-effort rather than independently decompile- or
    // dump-confirmed the way Seeker/Gjall/Morgen/Twitcher were. Same
    // safe-failure fallback either way: a wrong name just means that
    // drop source doesn't register, logged, nothing else breaks, and the
    // items themselves still exist and are spawnable via the Spawn tab.
    public static class LegendaryWeaponsDeepNorth
    {
        const string CreatureFenring = "Fenring";
        const string CreatureVolture = "Volture";
        const string CreatureUrchin = "Urchin";
        const float DefaultBonusDamage = 16f;
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
            // Fenring -- physical, lore-arc
            new Def { PrefabName = "FenringsReckoning", DisplayName = "Fenring's Reckoning", BaseItem = "SwordGold", Element = "physical", DropCreature = CreatureFenring,
                Description = "The last of a long line of wolves that never stopped chasing something." },
            new Def { PrefabName = "FrostfangCleaver", DisplayName = "Frostfang Cleaver", BaseItem = "AxeGold", Element = "physical", DropCreature = CreatureFenring,
                Description = "Carries the cold of the ice it was pulled from." },

            // Volture -- frost
            new Def { PrefabName = "SkydiversChill", DisplayName = "Skydiver's Chill", BaseItem = "BowGold", Element = "frost", DropCreature = CreatureVolture,
                Description = "Its arrows fall as cold as the wind that carried them." },
            new Def { PrefabName = "WyrmwindFang", DisplayName = "Wyrmwind Fang", BaseItem = "KnifeGold", Element = "frost", DropCreature = CreatureVolture,
                Description = "Small, fast, and always a little colder than the air around it." },
            new Def { PrefabName = "TalonOfTheNorth", DisplayName = "Talon of the North", BaseItem = "SpearGold", Element = "frost", DropCreature = CreatureVolture,
                Description = "Struck from above often enough that the wound never fully closes." },
            new Def { PrefabName = "GlacierbornMace", DisplayName = "Glacierborn Mace", BaseItem = "MaceGold", Element = "frost", DropCreature = CreatureVolture,
                Description = "Heavier than ice has any business being." },
            new Def { PrefabName = "Frostreach", DisplayName = "Frostreach", BaseItem = "AtgeirGold", Element = "frost", DropCreature = CreatureVolture,
                Description = "Keeps the cold at a distance the wielder controls." },

            // Urchin -- regular, physical/melee
            new Def { PrefabName = "Spinewrack", DisplayName = "Spinewrack", BaseItem = "MaceGold", Element = "physical", DropCreature = CreatureUrchin,
                Description = "Studded with something that used to be attached to its owner." },
            new Def { PrefabName = "IcequillBlade", DisplayName = "Icequill Blade", BaseItem = "SwordGold", Element = "physical", DropCreature = CreatureUrchin,
                Description = "Edged like the thing it's named for -- sharp in more places than expected." },
            new Def { PrefabName = "FrostspineAxe", DisplayName = "Frostspine Axe", BaseItem = "AxeGold", Element = "physical", DropCreature = CreatureUrchin,
                Description = "Every notch in the blade tells a story nobody asked for." },
            new Def { PrefabName = "BarbedReach", DisplayName = "Barbed Reach", BaseItem = "AtgeirGold", Element = "physical", DropCreature = CreatureUrchin,
                Description = "Not subtle. Doesn't need to be, out here." },
        };

        static readonly Dictionary<string, ConfigEntry<float>> BonusDamage = new Dictionary<string, ConfigEntry<float>>();
        static readonly Dictionary<string, ConfigEntry<float>> DropChance = new Dictionary<string, ConfigEntry<float>>();

        public static void BindConfig(ConfigFile config)
        {
            foreach (Def def in Defs)
            {
                BonusDamage[def.PrefabName] = config.Bind(
                    "LegendaryWeaponsDeepNorth", $"{def.PrefabName}_BonusDamage", DefaultBonusDamage,
                    $"Flat {def.Element} damage bonus for {def.DisplayName} (base: {def.BaseItem}).");

                DropChance[def.PrefabName] = config.Bind(
                    "LegendaryWeaponsDeepNorth", $"{def.PrefabName}_DropChance", DefaultDropChance,
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
                    Jotunn.Logger.LogError($"LegendaryWeaponsDeepNorth: '{def.PrefabName}' (base '{def.BaseItem}') is not valid, skipping");
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
                Jotunn.Logger.LogWarning($"LegendaryWeaponsDeepNorth: could not find CharacterDrop on '{creaturePrefabName}', skipping drop source for '{itemPrefabName}'");
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
