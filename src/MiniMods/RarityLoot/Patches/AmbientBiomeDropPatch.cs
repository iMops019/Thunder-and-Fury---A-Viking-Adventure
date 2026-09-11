using System;
using HarmonyLib;
using UnityEngine;
using ThunderFury.Core.Loot;
using ThunderFury.RarityLoot.Affixes;
using ThunderFury.RarityLoot.Loot;

namespace ThunderFury.RarityLoot.Patches
{
    // ---- Ambient biome-tiered rarity drops on kill ----
    //
    // The redesign the user asked for (2026-09-10), replacing the flat
    // "any weapon/armor has a % chance to roll Magic/Rare" idea with one
    // gated by where the kill actually happened: "a Greybeard in Meadows
    // can drop up to the highest tier able to be made in Meadows... this
    // sounds super OP" otherwise. Deliberately its own independent
    // mechanism, NOT a change to CharacterDrop.m_drops or to the existing
    // ItemRollTriggerPatch (Clone()-based) flow -- those keep governing
    // ordinary vanilla loot and crafted-item rolls exactly as before,
    // completely unaffected and uncapped, matching the user's explicit
    // "regular loot tables are fine" carve-out.
    //
    // Character.OnDeath() (public, confirmed via decompile, same hook
    // Quests' own KillTracking.GrantKillCreditPatch already uses) fires
    // once per death regardless of how many vanilla drops it produces --
    // this Postfix does at most ONE roll-and-spawn attempt per call, which
    // is the whole mechanism behind the user's "allow only 1 drop" cap.
    // No extra per-kill guard needed: one Postfix invocation, one attempt.
    [HarmonyPatch(typeof(Character), nameof(Character.OnDeath))]
    public static class AmbientBiomeDropPatch
    {
        static readonly System.Random Rng = new System.Random();

        static void Postfix(Character __instance)
        {
            if (__instance == null || __instance is Player) return;
            if (WorldGenerator.instance == null) return;

            if (Rng.NextDouble() >= RarityLootPlugin.AmbientDropChance.Value) return;

            Vector3 position = __instance.GetCenterPoint();
            Heightmap.Biome biome = WorldGenerator.instance.GetBiome(position);
            int maxTier = BiomeTierMap.MaxTierFor(biome);

            string creatureName = ThunderFury.Core.Utils.PrefabNameHelper.GetPrefabName(__instance);
            bool legendaryEligible = LegendaryDropSourceRegistry.IsEligible(creatureName);

            RollTier(legendaryEligible, out RarityTier tier, out int affixCount);

            if (!BiomeGearCatalog.TryPickRandomItem(maxTier, Rng, out GameObject prefab)) return;

            ItemDrop template = prefab.GetComponent<ItemDrop>();
            if (template?.m_itemData == null) return;

            ItemDrop spawned = ItemDrop.DropItem(template.m_itemData, 1, position, Quaternion.identity);
            if (spawned?.m_itemData == null) return;

            // Whatever the generic ordinary-gear roll (ItemRollTrigger.cs)
            // already did to this clone is exactly the flat, non-biome-
            // aware roll this feature replaces for ambient drops --
            // discard it and apply the tier this method actually decided.
            ItemRoller.ClearRoll(spawned.m_itemData);
            ItemRoller.Roll(spawned.m_itemData, tier, affixCount);
        }

        // The user's own two-stage description: stage one already
        // happened above (did anything drop at all this kill); this is
        // stage two, "a % chance on if its magic or rare or if its a
        // legendary." Rare is checked before Magic so its odds aren't
        // shadowed by the far more common Magic roll, same ordering
        // ItemRollTrigger's existing ordinary-gear roll already uses.
        // Legendary is checked first of all, but only reachable at all
        // when the dying creature is on the DevTool-editable eligibility
        // list -- everything else can only ever land Magic or Rare here.
        static void RollTier(bool legendaryEligible, out RarityTier tier, out int affixCount)
        {
            double roll = Rng.NextDouble();

            if (legendaryEligible && roll < RarityLootPlugin.AmbientLegendaryShare.Value)
            {
                tier = RarityTier.Legendary;
                affixCount = RarityLootPlugin.LegendaryAffixCount.Value;
                return;
            }

            if (roll < RarityLootPlugin.AmbientLegendaryShare.Value + RarityLootPlugin.AmbientRareShare.Value)
            {
                tier = RarityTier.Rare;
                affixCount = RarityLootPlugin.OrdinaryRareAffixCount.Value;
                return;
            }

            tier = RarityTier.Magic;
            affixCount = RarityLootPlugin.OrdinaryMagicAffixCount.Value;
        }
    }
}
