using HarmonyLib;
using ThunderFury.Core.SkillSystem;
using ThunderFury.RarityLoot.Affixes;

namespace ThunderFury.RarityLoot.Patches
{
    // ---- Legendary crafting requires a Smithing level ----
    //
    // vision.md's Smithing tier ladder (locked in): "Legendary
    // weapons/armor specifically DO require a Smithing level threshold
    // to craft -- a deliberate hard gate on the top tier." The exact
    // level was left undecided in vision.md, so it's a config value
    // (SmithingLegendaryCraftLevel) rather than a guessed number.
    //
    // Reuses ItemRollTrigger's existing registry (SharedData ->
    // RarityTier) to ask "is this recipe's item Legendary" instead of
    // adding a second way to answer that -- Voltun's Set is already
    // registered there.
    //
    // Confirmed against the real 1.0 decompile: Player.HaveRequirements
    // (Recipe, bool, int, int) is the exact check InventoryGui.DoCrafting
    // gates the actual craft action on, so blocking it here blocks
    // crafting itself, not just a UI indicator. `discover` mode (used
    // for "is this recipe known at all" checks) is left alone -- the
    // level gate only applies to the real craft attempt.
    [HarmonyPatch(typeof(Player), nameof(Player.HaveRequirements), typeof(Recipe), typeof(bool), typeof(int), typeof(int))]
    public static class LegendaryCraftGatePatch
    {
        static bool Prefix(Player __instance, Recipe recipe, bool discover, ref bool __result)
        {
            if (discover) return true;
            if (recipe?.m_item == null) return true;

            ItemDrop.ItemData.SharedData shared = recipe.m_item.m_itemData.m_shared;
            if (!ItemRollTrigger.TryGetRollSpec(shared, out RarityTier tier, out _)) return true;
            if (tier != RarityTier.Legendary) return true;

            float level = __instance.GetSkills().GetSkillLevel(SmithingSkill.Type);
            if (level >= RarityLootPlugin.SmithingLegendaryCraftLevel.Value) return true;

            __result = false;
            return false;
        }
    }
}
