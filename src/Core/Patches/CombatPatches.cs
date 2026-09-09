using HarmonyLib;
using UnityEngine;
using ThunderFury.Core.SkillSystem;

namespace ThunderFury.Core.Patches
{
    // ---- Combat gameplay effects (Attack / Strength / Defense) ----
    //
    // Attack's tempo effect and Defense's block-power effect both come for
    // free from SkillXpRedirect's existing generic patches (see
    // AttackSkill.cs / DefenseSkill.cs headers) -- nothing new needed for
    // those halves. What's here is the two mechanics that don't have an
    // existing vanilla choke point to reuse: Strength's damage scaling,
    // and Defense's general (not just block) damage reduction.

    // ---- Strength: damage output ----
    //
    // Confirmed against the real 1.0 decompile: Attack.cs's melee/area/
    // projectile hit paths all scale the outgoing HitData through
    // Character.GetRandomSkillFactor(m_weapon.m_shared.m_skillType) --
    // Player's override forwards straight to
    // Skills.GetRandomSkillFactor(skillType), which computes
    // Mathf.Lerp(0.4, 1, GetSkillFactor(skillType)) +/- 0.15 internally by
    // calling Skills.GetSkillFactor directly (NOT Player.GetSkillFactor),
    // so SkillXpRedirect.GetSkillFactorRedirectPatch never touches this --
    // it's a genuinely separate vanilla code path. That's exactly the
    // split vision.md wants (Attack = tempo, Strength = damage); it just
    // turned out to already exist in vanilla's own method structure rather
    // than needing to be carved out here. This Postfix substitutes
    // Strength's level for whichever weapon skill vanilla asked about,
    // using the same shape vanilla's own formula uses so the damage curve
    // feels consistent with what it's replacing.
    [HarmonyPatch(typeof(Player), nameof(Player.GetRandomSkillFactor))]
    public static class StrengthDamageFactorPatch
    {
        static void Postfix(Player __instance, global::Skills.SkillType skill, ref float __result)
        {
            if (!AttackSkill.WeaponSkillTypes.Contains(skill)) return;

            float strengthFactor = __instance.GetSkills().GetSkillFactor(StrengthSkill.Type);
            float mid = Mathf.Lerp(0.4f, 1f, strengthFactor);
            float min = Mathf.Clamp01(mid - 0.15f);
            float max = Mathf.Clamp01(mid + 0.15f);
            __result = Mathf.Lerp(min, max, Random.value);
        }
    }

    // Strength has no vanilla skill of its own to redirect XP from (see
    // StrengthSkill.cs header) -- it rides alongside Attack instead. A
    // second, independent Prefix on the same Player.RaiseSkill choke point
    // SkillXpRedirect.RaiseSkillRedirectPatch already patches -- Harmony
    // runs every registered Prefix on a method regardless of any other
    // Prefix's return value (same precedent as FletchingCraftRedirectPatch
    // coexisting with the generic redirect), so this fires alongside, not
    // instead of, the Attack redirect for the same weapon-skill
    // RaiseSkill call.
    [HarmonyPatch(typeof(Player), nameof(Player.RaiseSkill))]
    public static class StrengthXpSharePatch
    {
        static void Prefix(Player __instance, global::Skills.SkillType skill, float value)
        {
            if (!AttackSkill.WeaponSkillTypes.Contains(skill)) return;

            __instance.GetSkills().RaiseSkill(StrengthSkill.Type, value * CorePlugin.StrengthXpShareOfAttack.Value);
        }
    }

    // ---- Defense: general damage reduction ----
    //
    // Confirmed against the real 1.0 decompile: Character.RPC_Damage is
    // where a hit taken actually gets resistance/armor applied
    // (hit.ApplyResistance, then hit.ApplyArmor(GetBodyArmor()) for
    // players) before ApplyDamage subtracts health. A Prefix here, scoped
    // to the player being HIT (not the attacker -- this is the receiving
    // Character's own RPC), scales the same HitData.m_damage fields every
    // other damage patch in this mod already touches (Woodcutting/Mining's
    // boosts, just in reverse) before any of vanilla's own mitigation
    // runs, so it stacks as an additional layer on top of armor/resistance
    // rather than replacing them. Reducing hit.m_damage here also reduces
    // HitData.GetTotalStaggerDamage() (it reads the same fields), so one
    // patch covers both halves vision.md allows for ("damage reduction
    // and/or stagger resistance") without inventing a second mechanic.
    // Floor-clamped (DefenseMinDamageMultiplier, default 0.1) so a very
    // high Defense level can't be tuned into literal invincibility, unlike
    // Mining/Woodcutting's symmetric damage-UP scaling which has no such
    // ceiling risk on the receiving end.
    [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
    public static class DefenseDamageReductionPatch
    {
        static void Prefix(Character __instance, HitData hit)
        {
            if (hit == null) return;
            if (!(__instance is Player player)) return;

            float level = player.GetSkills().GetSkillLevel(DefenseSkill.Type);
            if (level <= 0f) return;

            float mult = Mathf.Max(
                CorePlugin.DefenseMinDamageMultiplier.Value,
                1f - level * CorePlugin.DefenseDamageReductionPerLevel.Value);

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
}
