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

        // The prefab name RarityLoot's SkinningKnife.cs registers its
        // item under. Not used for tool-gating anymore (Patches/
        // SkinningPatches.cs checks weapon skill type == Knives, so any
        // knife/dagger qualifies -- "just need a basic flint/stone knife
        // or dagger," this session's call) -- kept as the one place that
        // name is spelled out, so RarityLoot's registration and any
        // future reference to it stay in sync.
        public const string SkinningKnifePrefabName = "SkinningKnife";

        public static global::Skills.SkillType Type { get; private set; }

        public static void Register()
        {
            var config = new SkillConfig
            {
                Identifier = Identifier,
                Name = "Skinning",
                Description = "Skinning and butchering carcasses with a knife or dagger for hide and meat.",
                IncreaseStep = 1f,
            };

            Type = SkillManager.Instance.AddSkill(config);
        }
    }
}
