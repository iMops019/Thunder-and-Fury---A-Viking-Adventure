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
    // ---- 11 Legendary weapons for Swamp (2026-09-11) ----
    //
    // Same shape as LegendaryWeaponsBlackForest.cs -- read that file's
    // header for the full rationale. Same three-way split pattern, this
    // biome's own creatures: Draugr Elite gets the "unique/lore-arc"
    // small set (2), Blob gets the elemental set (5, all poison --
    // matches its own vanilla identity as a poison ooze rather than
    // forcing fire/frost onto a swamp creature that has neither),
    // regular Draugr gets the melee/no-element set (4). Skeleton gets
    // nothing, consistent with Black Forest. Bases are Iron/Elderbark
    // tier -- Swamp's own real material ceiling, confirmed real via the
    // same live ObjectDB dump used for the earlier batches.
    //
    // Creature name caveat: "Draugr" and "Blob" are Swamp's own
    // well-known creatures and near-certainly correct. "Draugr_Elite" is
    // a reasonable, standard naming guess (matches the Greydwarf_Elite
    // pattern already used for Black Forest) but not independently
    // decompile-confirmed this session -- same safe failure mode as
    // before if wrong.
    public static class LegendaryWeaponsSwamp
    {
        const string CreatureDraugrElite = "Draugr_Elite";
        const string CreatureBlob = "Blob";
        const string CreatureDraugr = "Draugr";
        const float DefaultBonusDamage = 6f;
        const float DefaultDropChance = 0.02f;

        class Def
        {
            public string PrefabName;
            public string DisplayName;
            public string Description;
            public string BaseItem;
            public string Element; // "poison" or "physical"
            public string DropCreature;
        }

        static readonly Def[] Defs =
        {
            // Draugr Elite -- barrow-lord, physical, lore-arc
            new Def { PrefabName = "HaugbuisJudgment", DisplayName = "Haugbui's Judgment", BaseItem = "MaceIron", Element = "physical", DropCreature = CreatureDraugrElite,
                Description = "Named for the howe-dweller that once swung it -- it hasn't forgotten how." },
            new Def { PrefabName = "BarrowKingsBlade", DisplayName = "Barrow-King's Blade", BaseItem = "SwordIron", Element = "physical", DropCreature = CreatureDraugrElite,
                Description = "A chieftain's blade, buried with him and unwilling to stay that way." },

            // Blob -- poison / ooze
            new Def { PrefabName = "NidhoggsVenom", DisplayName = "Nidhogg's Venom", BaseItem = "SpearElderbark", Element = "poison", DropCreature = CreatureBlob,
                Description = "Named for the serpent that gnaws at the world tree's roots." },
            new Def { PrefabName = "BogmiresBite", DisplayName = "Bogmire's Bite", BaseItem = "AxeIron", Element = "poison", DropCreature = CreatureBlob,
                Description = "Pulled from the muck, still weeping something that isn't water." },
            new Def { PrefabName = "RotcallersReach", DisplayName = "Rotcaller's Reach", BaseItem = "AtgeirIron", Element = "poison", DropCreature = CreatureBlob,
                Description = "It sweeps wide, and the rot follows where it points." },
            new Def { PrefabName = "MiasmasEdge", DisplayName = "Miasma's Edge", BaseItem = "KnifeCopper", Element = "poison", DropCreature = CreatureBlob,
                Description = "A short blade that carries the swamp's own sickness." },
            new Def { PrefabName = "Plaguebringer", DisplayName = "Plaguebringer", BaseItem = "MaceIron", Element = "poison", DropCreature = CreatureBlob,
                Description = "Every blow leaves something behind that keeps hurting." },

            // Draugr -- regular, physical/melee
            new Def { PrefabName = "GraveWardensMaul", DisplayName = "Grave-Warden's Maul", BaseItem = "SledgeIron", Element = "physical", DropCreature = CreatureDraugr,
                Description = "Kept the crypts quiet for longer than anyone living remembers." },
            new Def { PrefabName = "FallenLegionsAxe", DisplayName = "Fallen Legion's Axe", BaseItem = "AxeIron", Element = "physical", DropCreature = CreatureDraugr,
                Description = "One of many, once. The rest didn't make it out of the bog." },
            new Def { PrefabName = "WightsReckoning", DisplayName = "Wight's Reckoning", BaseItem = "SwordIron", Element = "physical", DropCreature = CreatureDraugr,
                Description = "Rusted at the edges, sharp enough where it counts." },
            new Def { PrefabName = "BoglandReaver", DisplayName = "Bogland Reaver", BaseItem = "AtgeirIron", Element = "physical", DropCreature = CreatureDraugr,
                Description = "Built for wading through muck and whatever's waiting in it." },
        };

        static readonly Dictionary<string, ConfigEntry<float>> BonusDamage = new Dictionary<string, ConfigEntry<float>>();
        static readonly Dictionary<string, ConfigEntry<float>> DropChance = new Dictionary<string, ConfigEntry<float>>();

        public static void BindConfig(ConfigFile config)
        {
            foreach (Def def in Defs)
            {
                BonusDamage[def.PrefabName] = config.Bind(
                    "LegendaryWeaponsSwamp", $"{def.PrefabName}_BonusDamage", DefaultBonusDamage,
                    $"Flat {def.Element} damage bonus for {def.DisplayName} (base: {def.BaseItem}).");

                DropChance[def.PrefabName] = config.Bind(
                    "LegendaryWeaponsSwamp", $"{def.PrefabName}_DropChance", DefaultDropChance,
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
                    Jotunn.Logger.LogError($"LegendaryWeaponsSwamp: '{def.PrefabName}' (base '{def.BaseItem}') is not valid, skipping");
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
                case "poison": damages.m_poison += bonus; break;
                case "physical": damages.m_damage += bonus; break;
            }
        }

        static void RegisterDropOn(string creaturePrefabName, string itemPrefabName, float chance)
        {
            GameObject creaturePrefab = PrefabManager.Instance.GetPrefab(creaturePrefabName);
            CharacterDrop drop = creaturePrefab != null ? creaturePrefab.GetComponent<CharacterDrop>() : null;
            if (drop == null)
            {
                Jotunn.Logger.LogWarning($"LegendaryWeaponsSwamp: could not find CharacterDrop on '{creaturePrefabName}', skipping drop source for '{itemPrefabName}'");
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
