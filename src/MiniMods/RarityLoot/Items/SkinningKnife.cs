using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using ThunderFury.Core.SkillSystem;

namespace ThunderFury.RarityLoot.Items
{
    // ---- Skinning Knife ----
    //
    // Second planned Pillar 3 item (docs/valheim-mod-vision.md): a basic,
    // cheap, RP-flavored knife for Core's Skinning/Butchering mechanic
    // (Core/Patches/SkinningPatches.cs). Built the moment Skinning itself
    // landed, matching docs/PROGRESS.md's own note that this item "pairs
    // with Core's Skinning work when that starts, not before."
    //
    // Not the exclusive gate: Core checks the equipped item's weapon
    // skill type (any Skills.SkillType.Knives tool qualifies, vanilla's
    // starting Knife included), not this item's identity specifically --
    // per this session's clarification, "just need a basic flint/stone
    // knife or dagger," not one hardcoded named item. This one exists
    // for players who want a purpose-named tool for the job (and to give
    // an early, cheap option independent of whatever knife they happen
    // to already own).
    //
    // "Knife" turned out to be wrong post-1.0 -- confirmed live in-game
    // 2026-09-10 (Jotunn: "can not find base prefab with name: Knife",
    // this item failing registration entirely). A live ObjectDB dump
    // confirmed the real name is "KnifeFlint" (1.0 apparently split the
    // old singular "Knife" into a full tier progression like every other
    // tool) -- fixed to clone that, which also fits this item's own
    // Wood+Flint recipe better than a guessed name ever did.
    public static class SkinningKnife
    {
        public static void Register()
        {
            var config = new ItemConfig
            {
                Name = "Skinning Knife",
                Description = "A short, plain blade for skinning and butchering. Not much of a weapon, but it gets the job done.",
                CraftingStation = CraftingStations.Workbench,
                Requirements = new[]
                {
                    new RequirementConfig("Wood", RarityLootPlugin.SkinningKnifeWoodCost.Value),
                    new RequirementConfig("Flint", RarityLootPlugin.SkinningKnifeFlintCost.Value),
                },
            };

            var knife = new CustomItem(SkinningSkill.SkinningKnifePrefabName, "KnifeFlint", config);
            if (!knife.IsValid())
            {
                Jotunn.Logger.LogError("SkinningKnife item is not valid, skipping registration");
                return;
            }

            ItemManager.Instance.AddItem(knife);
        }
    }
}
