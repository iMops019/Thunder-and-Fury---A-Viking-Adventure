namespace ThunderFury.Core.SkillSystem
{
    // ---- Known-skill lookup, by name ----
    //
    // Jotunn assigns each custom skill's SkillType at runtime
    // (SkillManager.AddSkill), so unlike vanilla's own SkillType entries
    // there's no compile-time constant for e.g. "Smithing" -- anything
    // that needs to resolve one of our skills by name (so far: the Dev
    // Tool's Recipe Creator level-gate picker, and RecipeLevelGatePatch
    // below which reads this at craft-attempt time) needs this lookup
    // instead of the enum value directly. Built fresh on every call
    // rather than cached, since it's cheap (12 dictionary entries) and
    // avoids any question of whether it was captured before or after
    // every skill's own PrefabManager.OnVanillaPrefabsAvailable-driven
    // Register() has actually run.
    public static class SkillRegistry
    {
        public static bool TryGet(string name, out global::Skills.SkillType type)
        {
            switch (name)
            {
                case "Woodcutting": type = WoodcuttingSkill.Type; return true;
                case "Mining": type = MiningSkill.Type; return true;
                case "Fishing": type = FishingSkill.Type; return true;
                case "Skinning": type = SkinningSkill.Type; return true;
                case "Smithing": type = SmithingSkill.Type; return true;
                case "Cooking": type = CookingSkill.Type; return true;
                case "Fletching": type = FletchingSkill.Type; return true;
                case "Building": type = BuildingSkill.Type; return true;
                case "Crafting": type = CraftingSkill.Type; return true;
                case "Attack": type = AttackSkill.Type; return true;
                case "Strength": type = StrengthSkill.Type; return true;
                case "Defense": type = DefenseSkill.Type; return true;
                default: type = default; return false;
            }
        }

        public static readonly string[] Names =
        {
            "Woodcutting", "Mining", "Fishing", "Skinning",
            "Smithing", "Cooking", "Fletching", "Building", "Crafting",
            "Attack", "Strength", "Defense",
        };
    }
}
