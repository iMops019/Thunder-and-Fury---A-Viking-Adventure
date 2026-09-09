using Jotunn.Configs;
using Jotunn.Managers;

namespace ThunderFury.Core.SkillSystem
{
    // ---- Defense (Combat) ----
    //
    // vision.md, locked in: "Defense: damage reduction and/or stagger
    // resistance (how much you take, how well you shrug off hits)."
    // Confirmed against the real 1.0 decompile: vanilla's own Blocking
    // skill already does half of this -- Humanoid.BlockAttack reads
    // GetSkillFactor(Skills.SkillType.Blocking) to scale
    // ItemDrop.ItemData.GetBlockPower, and (unlike the weapon-damage case
    // Strength handles) that call goes through the polymorphic
    // Player.GetSkillFactor, so redirecting Blocking -> Defense here gets
    // "better blocking = more stagger resistance while blocking" for free
    // from the same SkillXpRedirect architecture every other skill uses
    // -- no new patch needed for that half. The other half, a general
    // damage-reduction layer that applies whether or not the hit was
    // blocked, has no vanilla mechanic to reuse and is a new patch
    // (Patches/CombatPatches.cs's DefenseDamageReductionPatch).
    public static class DefenseSkill
    {
        public const string Identifier = "com.ThunderFury.core.skill.defense";

        public static global::Skills.SkillType Type { get; private set; }

        public static void Register()
        {
            var config = new SkillConfig
            {
                Identifier = Identifier,
                Name = "Defense",
                Description = "Damage reduction and stagger resistance. Replaces vanilla's Blocking skill for block power, and also reduces all incoming damage generally.",
                IncreaseStep = 1f,
            };

            Type = SkillManager.Instance.AddSkill(config);
            SkillXpRedirect.Register(global::Skills.SkillType.Blocking, Type);
        }
    }
}
