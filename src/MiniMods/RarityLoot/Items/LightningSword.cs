using System.Collections.Generic;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using ThunderFury.Core.Combat;
using ThunderFury.RarityLoot.Affixes;
using ThunderFury.RarityLoot.Patches;

namespace ThunderFury.RarityLoot.Items
{
    // ---- Lightning Sword ----
    //
    // "A simple, not OP but fun starting Legendary Sword found in the
    // Meadows" -- user's own design, 2026-09-10. Deliberately a rare
    // creature DROP, not craftable, so finding one is a genuine early
    // power spike moment rather than a guaranteed unlock -- gives a
    // taste of Legendary-tier gear well before Bronze Age crafting would
    // normally produce anything comparable.
    //
    // First real content built on the new generic weapon special-effect
    // mechanism (Core/Combat/WeaponSpecialEffectPatch.cs +
    // ChainLightningEffect.cs, built the same session): sets the same
    // ItemData.m_customData key the Dev Tool's Item Creator now exposes
    // through its own "Special On-Hit Effect" dropdown, so this isn't a
    // one-off hack -- any future item (hand-coded or DevTool-made) can
    // opt into the exact same Chain Lightning proc this way.
    //
    // Same unverified-string caveat as StonePickaxe/VoltunsSet: "Boar"/
    // "Neck" as real vanilla creature prefab names are standard,
    // well-established Valheim modding knowledge, not independently
    // confirmed against this install's binary asset data. Safe failure
    // mode if wrong: that one drop source just doesn't register, logged,
    // nothing else breaks.
    public static class LightningSword
    {
        public const string PrefabName = "LightningSword";

        public static void Register()
        {
            var config = new ItemConfig
            {
                Name = "Lightning Sword",
                Description = "Crackles with stolen storm-fire. Every strike bites with lightning alongside steel, and sometimes the storm itself answers the call.",
            };

            var sword = new CustomItem(PrefabName, "SwordBronze", config);
            if (!sword.IsValid())
            {
                Jotunn.Logger.LogError("LightningSword item is not valid, skipping registration");
                return;
            }

            // No recipe -- Jotunn's ItemConfig auto-generates one, but an
            // empty CraftingStation would leave it in an undefined
            // "craftable from nothing" state rather than genuinely
            // uncraftable. Explicitly nulling it out (ItemManager.AddItem
            // only registers a recipe when CustomItem.Recipe is non-null,
            // confirmed via decompile) is the real way to make this
            // drop-only.
            sword.Recipe = null;

            ItemDrop.ItemData itemData = sword.ItemDrop.m_itemData;
            ItemDrop.ItemData.SharedData shared = itemData.m_shared;

            // Additive, on top of SwordBronze's own untouched physical
            // damage -- "melee damage AND lightning damage" per the
            // user's own framing, not a replacement.
            shared.m_damages.m_lightning += RarityLootPlugin.LightningSwordBonusDamage.Value;

            if (itemData.m_customData == null) itemData.m_customData = new Dictionary<string, string>();
            itemData.m_customData[WeaponSpecialEffectPatch.EffectKey] = WeaponSpecialEffectPatch.ChainLightning;

            ItemManager.Instance.AddItem(sword);
            ItemRollTrigger.Register(shared, RarityTier.Legendary, RarityLootPlugin.LegendaryAffixCount.Value);

            RegisterDropOn("Boar");
            RegisterDropOn("Neck");
        }

        static void RegisterDropOn(string creaturePrefabName)
        {
            GameObject creaturePrefab = PrefabManager.Instance.GetPrefab(creaturePrefabName);
            CharacterDrop drop = creaturePrefab != null ? creaturePrefab.GetComponent<CharacterDrop>() : null;
            if (drop == null)
            {
                Jotunn.Logger.LogWarning($"LightningSword: could not find CharacterDrop on '{creaturePrefabName}', skipping that drop source");
                return;
            }

            GameObject swordPrefab = PrefabManager.Instance.GetPrefab(PrefabName);
            if (swordPrefab == null) return;

            drop.m_drops.Add(new CharacterDrop.Drop
            {
                m_prefab = swordPrefab,
                m_amountMin = 1,
                m_amountMax = 1,
                m_chance = RarityLootPlugin.LightningSwordDropChance.Value,
                m_onePerPlayer = true,
            });
        }
    }
}
