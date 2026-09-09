using Jotunn.Configs;
using Jotunn.Managers;

namespace VikingAdventure.Core.SkillSystem
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
    // No milestone here, unlike Woodcutting's level-15 bonus --
    // docs/valheim-mod-vision.md only designed a concrete milestone for
    // Woodcutting ("First concrete milestone: Woodcutting level 15 ...").
    // Nothing's been decided for Mining's milestone yet, so nothing's
    // invented here; just the shared gathering mechanic vision.md
    // explicitly says both skills use ("level -> speed + damage to
    // ore/trees").
    public static class MiningSkill
    {
        public const string Identifier = "com.vikingadventure.core.skill.mining";

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
            SkillXpRedirect.Register(global::Skills.SkillType.Pickaxes, Type);
        }
    }
}
