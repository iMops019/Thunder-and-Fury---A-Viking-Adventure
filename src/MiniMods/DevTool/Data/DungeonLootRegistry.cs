using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ThunderFury.DevTool.Data
{
    public static class DungeonLootRegistry
    {
        public static readonly List<DungeonLootEntry> Entries = new List<DungeonLootEntry>();

        static string FilePath => Path.Combine(DevToolPaths.DataDirectory, "dungeonLoot.json");

        public static void Load()
        {
            Entries.Clear();
            DevToolPaths.EnsureDataDirectory();
            if (!File.Exists(FilePath)) return;

            string json = File.ReadAllText(FilePath);
            DungeonLootPoolList wrapper = JsonUtility.FromJson<DungeonLootPoolList>(json);
            if (wrapper?.Entries != null) Entries.AddRange(wrapper.Entries);
        }

        public static void Save()
        {
            DevToolPaths.EnsureDataDirectory();
            File.WriteAllText(FilePath, JsonUtility.ToJson(new DungeonLootPoolList { Entries = Entries }, true));
        }
    }
}
