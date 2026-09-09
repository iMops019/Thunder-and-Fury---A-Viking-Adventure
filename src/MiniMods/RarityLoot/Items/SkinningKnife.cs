using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using VikingAdventure.Core.SkillSystem;

namespace VikingAdventure.RarityLoot.Items
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
    // Same verification caveat as Stone Pickaxe: "Knife" as the base
    // prefab and "Wood"/"Flint" as requirement item ids are standard,
    // well-established Jotunn/Valheim names, not independently verified
    // against this install's binary asset data.
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

            var knife = new CustomItem(SkinningSkill.SkinningKnifePrefabName, "Knife", config);
            if (!knife.IsValid())
            {
                Jotunn.Logger.LogError("SkinningKnife item is not valid, skipping registration");
                return;
            }

            ItemManager.Instance.AddItem(knife);
        }
    }
}
