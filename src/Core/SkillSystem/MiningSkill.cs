using Jotunn.Configs;
using Jotunn.Managers;

namespace ThunderFury.Core.SkillSystem
{
    // ---- Mining ----
    //
    // Second skill, same pattern Woodcutting proved out. Confirmed
    // against the real 1.0 decompile: vanilla's mining tool skill is
    // Skills.SkillType.Pickaxes (=12), and ore/rock damage flows through
    // two different components depending on the rock type -- MineRock5
    // (newer, multi-hit-area rocks) and MineRock (older, single-area) --
    // both process hits via the same HitData struct every other damage
    // patch in this mod already scales (see Patches/MiningPatches.cs).
    //
    // Shares Woodcutting's level-15 milestone (2x XP + bonus yield),
    // by direction -- vision.md itself only ever spelled out the
    // milestone for Woodcutting, but Mining and Woodcutting are the same
    // designed mechanic applied to different node types, so the same
    // milestone level/shape carries over. See Patches/MiningPatches.cs
    // for the ore-yield half.
    public static class MiningSkill
    {
        public const string Identifier = "com.ThunderFury.core.skill.mining";

        public static global::Skills.SkillType Type { get; private set; }

        public static void Register()
        {
            var config = new SkillConfig
            {
                Identifier = Identifier,
                Name = "Mining",
                Description = "Breaking rock and ore with a pickaxe. Higher levels hit harder and cost less stamina per swing.",
                IncreaseStep = 1f,
            };

            Type = SkillManager.Instance.AddSkill(config);
            SkillXpRedirect.Register(global::Skills.SkillType.Pickaxes, Type, AdjustXp);
        }

        static float AdjustXp(Player player, float baseValue)
        {
            float level = player.GetSkills().GetSkillLevel(Type);
            if (level >= CorePlugin.MiningMilestoneLevel.Value)
            {
                return baseValue * CorePlugin.MiningMilestoneXpMultiplier.Value;
            }
            return baseValue;
        }
    }
}
