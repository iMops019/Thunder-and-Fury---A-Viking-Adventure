using System.Collections.Generic;
using Jotunn.Configs;
using Jotunn.Managers;

namespace ThunderFury.Core.SkillSystem
{
    // ---- Attack (Combat) ----
    //
    // vision.md, locked in: "Attack: attack speed + stamina efficiency for
    // weapon use (tempo -- how much you can fight)" and "OSRS-style broad
    // stats that apply across all weapon types, not per-weapon-category
    // like vanilla's Swords/Axes/Spears split." Confirmed against the real
    // 1.0 decompile: vanilla splits weapon use across 9 separate
    // Skills.SkillType entries (Swords, Knives, Clubs, Polearms, Spears,
    // Axes, Bows, Crossbows, Unarmed) -- WeaponSkillTypes below is that
    // full set. Every one of them redirects into this single Attack skill
    // via the same generic SkillXpRedirect architecture Woodcutting/Mining
    // etc. already use, which means the "tempo" half (Attack.GetAttackStamina's
    // `0.33 * GetSkillFactor(weapon's skill type)` stamina-cost-per-swing
    // reduction) comes for free from SkillXpRedirect.GetSkillFactorRedirectPatch
    // -- no new patch needed for that half, same as every gathering skill's
    // "speed" already works.
    //
    // Deliberately does NOT cover weapon damage scaling -- confirmed that's
    // a separate vanilla code path (Character.GetRandomSkillFactor, called
    // internally by Skills.GetRandomSkillFactor via its own
    // Skills.GetSkillFactor rather than through Player.GetSkillFactor, so
    // the generic redirect above never touches it) which vision.md assigns
    // to Strength instead (StrengthSkill.cs / Patches/CombatPatches.cs).
    // That split turned out to already exist in vanilla's own method
    // structure, not something this mod had to engineer.
    public static class AttackSkill
    {
        public const string Identifier = "com.ThunderFury.core.skill.attack";

        public static global::Skills.SkillType Type { get; private set; }

        // The full set of vanilla weapon-use skills this mod's single
        // Attack stat replaces. Shared with Patches/CombatPatches.cs so
        // Strength's damage-factor and XP-share patches key off the exact
        // same list rather than duplicating it.
        public static readonly HashSet<global::Skills.SkillType> WeaponSkillTypes = new HashSet<global::Skills.SkillType>
        {
            global::Skills.SkillType.Swords,
            global::Skills.SkillType.Knives,
            global::Skills.SkillType.Clubs,
            global::Skills.SkillType.Polearms,
            global::Skills.SkillType.Spears,
            global::Skills.SkillType.Axes,
            global::Skills.SkillType.Bows,
            global::Skills.SkillType.Crossbows,
            global::Skills.SkillType.Unarmed,
        };

        public static void Register()
        {
            var config = new SkillConfig
            {
                Identifier = Identifier,
                Name = "Attack",
                Description = "Weapon tempo -- attack speed and stamina efficiency across every weapon type, replacing vanilla's per-weapon skill split.",
                IncreaseStep = 1f,
            };

            Type = SkillManager.Instance.AddSkill(config);

            foreach (global::Skills.SkillType vanillaType in WeaponSkillTypes)
            {
                SkillXpRedirect.Register(vanillaType, Type);
            }
        }
    }
}
