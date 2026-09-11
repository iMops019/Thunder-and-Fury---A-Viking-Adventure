using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ThunderFury.Core.Loot;

namespace ThunderFury.DevTool.Data
{
    // ---- Editor-side persistence for "which creatures can drop Legendaries" ----
    //
    // Answers the user's own request (2026-09-10: "add a field to what
    // can drop the legendaries") as a creature-level list rather than a
    // per-item one -- the ambient biome drop system
    // (RarityLoot/Patches/AmbientBiomeDropPatch.cs) rolls a random base
    // item at kill time, so Legendary eligibility has to be a property of
    // the CREATURE, not any one item. DevTool owns the JSON + UI here,
    // same "dumb registry in Core, mini-mod owns persistence" split
    // SkillRegistry/QuestRegistry already established -- RarityLoot only
    // ever reads Core.Loot.LegendaryDropSourceRegistry, never this file.
    [Serializable]
    public class LegendaryDropSourceEntry
    {
        public string CreatureName = "";
    }

    [Serializable]
    public class LegendaryDropSourceList
    {
        public List<LegendaryDropSourceEntry> Entries = new List<LegendaryDropSourceEntry>();
    }

    public static class DevLegendaryDropSourceRegistry
    {
        public static readonly List<LegendaryDropSourceEntry> Entries = new List<LegendaryDropSourceEntry>();

        static string FilePath => Path.Combine(DevToolPaths.DataDirectory, "legendaryDropSources.json");

        public static void Load()
        {
            Entries.Clear();
            DevToolPaths.EnsureDataDirectory();
            if (!File.Exists(FilePath)) return;

            string json = File.ReadAllText(FilePath);
            LegendaryDropSourceList wrapper = JsonUtility.FromJson<LegendaryDropSourceList>(json);
            if (wrapper?.Entries != null) Entries.AddRange(wrapper.Entries);
        }

        public static void Save()
        {
            DevToolPaths.EnsureDataDirectory();
            File.WriteAllText(FilePath, JsonUtility.ToJson(new LegendaryDropSourceList { Entries = Entries }, true));
        }

        public static void LoadAndApplyAll()
        {
            Load();

            LegendaryDropSourceRegistry.Eligible.Clear();
            foreach (LegendaryDropSourceEntry entry in Entries)
            {
                if (!string.IsNullOrEmpty(entry.CreatureName))
                {
                    LegendaryDropSourceRegistry.Eligible.Add(entry.CreatureName);
                }
            }
        }
    }
}
