using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Managers;

namespace ThunderFury.Core.SkillSystem
{
    // ---- Fletching ----
    //
    // Eighth skill, and unlike every other skill built this session,
    // docs/valheim-mod-vision.md never gave Fletching its own design
    // section -- it's named once in the skill list ("Production:
    // Smithing, Cooking, Fletching, Building, Crafting") with nothing
    // else decided. So this deliberately builds ONLY what that one line
    // actually commits to -- a distinct production skill for crafting
    // bows and arrows, separate from Smithing -- and invents no gameplay
    // effects (no fail chance, no milestone, nothing) the way Cooking's
    // burn-prevention or Woodcutting's milestone had real vision.md text
    // to build from.
    //
    // Real technical problem this solves, found while wiring it up:
    // Smithing (this session, earlier) reuses
    // CraftingStation.m_craftingSkill to get free XP-granting from
    // vanilla's own InventoryGui.DoCrafting -- but that field is
    // per-STATION, not per-recipe, and bows/arrows craft at the same
    // Workbench as every other early tool Smithing already claims. A
    // station can only declare one skill, so reusing that same
    // mechanism for Fletching would just fight Smithing over the same
    // field. Fixed by intercepting at the one place that DOES know which
    // recipe is actually being crafted: InventoryGui.m_craftRecipe
    // (confirmed public), checked in a Prefix on Player.RaiseSkill
    // specifically when the skill about to be raised is Smithing's (i.e.
    // it came from a station-level grant) -- if the recipe being crafted
    // is a Bow or Ammo item, this reclassifies that XP as Fletching
    // instead. Everything else crafted at Workbench/Forge/Black Forge
    // still goes to Smithing untouched.
    public static class FletchingSkill
    {
        public const string Identifier = "com.ThunderFury.core.skill.fletching";

        public static global::Skills.SkillType Type { get; private set; }

        public static void Register()
        {
            var config = new SkillConfig
            {
                Identifier = Identifier,
                Name = "Fletching",
                Description = "Crafting bows and arrows.",
                IncreaseStep = 1f,
            };

            Type = SkillManager.Instance.AddSkill(config);
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.RaiseSkill))]
    public static class FletchingCraftRedirectPatch
    {
        static bool Prefix(Player __instance, global::Skills.SkillType skill, float value)
        {
            if (skill != SmithingSkill.Type) return true;

            Recipe recipe = InventoryGui.instance != null ? InventoryGui.instance.m_craftRecipe : null;
            ItemDrop item = recipe != null ? recipe.m_item : null;
            if (item == null) return true;

            ItemDrop.ItemData.ItemType itemType = item.m_itemData.m_shared.m_itemType;
            bool isFletchingItem = itemType == ItemDrop.ItemData.ItemType.Bow
                || itemType == ItemDrop.ItemData.ItemType.Ammo
                || itemType == ItemDrop.ItemData.ItemType.AmmoNonEquipable;
            if (!isFletchingItem) return true;

            __instance.GetSkills().RaiseSkill(FletchingSkill.Type, value);
            return false;
        }
    }
}
