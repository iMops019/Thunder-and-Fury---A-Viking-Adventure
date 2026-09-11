using System.Collections.Generic;
using UnityEngine;

namespace ThunderFury.DevTool.Data
{
    // ---- Live catalog of every placeable location (points of interest
    // and dungeons alike) ----
    //
    // Built from ZoneSystem.instance.m_locations at the moment it's
    // needed, same "read the game's own live data instead of a hand-typed
    // list" principle as VanillaItemCatalog. Confirmed via decompile of
    // the real (non-publicized) assemblies: ZoneSystem.m_locations,
    // ZoneLocation.m_biome/m_quantity are all genuinely public, and
    // SoftReference<T>.Asset (used to get the actual prefab GameObject
    // out of ZoneLocation.m_prefab) lazily loads on demand rather than
    // requiring a separate preload step.
    //
    // A "dungeon" is just a location whose prefab has a DungeonGenerator
    // component somewhere in its hierarchy -- confirmed there's no
    // separate "is this a dungeon" flag anywhere in the data, the
    // component's presence IS the distinction vanilla itself uses.
    public static class ZoneLocationCatalog
    {
        public class Entry
        {
            public ZoneSystem.ZoneLocation Location;
            public string PrefabName;
            public Heightmap.Biome Biome;
            public bool IsDungeon;
        }

        public static List<Entry> GetAll()
        {
            var result = new List<Entry>();
            if (ZoneSystem.instance == null) return result;

            foreach (ZoneSystem.ZoneLocation location in ZoneSystem.instance.m_locations)
            {
                if (location == null || !location.m_prefab.IsValid) continue;

                GameObject asset = location.m_prefab.Asset;
                if (asset == null) continue;

                result.Add(new Entry
                {
                    Location = location,
                    PrefabName = asset.name,
                    Biome = location.m_biome,
                    IsDungeon = asset.GetComponentInChildren<DungeonGenerator>(true) != null,
                });
            }

            result.Sort((a, b) => string.Compare(a.PrefabName, b.PrefabName, System.StringComparison.OrdinalIgnoreCase));
            return result;
        }
    }
}
