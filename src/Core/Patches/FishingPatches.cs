using HarmonyLib;
using UnityEngine;
using ThunderFury.Core.SkillSystem;

namespace ThunderFury.Core.Patches
{
    // ---- Fishing gameplay effect: bite chance ----
    //
    // Confirmed against the real 1.0 decompile: Fish.FindFloat() is where
    // a nearby fish decides whether to bite -- `if (Random.value <
    // m_baseHookChance) return allInstance;` -- and that roll currently
    // has NO skill factor in it at all; the "Fishing" skill only affects
    // the reel-in minigame once a fish is already hooked (handled for
    // free by SkillXpRedirect's generic GetSkillFactor redirect, see
    // FishingSkill.cs). This is the genuinely missing "level scales catch
    // chance" half vision.md asks for.
    //
    // FindFloat() iterates every FishingFloat in the world and rolls once
    // per candidate in range, so there's no single per-attempt hook point
    // to scale in isolation. Instead this temporarily boosts the Fish's
    // own m_baseHookChance field for the duration of one FindFloat() call
    // -- based on the best Fishing level among any angler with a float in
    // range of this fish -- then restores it in a Postfix. Harmony's
    // __state parameter (not a static field) carries the original value
    // between the two, so this is safe even if FindFloat() runs for
    // multiple fish back-to-back in the same frame.
    [HarmonyPatch(typeof(Fish), nameof(Fish.FindFloat))]
    public static class FishingBiteChancePatch
    {
        static void Prefix(Fish __instance, out float __state)
        {
            __state = __instance.m_baseHookChance;

            float bestSkillFactor = 0f;
            foreach (FishingFloat ff in FishingFloat.GetAllInstances())
            {
                if (!ff.IsInWater()) continue;
                if (Vector3.Distance(__instance.transform.position, ff.transform.position) > ff.m_range) continue;
                if (!(ff.GetOwner() is Player player)) continue;

                float skillFactor = player.GetSkillFactor(FishingSkill.Type);
                if (skillFactor > bestSkillFactor) bestSkillFactor = skillFactor;
            }

            if (bestSkillFactor <= 0f) return;

            __instance.m_baseHookChance = Mathf.Clamp01(__state * (1f + bestSkillFactor * CorePlugin.FishingBiteChanceBonusAtMaxLevel.Value));
        }

        static void Postfix(Fish __instance, float __state)
        {
            __instance.m_baseHookChance = __state;
        }
    }
}
