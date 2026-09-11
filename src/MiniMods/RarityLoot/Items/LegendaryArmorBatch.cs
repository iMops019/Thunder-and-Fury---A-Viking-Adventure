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
    // ---- 11 Legendary armor pieces (2026-09-11) ----
    //
    // User's own request: at least one of every armor slot, a couple of
    // shields, and "a couple different body types" -- built as two loose
    // sets (Brynhild: light/valkyrie, Odin/Leather/Linen-based; Ulfhednar:
    // heavier wolf-warrior, TrollLeather/Wolf-cape-based) plus three
    // standalone pieces, so gearing into one full look is possible but
    // never required. Same shape and same reasoning as
    // LegendaryWeaponsBatch.cs in this same folder -- read that file's
    // header for the full rationale (data table over 11 near-identical
    // files, Config.Bind per tunable number for free Values-tab editing,
    // Wood/Flint/Leather-tier bases only after the Lightning Sword
    // power-spike lesson). All base names confirmed real via the same
    // live ObjectDB dump, including two happy finds that already fit the
    // lore perfectly by name alone: "HelmetOdin" and "CapeFeather"
    // (Freyja's mythological feathered cloak).
    //
    // Armor bonus is additive (+N flat), not a multiplier -- these bases
    // have very low starting armor (Leather/TrollLeather chest+legs are
    // 2 and 6 respectively), so a multiplier would either do nothing at
    // low values or swing wildly; a small flat bonus is easier to reason
    // about and to tune later.
    public static class LegendaryArmorBatch
    {
        const string DropCreature = "Greydwarf";
        // Halved 2026-09-11 (user's own call, pre-emptive nerf before
        // first in-game test) from an original 3.
        const float DefaultArmorBonus = 1.5f;
        const float DefaultDropChance = 0.02f;

        class Def
        {
            public string PrefabName;
            public string DisplayName;
            public string Description;
            public string BaseItem;
        }

        static readonly Def[] Defs =
        {
            // Brynhild's line -- light, valkyrie-themed
            new Def { PrefabName = "OdinsSight", DisplayName = "Odin's Sight", BaseItem = "HelmetOdin",
                Description = "A wide-brimmed hood said to grant the All-Father's own vision." },
            new Def { PrefabName = "BrynhildsWard", DisplayName = "Brynhild's Ward", BaseItem = "ArmorLeatherChest",
                Description = "Worn by a valkyrie who chose the fallen -- it has never once failed to protect a wearer worthy of it." },
            new Def { PrefabName = "BrynhildsGreaves", DisplayName = "Brynhild's Greaves", BaseItem = "ArmorLeatherLegs",
                Description = "Light enough to run in, strong enough to matter." },
            new Def { PrefabName = "CloakOfTheValkyrie", DisplayName = "Cloak of the Valkyrie", BaseItem = "CapeLinen",
                Description = "Woven from something finer than flax, if the old stories are true." },

            // Ulfhednar's line -- heavier, wolf-warrior/berserker-themed
            new Def { PrefabName = "SkullHelmOfTheUlfhednar", DisplayName = "Skull-Helm of the Ulfhednar", BaseItem = "HelmetTrollLeather",
                Description = "Worn by the wolf-skinned warriors who fought without fear -- or without sense." },
            new Def { PrefabName = "PeltOfTheUlfhednar", DisplayName = "Pelt of the Ulfhednar", BaseItem = "ArmorTrollLeatherChest",
                Description = "Still smells faintly of the beast it once was." },
            new Def { PrefabName = "UlfhednarsLegguards", DisplayName = "Ulfhednar's Legguards", BaseItem = "ArmorTrollLeatherLegs",
                Description = "Built for a charge, not a retreat." },
            new Def { PrefabName = "MantleOfTheBearSark", DisplayName = "Mantle of the Bear-Sark", BaseItem = "CapeWolf",
                Description = "A berserker's mantle, said to lend the wearer a beast's own fury." },

            // Standalone pieces
            new Def { PrefabName = "FreyjasFeatherCloak", DisplayName = "Freyja's Feather Cloak", BaseItem = "CapeFeather",
                Description = "Legend says Freyja herself once wore a cloak of feathers to cross the Nine Realms unseen." },
            new Def { PrefabName = "AegisOfThor", DisplayName = "Aegis of Thor", BaseItem = "ShieldWood",
                Description = "It has caught a great many blows meant for someone else." },
            new Def { PrefabName = "YmirsRib", DisplayName = "Ymir's Rib", BaseItem = "ShieldBronzeBuckler",
                Description = "Carved from the frost giant whose bones shaped the nine worlds." },
        };

        static readonly Dictionary<string, ConfigEntry<float>> ArmorBonus = new Dictionary<string, ConfigEntry<float>>();
        static readonly Dictionary<string, ConfigEntry<float>> DropChance = new Dictionary<string, ConfigEntry<float>>();

        public static void BindConfig(ConfigFile config)
        {
            foreach (Def def in Defs)
            {
                ArmorBonus[def.PrefabName] = config.Bind(
                    "LegendaryArmor", $"{def.PrefabName}_ArmorBonus", DefaultArmorBonus,
                    $"Flat armor bonus for {def.DisplayName} (base: {def.BaseItem}).");

                DropChance[def.PrefabName] = config.Bind(
                    "LegendaryArmor", $"{def.PrefabName}_DropChance", DefaultDropChance,
                    $"Chance per {DropCreature} kill for {def.DisplayName} to drop. {DefaultDropChance * 100f:0.#}% default.");
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
                    Jotunn.Logger.LogError($"LegendaryArmorBatch: '{def.PrefabName}' (base '{def.BaseItem}') is not valid, skipping");
                    continue;
                }

                item.Recipe = null; // drop-only

                ItemDrop.ItemData.SharedData shared = item.ItemDrop.m_itemData.m_shared;
                shared.m_armor += ArmorBonus[def.PrefabName].Value;

                ItemManager.Instance.AddItem(item);
                ItemRollTrigger.Register(shared, RarityTier.Legendary, RarityLootPlugin.LegendaryAffixCount.Value);

                RegisterDropOn(DropCreature, def.PrefabName, DropChance[def.PrefabName].Value);
            }
        }

        static void RegisterDropOn(string creaturePrefabName, string itemPrefabName, float chance)
        {
            GameObject creaturePrefab = PrefabManager.Instance.GetPrefab(creaturePrefabName);
            CharacterDrop drop = creaturePrefab != null ? creaturePrefab.GetComponent<CharacterDrop>() : null;
            if (drop == null)
            {
                Jotunn.Logger.LogWarning($"LegendaryArmorBatch: could not find CharacterDrop on '{creaturePrefabName}', skipping drop source for '{itemPrefabName}'");
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
