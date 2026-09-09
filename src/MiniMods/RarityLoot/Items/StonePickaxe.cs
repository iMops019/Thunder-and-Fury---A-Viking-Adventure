using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;

namespace ThunderFury.RarityLoot.Items
{
    // ---- Stone Pickaxe ----
    //
    // First planned Pillar 3 item (docs/valheim-mod-vision.md, Gathering
    // design philosophy): a tier below the vanilla Antler Pickaxe,
    // craftable from Wood + Stone with no boss-material requirement. Can
    // mine copper, just slower/weaker than the Antler Pickaxe -- tool
    // tier is meant to be an efficiency curve, not a hard gate.
    //
    // Built the way Pillar 3 is supposed to work: assembled from an
    // existing vanilla asset (Jotunn's CustomItem clones "PickaxeAntler"
    // wholesale, keeping its model/animations/tool tier -- still able to
    // mine copper -- exactly as-is) rather than modeling anything new.
    // Only the mining damage gets scaled down afterward, and it's scaled
    // relative to whatever the real cloned Antler value turns out to be
    // at runtime rather than a hardcoded guess, since that number lives
    // in Unity asset data we can't read from the decompiled C# directly.
    //
    // Two strings here ("PickaxeAntler" as the base prefab, "Wood"/"Stone"
    // as requirement item ids) are standard, extremely well-established
    // Valheim/Jotunn internal names but weren't independently verified
    // against this local install's binary asset data the way the C# hook
    // points elsewhere in this mod were -- if wrong, Jotunn logs a clear
    // "could not resolve reference" error on load rather than failing
    // silently, so this is a safe bet to make untested.
    public static class StonePickaxe
    {
        public static void Register()
        {
            var config = new ItemConfig
            {
                Name = "Stone Pickaxe",
                Description = "A crude pickaxe lashed from wood and stone. Weaker and slower than the Antler Pickaxe, but needs nothing more to make.",
                CraftingStation = CraftingStations.Workbench,
                Requirements = new[]
                {
                    new RequirementConfig("Wood", RarityLootPlugin.StonePickaxeWoodCost.Value),
                    new RequirementConfig("Stone", RarityLootPlugin.StonePickaxeStoneCost.Value),
                },
            };

            var stonePickaxe = new CustomItem("StonePickaxe", "PickaxeAntler", config);
            if (!stonePickaxe.IsValid())
            {
                Jotunn.Logger.LogError("StonePickaxe item is not valid, skipping registration");
                return;
            }

            var shared = stonePickaxe.ItemDrop.m_itemData.m_shared;
            shared.m_damages.m_pickaxe *= RarityLootPlugin.StonePickaxeDamageMultiplier.Value;

            ItemManager.Instance.AddItem(stonePickaxe);
        }
    }
}
