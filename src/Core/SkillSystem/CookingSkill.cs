using Jotunn.Configs;
using Jotunn.Managers;

namespace ThunderFury.Core.SkillSystem
{
    // ---- Cooking ----
    //
    // Seventh skill. Unlike Skinning/Smithing, vanilla's Cooking skill
    // (Skills.SkillType.Cooking) IS already actively used -- confirmed
    // against the real 1.0 decompile: CookingStation.OnInteract raises
    // it both when adding an ingredient and when collecting a finished
    // dish, with a GetSkillFactor-scaled chance of a bonus extra item on
    // collection (the same style of bonus-yield roll seen elsewhere in
    // vanilla). Redirected here the same way as Woodcutting/Pickaxes/
    // Fishing, for consistency with vision.md's "every skill the player
    // sees is ours" architecture -- not because vanilla's own Cooking
    // skill is broken, just to keep the system uniform. That bonus-food
    // roll already tracks our skill automatically once redirected, via
    // SkillXpRedirect's generic GetSkillFactor patch -- no separate code
    // needed for that half.
    //
    // vision.md, locked in: "Level effect: reduced burn/fail chance
    // while cooking (not speed, not access)." Confirmed vanilla's own
    // burn timer (UpdateCooking,
    // `cookedTime > itemConversion.m_cookTime * 2f`) has NO skill factor
    // in it at all currently -- purely time-based. See
    // Patches/CookingPatches.cs for how that's fixed.
    //
    // Also per vision.md: no level-gating on which recipes/items can be
    // cooked (biome progression already paces ingredients) -- nothing to
    // build there, that's just leaving vanilla's own recipe availability
    // alone. And the "special/multi-ingredient recipe tier" is
    // explicitly flagged in vision.md as still unfleshed design space,
    // so nothing invented for it here.
    public static class CookingSkill
    {
        public const string Identifier = "com.ThunderFury.core.skill.cooking";

        public static global::Skills.SkillType Type { get; private set; }

        public static void Register()
        {
            var config = new SkillConfig
            {
                Identifier = Identifier,
                Name = "Cooking",
                Description = "Preparing food at a cooking station. Higher levels give food a better chance of being saved from burning.",
                IncreaseStep = 1f,
            };

            Type = SkillManager.Instance.AddSkill(config);
            SkillXpRedirect.Register(global::Skills.SkillType.Cooking, Type);
        }
    }
}
