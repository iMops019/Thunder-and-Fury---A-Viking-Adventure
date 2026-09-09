using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using VikingAdventure.Core.SkillSystem;

namespace VikingAdventure.RarityLoot.Items
{
    // ---- Skinning Knife ----
    //
    // Second planned Pillar 3 item (docs/valheim-mod-vision.md): required
    // to harvest carcasses under Core's new Skinning mechanic
    // (Core/Patches/SkinningPatches.cs). Built the moment Skinning itself
    // landed, matching docs/PROGRESS.md's own note that this item "pairs
    // with Core's Skinning work when that starts, not before."
    //
    // Registered under the exact prefab name
    // SkinningSkill.SkinningKnifePrefabName -- Core's carcass tool-gate
    // check compares against that same constant, so this item and Core's
    // check can't drift out of sync even though Core has no reference
    // back to this class (RarityLoot depends on Core, not the reverse).
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
