using Jotunn.Configs;
using Jotunn.Managers;

namespace ThunderFury.Core.SkillSystem
{
    // ---- Fishing ----
    //
    // Third skill. Different shape from Woodcutting/Mining on purpose --
    // docs/valheim-mod-vision.md: "Fishing keeps vanilla's cast/bait/reel
    // mechanic largely as-is (not reinventing it) ... with level
    // primarily scaling catch chance." No damage concept here; the
    // registration + XP redirect is identical to every other skill, but
    // the gameplay effect is entirely in Patches/FishingPatches.cs.
    //
    // Confirmed against the real 1.0 decompile: vanilla's reel-in
    // mechanics (stamina cost, pull speed) already read
    // Player.GetSkillFactor(Skills.SkillType.Fishing) directly, so once
    // Fishing is registered here, SkillXpRedirect's generic
    // GetSkillFactorRedirectPatch makes those vanilla formulas track our
    // skill automatically -- no separate patch needed for the reel-in
    // half, unlike Woodcutting/Mining's speed mechanic which needed its
    // own research. The one genuinely missing piece, researched
    // separately: bite chance (see FishingPatches.cs), which vanilla
    // currently doesn't scale by skill at all.
    //
    // Not done: vision.md also wants the Fishing Rod craftable from
    // level 1 (no Haldor dependency) and Bait obtainable from common
    // early drops instead of gold-gated. That's an item/recipe change --
    // adding a Workbench recipe to the existing vanilla FishingRod/Bait
    // prefabs rather than skill-mechanic code -- deliberately left for a
    // separate pass, same way Woodcutting/Mining's itemization
    // (Stone Pickaxe, Voltun's Set) lives in RarityLoot rather than here.
    public static class FishingSkill
    {
        public const string Identifier = "com.ThunderFury.core.skill.fishing";

        public static global::Skills.SkillType Type { get; private set; }

        public static void Register()
        {
            var config = new SkillConfig
            {
                Identifier = Identifier,
                Name = "Fishing",
                Description = "Casting, baiting, and reeling. Higher levels mean fish bite more often and you reel them in faster with less stamina.",
                IncreaseStep = 1f,
            };

            Type = SkillManager.Instance.AddSkill(config);
            SkillXpRedirect.Register(global::Skills.SkillType.Fishing, Type);
        }
    }
}
