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
    // ---- 10 Legendary weapons, one per elemental theme (2026-09-11) ----
    //
    // User's own request: 10 hand-picked Legendary weapon concepts (a mix
    // of Fire/Frost/Poison/Lightning, Norse-lore names), all droppable
    // from Greydwarf in Meadows -- "throw them in the game for me to
    // change values on later in the editor panel." Same shape as
    // LightningSword.cs (clone + flat elemental bonus + Legendary tier +
    // drop-only), generalized into a data table so 10 near-identical
    // items don't need 10 near-identical files. Every tunable number
    // (bonus damage, drop chance) is its own Config.Bind entry, which the
    // DevTool's Values tab already surfaces generically for ANY plugin's
    // config -- no new editor plumbing needed for this to be tunable.
    //
    // Base items deliberately kept to Wood/Flint tier (confirmed real via
    // a live ObjectDB dump 2026-09-10/11, same session that already
    // learned the hard way that a Black-Forest-tier base -- SwordBronze,
    // Lightning Sword's own choice -- plus stacked bonuses one-shot a
    // gearless Meadows character). Sköll's Sting reuses the existing
    // Chain Lightning special effect verbatim (Core/Combat/
    // ChainLightningEffect.cs) rather than inventing a new ranged proc.
    // Every other weapon here is elemental-bonus-only for this pass,
    // deliberately -- signature procs (ignite burst, frost nova, execute-
    // on-poisoned, etc.) are real follow-up work, not faked in this batch.
    public static class LegendaryWeaponsBatch
    {
        const string DropCreature = "Greydwarf";
        // Halved 2026-09-11 (user's own call, pre-emptive nerf before
        // first in-game test) from an original 4 -- same instinct as the
        // Lightning Sword one-shot incident, applied before it could
        // repeat across 10 more items.
        const float DefaultBonusDamage = 2f;
        const float DefaultDropChance = 0.02f;

        class Def
        {
            public string PrefabName;
            public string DisplayName;
            public string Description;
            public string BaseItem;
            public string Element; // "fire", "frost", "poison", "lightning"
            public string SpecialEffect; // null = none
        }

        static readonly Def[] Defs =
        {
            new Def { PrefabName = "SurtrssonsEdge", DisplayName = "Surtrsson's Edge", BaseItem = "SwordWood", Element = "fire",
                Description = "A blade said to carry an ember from Muspelheim itself." },
            new Def { PrefabName = "YmirsBite", DisplayName = "Ymir's Bite", BaseItem = "AxeFlint", Element = "frost",
                Description = "Chipped from the frost giant's own bones -- it bites colder than steel should." },
            new Def { PrefabName = "JormungandrsFang", DisplayName = "Jormungandr's Fang", BaseItem = "KnifeWood", Element = "poison",
                Description = "A fang torn from the World Serpent, still weeping venom." },
            new Def { PrefabName = "SkadisFrostpoint", DisplayName = "Skadi's Frostpoint", BaseItem = "SpearFlint", Element = "frost",
                Description = "Blessed by the huntress of winter -- its point never warms." },
            new Def { PrefabName = "MuspelheimsWrath", DisplayName = "Muspelheim's Wrath", BaseItem = "SledgeWood", Element = "fire",
                Description = "A hammer that remembers the fire realm it was forged in." },
            new Def { PrefabName = "HelsReach", DisplayName = "Hel's Reach", BaseItem = "AtgeirWood", Element = "poison",
                Description = "Said to have swept the halls of the dead -- poison clings to its edge." },
            new Def { PrefabName = "SkollsSting", DisplayName = "Skoll's Sting", BaseItem = "Bow", Element = "lightning", SpecialEffect = WeaponSpecialEffectPatch.ChainLightning,
                Description = "Named for the wolf that chases the sun -- its arrows crackle with sky-fire." },
            new Def { PrefabName = "FenrirsHowl", DisplayName = "Fenrir's Howl", BaseItem = "SwordWood", Element = "frost",
                Description = "A blade as cold and relentless as the great wolf's howl." },
            new Def { PrefabName = "LokisEmber", DisplayName = "Loki's Ember", BaseItem = "KnifeFlint", Element = "fire",
                Description = "A trickster's blade, warm to the hand and hungry for a hidden strike." },
            new Def { PrefabName = "DraugrsCurse", DisplayName = "Draugr's Curse", BaseItem = "AxeWood", Element = "poison",
                Description = "Pulled from a barrow-wight's grip, still carrying the rot of the grave." },
        };

        static readonly Dictionary<string, ConfigEntry<float>> BonusDamage = new Dictionary<string, ConfigEntry<float>>();
        static readonly Dictionary<string, ConfigEntry<float>> DropChance = new Dictionary<string, ConfigEntry<float>>();

        public static void BindConfig(ConfigFile config)
        {
            foreach (Def def in Defs)
            {
                BonusDamage[def.PrefabName] = config.Bind(
                    "LegendaryWeapons", $"{def.PrefabName}_BonusDamage", DefaultBonusDamage,
                    $"Flat {def.Element} damage bonus for {def.DisplayName} (base: {def.BaseItem}).");

                DropChance[def.PrefabName] = config.Bind(
                    "LegendaryWeapons", $"{def.PrefabName}_DropChance", DefaultDropChance,
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
                    Jotunn.Logger.LogError($"LegendaryWeaponsBatch: '{def.PrefabName}' (base '{def.BaseItem}') is not valid, skipping");
                    continue;
                }

                item.Recipe = null; // drop-only, same as LightningSword.cs

                ItemDrop.ItemData itemData = item.ItemDrop.m_itemData;
                ItemDrop.ItemData.SharedData shared = itemData.m_shared;

                ApplyElementBonus(ref shared.m_damages, def.Element, BonusDamage[def.PrefabName].Value);

                if (!string.IsNullOrEmpty(def.SpecialEffect))
                {
                    if (itemData.m_customData == null) itemData.m_customData = new Dictionary<string, string>();
                    itemData.m_customData[WeaponSpecialEffectPatch.EffectKey] = def.SpecialEffect;
                }

                ItemManager.Instance.AddItem(item);
                ItemRollTrigger.Register(shared, RarityTier.Legendary, RarityLootPlugin.LegendaryAffixCount.Value);

                RegisterDropOn(DropCreature, def.PrefabName, DropChance[def.PrefabName].Value);
            }
        }

        static void ApplyElementBonus(ref HitData.DamageTypes damages, string element, float bonus)
        {
            switch (element)
            {
                case "fire": damages.m_fire += bonus; break;
                case "frost": damages.m_frost += bonus; break;
                case "poison": damages.m_poison += bonus; break;
                case "lightning": damages.m_lightning += bonus; break;
            }
        }

        // Same shape as LightningSword.cs's own RegisterDropOn -- kept as
        // a private copy here rather than a shared helper, since it's a
        // handful of lines and this file is meant to stay self-contained.
        static void RegisterDropOn(string creaturePrefabName, string itemPrefabName, float chance)
        {
            GameObject creaturePrefab = PrefabManager.Instance.GetPrefab(creaturePrefabName);
            CharacterDrop drop = creaturePrefab != null ? creaturePrefab.GetComponent<CharacterDrop>() : null;
            if (drop == null)
            {
                Jotunn.Logger.LogWarning($"LegendaryWeaponsBatch: could not find CharacterDrop on '{creaturePrefabName}', skipping drop source for '{itemPrefabName}'");
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
