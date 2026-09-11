using System;
using System.Collections.Generic;

namespace ThunderFury.DevTool.Data
{
    // ---- Recipe Creator's data model ----
    //
    // Attaches a recipe (materials, station, station level) to any
    // existing item -- a vanilla item, or one already created by the Item
    // Creator -- rather than creating a new item itself (that's the Item
    // Creator's job, which auto-generates its own recipe). GateSkillName
    // empty means no skill/level gate; set it to opt into one, resolved
    // by name via Core's SkillRegistry and enforced by
    // Core's RecipeLevelGatePatch.
    [Serializable]
    public class DevRecipeDefinition
    {
        public string ItemName = "";
        public int Amount = 1;
        public string CraftingStation = "";
        public int MinStationLevel = 1;
        public List<DevRequirement> Requirements = new List<DevRequirement>();

        public string GateSkillName = "";
        public int GateLevel = 0;
    }

    [Serializable]
    public class DevRecipeDefinitionList
    {
        public List<DevRecipeDefinition> Recipes = new List<DevRecipeDefinition>();
    }
}
