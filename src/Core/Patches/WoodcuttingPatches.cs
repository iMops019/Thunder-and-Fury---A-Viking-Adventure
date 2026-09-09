using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using VikingAdventure.Core.SkillSystem;

namespace VikingAdventure.Core.Patches
{
    // ---- Woodcutting gameplay effects ----
    //
    // Confirmed against the real 1.0 decompile: small trees (TreeBase)
    // and the fallen logs they spawn (TreeLog) both process hits through
    // their own RPC_Damage(long, HitData), each computing damage from
    // the same HitData.m_damage struct the attack itself built. Scaling
    // that struct in a Prefix, keyed off the attacker's Woodcutting
    // level, is "damage dealt to their resource" from vision.md's
    // gathering design (a smooth per-level curve, not a milestone) --
    // same technique already used for RarityLoot's weapon damage affix.
    //
    // Chop SPEED: researched. Vanilla has no literal swing-animation-speed
    // stat anywhere -- melee timing is baked into each weapon's animation
    // clip, not a scriptable number, and hacking Animator.speed directly
    // would risk desyncing the networked hit-trigger timing. Instead,
    // vanilla represents weapon "efficiency" the same way ValheimQoL's
    // StaminaCombatPacing does: stamina cost per swing, via
    // Attack.GetAttackStamina() reducing cost by
    // `0.33 * GetSkillFactor(m_weapon.m_shared.m_skillType)`. No patch
    // needed here anymore -- SkillXpRedirect.GetSkillFactorRedirectPatch
    // (added while wiring up Fishing) fixes Player.GetSkillFactor itself
    // for every redirected skill, so this formula now reads Woodcutting's
    // real level generically, the same way every other vanilla formula
    // that calls GetSkillFactor does. Net effect unchanged: higher
    // Woodcutting level = more chops per stamina bar = faster sustained
    // gathering, without touching animation timing at all.
    public static class TreeDamageBoost
    {
        public static void Apply(HitData hit)
        {
            if (hit == null) return;
            if (!(hit.GetAttacker() is Player player)) return;

            float level = player.GetSkills().GetSkillLevel(WoodcuttingSkill.Type);
            if (level <= 0f) return;

            float mult = 1f + level * CorePlugin.WoodcuttingDamagePerLevel.Value;
            hit.m_damage.m_damage *= mult;
            hit.m_damage.m_blunt *= mult;
            hit.m_damage.m_slash *= mult;
            hit.m_damage.m_pierce *= mult;
            hit.m_damage.m_chop *= mult;
            hit.m_damage.m_pickaxe *= mult;
            hit.m_damage.m_fire *= mult;
            hit.m_damage.m_frost *= mult;
            hit.m_damage.m_lightning *= mult;
            hit.m_damage.m_poison *= mult;
        }
    }

    [HarmonyPatch(typeof(TreeBase), nameof(TreeBase.RPC_Damage))]
    public static class TreeDamagePatch
    {
        static void Prefix(HitData hit) => TreeDamageBoost.Apply(hit);
    }

    [HarmonyPatch(typeof(TreeLog), nameof(TreeLog.RPC_Damage))]
    public static class LogDamagePatch
    {
        static void Prefix(HitData hit) => TreeDamageBoost.Apply(hit);
    }

    // Woodcutting level-15 milestone's "+25% bonus log yield" half (the
    // other half, 2x XP, lives in WoodcuttingSkill.AdjustXp). Same
    // technique as RarityLoot's LogYieldPatch -- TreeLog.Destroy's
    // per-item drop count is an unreachable local variable, so this
    // independently rolls and spawns bonus drops from the log's own drop
    // table in a Prefix, before vanilla's Destroy() runs and destroys the
    // GameObject those fields live on. Deliberately a separate patch
    // class from RarityLoot's (Core can't depend on RarityLoot -- wrong
    // dependency direction, mini-mods depend on Core, not the other way)
    // but both can Prefix the same method and stack; Harmony chains
    // multiple patches on one method automatically.
    [HarmonyPatch(typeof(TreeLog), nameof(TreeLog.Destroy))]
    public static class WoodcuttingMilestoneLogYieldPatch
    {
        static void Prefix(TreeLog __instance, HitData hitData)
        {
            if (hitData == null) return;
            if (!(hitData.GetAttacker() is Player player)) return;

            float level = player.GetSkills().GetSkillLevel(WoodcuttingSkill.Type);
            if (level < CorePlugin.WoodcuttingMilestoneLevel.Value) return;

            List<GameObject> dropList = __instance.m_dropWhenDestroyed.GetDropList();
            if (dropList.Count == 0) return;

            int extra = Mathf.RoundToInt(dropList.Count * CorePlugin.WoodcuttingMilestoneLogYieldBonusPercent.Value / 100f);
            Vector3 basePos = __instance.transform.position;

            for (int i = 0; i < extra; i++)
            {
                GameObject prefab = dropList[i % dropList.Count];
                Vector3 pos = basePos + Random.insideUnitSphere * 0.5f + Vector3.up * 0.3f;
                ItemDrop.OnCreateNew(Object.Instantiate(prefab, pos, Quaternion.identity));
            }
        }
    }
}
