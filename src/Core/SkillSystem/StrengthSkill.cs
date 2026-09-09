using Jotunn.Configs;
using Jotunn.Managers;

namespace ThunderFury.Core.SkillSystem
{
    // ---- Strength (Combat) ----
    //
    // vision.md, locked in: "Strength: damage output, one shared scaling
    // stat across all weapons (replaces vanilla's per-weapon ~1%/level
    // damage bump with a single unified stat)." Unlike every other skill
    // built so far, there's no single vanilla SkillType to redirect from
    // -- the vanilla mechanic being replaced (Character.GetRandomSkillFactor,
    // confirmed: Mathf.Lerp(0.4, 1, level/100) +/- 0.15 random, scaling
    // outgoing weapon damage) is keyed off the same 9 weapon SkillTypes
    // AttackSkill.WeaponSkillTypes already claims for tempo. So Strength
    // has no registration-time redirect of its own -- it's granted XP
    // alongside Attack (Patches/CombatPatches.cs's StrengthXpSharePatch)
    // and drives that damage formula directly
    // (Patches/CombatPatches.cs's StrengthDamageFactorPatch), both keyed
    // off AttackSkill.WeaponSkillTypes rather than a second list.
    public static class StrengthSkill
    {
        public const string Identifier = "com.ThunderFury.core.skill.strength";

        public static global::Skills.SkillType Type { get; private set; }

        public static void Register()
        {
            var config = new SkillConfig
            {
                Identifier = Identifier,
                Name = "Strength",
                Description = "Damage output across every weapon type -- one shared stat, replacing vanilla's per-weapon damage scaling.",
                IncreaseStep = 1f,
            };

            Type = SkillManager.Instance.AddSkill(config);
        }
    }
}
