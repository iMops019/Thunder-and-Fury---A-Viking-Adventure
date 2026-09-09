using Jotunn.Configs;
using Jotunn.Managers;

namespace VikingAdventure.Core.SkillSystem
{
    // ---- Skinning ----
    //
    // Fourth skill, and the first one with no vanilla equivalent at all --
    // confirmed against the real 1.0 decompile's full Skills.SkillType
    // enum, there's no "Skinning" entry, so there's nothing to redirect
    // XP away from (unlike Woodcutting/Pickaxes/Fishing). Just a
    // straight Jotunn registration.
    //
    // Per docs/valheim-mod-vision.md: "treating this as an action/flavor
    // mechanic first ... rather than designing deep level-gated bonuses
    // right away." Matches that directly -- no per-level scaling here.
    // What XP and bonus-yield chance this skill DOES get comes entirely
    // free from vanilla's own Pickable component
    // (Pickable.m_pickRaiseSkill + m_maxLevelBonusChance), which already
    // implements "raise a skill on harvest, chance of bonus yield scaled
    // by that skill's level" generically for any SkillType -- confirmed
    // this accepts a Jotunn custom SkillType exactly like a vanilla one,
    // so no custom XP-granting code was needed. See
    // Patches/SkinningPatches.cs for the carcass mechanic itself.
    public static class SkinningSkill
    {
        public const string Identifier = "com.vikingadventure.core.skill.skinning";

        // Shared with RarityLoot's SkinningKnife.cs -- the exact prefab
        // name the Skinning Knife item must be registered under, so
        // Core's tool-gate check and RarityLoot's item registration
        // agree without RarityLoot's class needing to be referenced from
        // Core (wrong dependency direction; only RarityLoot depends on
        // Core, not the other way).
        public const string SkinningKnifePrefabName = "SkinningKnife";

        public static global::Skills.SkillType Type { get; private set; }

        public static void Register()
        {
            var config = new SkillConfig
            {
                Identifier = Identifier,
                Name = "Skinning",
                Description = "Harvesting hide and meat from a carcass with a Skinning Knife.",
                IncreaseStep = 1f,
            };

            Type = SkillManager.Instance.AddSkill(config);
        }
    }
}
