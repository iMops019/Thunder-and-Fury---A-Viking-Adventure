using Jotunn.Configs;
using Jotunn.Managers;

namespace VikingAdventure.Core.SkillSystem
{
    // ---- Woodcutting ----
    //
    // First skill built end-to-end, proving out the whole pattern the
    // other 8 skills (docs/valheim-mod-vision.md's skill list) will
    // follow: register via Jotunn, redirect vanilla's XP into it,
    // reuse vanilla's own leveling curve (Skill.Raise's
    // Mathf.Pow(level+1, 1.5) * 0.5 + 0.5 formula -- confirmed from the
    // real decompile, and exactly what vision.md means by "smooth,
    // no artificial walls"; Jotunn's custom skills plug into the same
    // Skills/Skill system vanilla's own skills use, so this comes for
    // free rather than needing a bespoke curve), then layer gameplay
    // effects on top (Patches/WoodcuttingPatches.cs).
    public static class WoodcuttingSkill
    {
        public const string Identifier = "com.vikingadventure.core.skill.woodcutting";

        public static global::Skills.SkillType Type { get; private set; }

        public static void Register()
        {
            var config = new SkillConfig
            {
                Identifier = Identifier,
                Name = "Woodcutting",
                Description = "Chopping trees and logs for wood. Higher levels hit trees harder; milestone levels grant bonus XP and yield.",
                IncreaseStep = 1f,
            };

            Type = SkillManager.Instance.AddSkill(config);
            SkillXpRedirect.Register(global::Skills.SkillType.WoodCutting, Type, AdjustXp);
        }

        static float AdjustXp(Player player, float baseValue)
        {
            float level = player.GetSkills().GetSkillLevel(Type);
            if (level >= CorePlugin.WoodcuttingMilestoneLevel.Value)
            {
                return baseValue * CorePlugin.WoodcuttingMilestoneXpMultiplier.Value;
            }
            return baseValue;
        }
    }
}
