using System.Collections.Generic;
using System.IO;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using ThunderFury.Core.Combat;

namespace ThunderFury.DevTool.Data
{
    // ---- Item Creator: load, persist, and register DevTool-authored items ----
    //
    // Registration follows the exact same shape as StonePickaxe.cs and
    // VoltunsSet.cs -- CustomItem(name, basePrefabName, ItemConfig) clones
    // a real vanilla item wholesale, then stat multipliers are applied
    // directly to the cloned SharedData afterward, relative to whatever
    // the real value turns out to be. This just generalizes that pattern
    // from a hand-written .cs file per item into data.
    //
    // Persistence via UnityEngine.JsonUtility rather than a JSON NuGet
    // package -- already available through the Unity/Jotunn reference,
    // no new dependency, and JsonUtility needs a wrapper object at the
    // root (can't serialize a bare List<T>), hence DevItemDefinitionList.
    public static class DevItemRegistry
    {
        public static readonly System.Collections.Generic.List<DevItemDefinition> Definitions =
            new System.Collections.Generic.List<DevItemDefinition>();

        public static void Load()
        {
            Definitions.Clear();
            DevToolPaths.EnsureDataDirectory();
            if (!File.Exists(DevToolPaths.ItemsFile)) return;

            string json = File.ReadAllText(DevToolPaths.ItemsFile);
            DevItemDefinitionList wrapper = JsonUtility.FromJson<DevItemDefinitionList>(json);
            if (wrapper?.Items != null) Definitions.AddRange(wrapper.Items);
        }

        public static void Save()
        {
            DevToolPaths.EnsureDataDirectory();
            string json = JsonUtility.ToJson(new DevItemDefinitionList { Items = Definitions }, true);
            File.WriteAllText(DevToolPaths.ItemsFile, json);
        }

        public static void LoadAndRegisterAll()
        {
            Load();
            foreach (DevItemDefinition def in Definitions)
            {
                Register(def);
            }
        }

        public static bool Register(DevItemDefinition def)
        {
            var itemConfig = new ItemConfig
            {
                Name = string.IsNullOrEmpty(def.DisplayName) ? def.Name : def.DisplayName,
                Description = def.Description,
                CraftingStation = def.CraftingStation,
                MinStationLevel = def.MinStationLevel,
            };
            foreach (DevRequirement req in def.Requirements)
            {
                if (string.IsNullOrEmpty(req.ItemName)) continue;
                itemConfig.AddRequirement(req.ItemName, req.Amount, req.AmountPerLevel);
            }

            var customItem = new CustomItem(def.Name, def.BasePrefabName, itemConfig);
            if (!customItem.IsValid())
            {
                Jotunn.Logger.LogError($"DevTool: item '{def.Name}' (base '{def.BasePrefabName}') is not valid, skipping");
                return false;
            }

            ApplyStatOverrides(customItem.ItemDrop.m_itemData, def);

            return ItemManager.Instance.AddItem(customItem);
        }

        static void ApplyStatOverrides(ItemDrop.ItemData itemData, DevItemDefinition def)
        {
            ItemDrop.ItemData.SharedData shared = itemData.m_shared;

            if (!Mathf.Approximately(def.DamageMultiplier, 1f))
            {
                shared.m_damages.m_damage *= def.DamageMultiplier;
                shared.m_damages.m_blunt *= def.DamageMultiplier;
                shared.m_damages.m_slash *= def.DamageMultiplier;
                shared.m_damages.m_pierce *= def.DamageMultiplier;
                shared.m_damages.m_chop *= def.DamageMultiplier;
                shared.m_damages.m_pickaxe *= def.DamageMultiplier;
                shared.m_damages.m_fire *= def.DamageMultiplier;
                shared.m_damages.m_frost *= def.DamageMultiplier;
                shared.m_damages.m_lightning *= def.DamageMultiplier;
                shared.m_damages.m_poison *= def.DamageMultiplier;
            }

            // Additive elemental bonuses -- see DevItemDefinition.cs for
            // why these can't just be more multipliers (0 base * anything
            // is still 0).
            shared.m_damages.m_lightning += def.BonusLightningDamage;
            shared.m_damages.m_fire += def.BonusFireDamage;
            shared.m_damages.m_frost += def.BonusFrostDamage;
            shared.m_damages.m_poison += def.BonusPoisonDamage;

            if (!Mathf.Approximately(def.ArmorMultiplier, 1f))
            {
                shared.m_armor *= def.ArmorMultiplier;
                shared.m_armorPerLevel *= def.ArmorMultiplier;
            }

            if (!Mathf.Approximately(def.WeightMultiplier, 1f))
            {
                shared.m_weight *= def.WeightMultiplier;
            }

            if (!Mathf.Approximately(def.DurabilityMultiplier, 1f))
            {
                shared.m_maxDurability *= def.DurabilityMultiplier;
            }

            if (!Mathf.Approximately(def.SpeedMultiplier, 1f) && shared.m_attack != null)
            {
                shared.m_attack.m_speedFactor *= def.SpeedMultiplier;
            }

            if (!string.IsNullOrEmpty(def.StatusEffectName))
            {
                StatusEffect effect = ObjectDB.instance != null
                    ? ObjectDB.instance.GetStatusEffect(def.StatusEffectName.GetStableHashCode())
                    : null;

                if (effect != null)
                {
                    // Set on both -- whichever field this item's own type
                    // actually reads (consume for food/potions, equip for
                    // weapons/armor) picks it up; the other is simply
                    // never looked at, not a conflict.
                    shared.m_consumeStatusEffect = effect;
                    shared.m_equipStatusEffect = effect;
                }
                else
                {
                    Jotunn.Logger.LogWarning($"DevTool: could not resolve status effect '{def.StatusEffectName}' for item '{shared.m_name}'");
                }
            }

            if (!string.IsNullOrEmpty(def.SpecialEffect) && def.SpecialEffect != "None")
            {
                // Per-instance custom data, NOT m_shared -- ItemData.Clone()
                // deep-copies this, so setting it on the template ItemData
                // (this one) propagates to every real clone made from this
                // prefab, same mechanism Voltun's Hatchet's log-yield bonus
                // already relies on.
                if (itemData.m_customData == null) itemData.m_customData = new Dictionary<string, string>();
                itemData.m_customData[WeaponSpecialEffectPatch.EffectKey] = def.SpecialEffect;
            }
        }
    }
}
