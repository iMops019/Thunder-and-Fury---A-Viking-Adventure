using System.Collections.Generic;
using System.IO;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using ThunderFury.Core.SkillSystem;

namespace ThunderFury.DevTool.Data
{
    // ---- Recipe Creator: load, persist, and register DevTool-authored recipes ----
    //
    // RecipeConfig + CustomRecipe(RecipeConfig) is the exact mechanism
    // Jotunn's own ItemManager.AddRecipesFromJson uses internally --
    // registering through the same CustomRecipe type here (rather than
    // calling AddRecipesFromJson directly) so the optional skill/level
    // gate can be wired up in the same pass as the recipe itself.
    public static class DevRecipeRegistry
    {
        public static readonly List<DevRecipeDefinition> Definitions = new List<DevRecipeDefinition>();

        public static void Load()
        {
            Definitions.Clear();
            DevToolPaths.EnsureDataDirectory();
            if (!File.Exists(DevToolPaths.RecipesFile)) return;

            string json = File.ReadAllText(DevToolPaths.RecipesFile);
            DevRecipeDefinitionList wrapper = JsonUtility.FromJson<DevRecipeDefinitionList>(json);
            if (wrapper?.Recipes != null) Definitions.AddRange(wrapper.Recipes);
        }

        public static void Save()
        {
            DevToolPaths.EnsureDataDirectory();
            string json = JsonUtility.ToJson(new DevRecipeDefinitionList { Recipes = Definitions }, true);
            File.WriteAllText(DevToolPaths.RecipesFile, json);
        }

        public static void LoadAndRegisterAll()
        {
            Load();
            foreach (DevRecipeDefinition def in Definitions)
            {
                Register(def);
            }
        }

        public static bool Register(DevRecipeDefinition def)
        {
            var recipeConfig = new RecipeConfig
            {
                Item = def.ItemName,
                Amount = def.Amount,
                CraftingStation = def.CraftingStation,
                MinStationLevel = def.MinStationLevel,
            };
            foreach (DevRequirement req in def.Requirements)
            {
                if (string.IsNullOrEmpty(req.ItemName)) continue;
                recipeConfig.AddRequirement(req.ItemName, req.Amount, req.AmountPerLevel);
            }

            var customRecipe = new CustomRecipe(recipeConfig);
            if (!customRecipe.IsValid())
            {
                Jotunn.Logger.LogError($"DevTool: recipe for '{def.ItemName}' is not valid, skipping");
                return false;
            }

            bool added = ItemManager.Instance.AddRecipe(customRecipe);
            if (added && !string.IsNullOrEmpty(def.GateSkillName))
            {
                RecipeLevelGate.Register(def.ItemName, def.GateSkillName, def.GateLevel);
            }

            return added;
        }
    }
}
