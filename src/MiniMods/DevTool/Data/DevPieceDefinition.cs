using System;
using System.Collections.Generic;

namespace ThunderFury.DevTool.Data
{
    // ---- Piece Creator's data model ----
    //
    // Same shape as DevItemDefinition, for buildable pieces instead of
    // items -- clones a vanilla piece via CustomPiece(name, baseName,
    // PieceConfig), same mechanism AdventureBoard.cs already proved out
    // by hand for the Quests mini-mod, generalized into a form so a new
    // buildable object doesn't require asking for a new hand-written .cs
    // file every time.
    [Serializable]
    public class DevPieceDefinition
    {
        public string Name = "";
        public string BasePrefabName = "";
        public string DisplayName = "";
        public string Description = "";

        public string Category = "";
        public string PieceTable = "";
        public string CraftingStation = "";

        public List<DevRequirement> Requirements = new List<DevRequirement>();
    }

    [Serializable]
    public class DevPieceDefinitionList
    {
        public List<DevPieceDefinition> Pieces = new List<DevPieceDefinition>();
    }
}
