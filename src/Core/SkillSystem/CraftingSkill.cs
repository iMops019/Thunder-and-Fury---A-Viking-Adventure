using Jotunn.Configs;
using Jotunn.Managers;

namespace ThunderFury.Core.SkillSystem
{
    // ---- Crafting ----
    //
    // Tenth and final skill from vision.md's original skill list. Like
    // Fletching, vision.md gives it zero dedicated design -- named once
    // in the skill list ("Production: Smithing, Cooking, Fletching,
    // Building, Crafting") with nothing else decided. Unlike Fletching,
    // though, this one has an obvious, low-risk role given everything
    // already built this session: vanilla's Skills.SkillType.Crafting
    // already exists and is the DEFAULT value of
    // CraftingStation.m_craftingSkill in code -- meaning any
    // CraftingStation whose real prefab data was never explicitly
    // pointed elsewhere (Stonecutter, Artisan Table, Mead Ketill, Food
    // Preparation Table, etc. -- none of which this mod has touched)
    // already grants vanilla Crafting XP for whatever gets made there.
    //
    // Redirecting that generic vanilla SkillType here -- same mechanism
    // as Woodcutting/Pickaxes/Fishing/Cooking -- makes this skill the
    // natural catch-all for "everything produced at a station Smithing/
    // Fletching/Building didn't explicitly claim," matching the OSRS
    // shape of Crafting as the broad generic production skill alongside
    // more specialized ones. This is also what closes the loop on
    // Smithing's own notes: the decision back then NOT to blanket-
    // redirect vanilla Crafting was made specifically because there was
    // no custom Crafting skill registered yet to redirect it into --
    // there wasn't a leak risk to solve, just a missing destination.
    // Now there is one.
    //
    // No custom gameplay effects -- same "nothing designed beyond the
    // name" scope as Fletching and Building.
    public static class CraftingSkill
    {
        public const string Identifier = "com.ThunderFury.core.skill.crafting";

        public static global::Skills.SkillType Type { get; private set; }

        public static void Register()
        {
            var config = new SkillConfig
            {
                Identifier = Identifier,
                Name = "Crafting",
                Description = "General crafting at stations not covered by a more specialized production skill.",
                IncreaseStep = 1f,
            };

            Type = SkillManager.Instance.AddSkill(config);
            SkillXpRedirect.Register(global::Skills.SkillType.Crafting, Type);
        }
    }
}
