using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ThunderFury.DevTool.Data
{
    // ---- Biome/Dungeon tabs: persist and reapply location spawn-quantity overrides ----
    //
    // Same shape as DevWorldSettingsRegistry: ZoneLocation.m_quantity is a
    // real, live, public field, but ZoneSystem builds its placement plan
    // from these values once per session (and per world seed's already-
    // generated terrain) rather than exposing a "re-place everything"
    // save key -- so this remembers overrides in Dev Tool's own JSON and
    // reapplies them on every Game.Start, the same real hook
    // WorldSettingsReapplyPatch already uses.
    //
    // Honest limitation, not hidden: raising a quantity only affects
    // zones the world generates/explores AFTER the change -- it can't
    // retroactively add more instances of something to terrain that's
    // already been generated and explored.
    public static class DevZoneSettingsRegistry
    {
        public static readonly List<DevZoneQuantityOverride> Overrides = new List<DevZoneQuantityOverride>();

        static string FilePath => Path.Combine(DevToolPaths.DataDirectory, "zoneQuantities.json");

        public static void Load()
        {
            Overrides.Clear();
            DevToolPaths.EnsureDataDirectory();
            if (!File.Exists(FilePath)) return;

            string json = File.ReadAllText(FilePath);
            DevZoneSettingsList wrapper = JsonUtility.FromJson<DevZoneSettingsList>(json);
            if (wrapper?.Overrides != null) Overrides.AddRange(wrapper.Overrides);
        }

        public static void Save()
        {
            DevToolPaths.EnsureDataDirectory();
            File.WriteAllText(FilePath, JsonUtility.ToJson(new DevZoneSettingsList { Overrides = Overrides }, true));
        }

        public static void SetQuantity(string prefabName, int quantity)
        {
            DevZoneQuantityOverride existing = Overrides.Find(o => o.PrefabName == prefabName);
            if (existing != null)
            {
                existing.Quantity = quantity;
            }
            else
            {
                Overrides.Add(new DevZoneQuantityOverride { PrefabName = prefabName, Quantity = quantity });
            }
        }

        public static void Apply()
        {
            Load();
            if (Overrides.Count == 0) return;
            if (ZoneSystem.instance == null) return;

            foreach (ZoneSystem.ZoneLocation location in ZoneSystem.instance.m_locations)
            {
                if (location == null || !location.m_prefab.IsValid) continue;
                GameObject asset = location.m_prefab.Asset;
                if (asset == null) continue;

                DevZoneQuantityOverride match = Overrides.Find(o => o.PrefabName == asset.name);
                if (match != null)
                {
                    location.m_quantity = match.Quantity;
                }
            }

            Jotunn.Logger.LogInfo("DevTool: reapplied saved Biome/Dungeon spawn-quantity overrides for this session.");
        }
    }
}
