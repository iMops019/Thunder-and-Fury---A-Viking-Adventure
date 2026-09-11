using System.IO;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace ThunderFury.DevTool.Data
{
    public static class DevPieceRegistry
    {
        public static readonly System.Collections.Generic.List<DevPieceDefinition> Definitions =
            new System.Collections.Generic.List<DevPieceDefinition>();

        static string FilePath => Path.Combine(DevToolPaths.DataDirectory, "pieces.json");

        public static void Load()
        {
            Definitions.Clear();
            DevToolPaths.EnsureDataDirectory();
            if (!File.Exists(FilePath)) return;

            string json = File.ReadAllText(FilePath);
            DevPieceDefinitionList wrapper = JsonUtility.FromJson<DevPieceDefinitionList>(json);
            if (wrapper?.Pieces != null) Definitions.AddRange(wrapper.Pieces);
        }

        public static void Save()
        {
            DevToolPaths.EnsureDataDirectory();
            File.WriteAllText(FilePath, JsonUtility.ToJson(new DevPieceDefinitionList { Pieces = Definitions }, true));
        }

        public static void LoadAndRegisterAll()
        {
            Load();
            foreach (DevPieceDefinition def in Definitions)
            {
                Register(def);
            }
        }

        public static bool Register(DevPieceDefinition def)
        {
            var pieceConfig = new PieceConfig
            {
                Name = string.IsNullOrEmpty(def.DisplayName) ? def.Name : def.DisplayName,
                Description = def.Description,
                Category = def.Category,
                PieceTable = def.PieceTable,
                CraftingStation = def.CraftingStation,
            };
            foreach (DevRequirement req in def.Requirements)
            {
                if (string.IsNullOrEmpty(req.ItemName)) continue;
                pieceConfig.AddRequirement(req.ItemName, req.Amount);
            }

            var customPiece = new CustomPiece(def.Name, def.BasePrefabName, pieceConfig);
            if (!customPiece.IsValid())
            {
                Jotunn.Logger.LogError($"DevTool: piece '{def.Name}' (base '{def.BasePrefabName}') is not valid, skipping");
                return false;
            }

            return PieceManager.Instance.AddPiece(customPiece);
        }
    }
}
