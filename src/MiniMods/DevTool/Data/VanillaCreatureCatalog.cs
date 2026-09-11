using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThunderFury.DevTool.Data
{
    // ---- Live catalog of every creature that can drop loot ----
    //
    // Same "read the game's own data, don't hand-type a list" principle
    // as VanillaItemCatalog/ZoneLocationCatalog. Built from
    // ZNetScene.instance.m_prefabs (confirmed public on the real
    // assembly -- every network-synced prefab in the game, not just
    // items) filtered to anything with a CharacterDrop component, which
    // is the real vanilla mechanism every creature (bosses included)
    // already uses for its own loot table.
    public static class VanillaCreatureCatalog
    {
        public class Entry
        {
            public string PrefabName;
            public bool IsBoss;
        }

        // No dedicated "is this a boss" flag exists on CharacterDrop or
        // Character -- bosses are just creatures with a lot of health and
        // a boss-only UI hookup elsewhere. This is a small, honestly-a-guess
        // allowlist of the well-known boss prefab names for display/filtering
        // convenience only; it doesn't gate anything, a wrong/missing name
        // here just means that one entry doesn't get the "(Boss)" label.
        static readonly HashSet<string> KnownBossNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Eikthyr", "gd_king", "Bonemass", "Dragon", "GoblinKing", "SeekerQueen", "FaderQueen",
        };

        public static List<Entry> GetAll()
        {
            var result = new List<Entry>();
            if (ZNetScene.instance == null) return result;

            foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
            {
                if (prefab == null) continue;
                if (prefab.GetComponent<CharacterDrop>() == null) continue;

                result.Add(new Entry
                {
                    PrefabName = prefab.name,
                    IsBoss = KnownBossNames.Contains(prefab.name),
                });
            }

            result.Sort((a, b) => string.Compare(a.PrefabName, b.PrefabName, StringComparison.OrdinalIgnoreCase));
            return result;
        }
    }
}
