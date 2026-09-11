using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThunderFury.DevTool.Data
{
    // ---- Live catalog of every buildable piece ----
    //
    // Same "read the game's own data" principle as every other catalog
    // built this session. Pieces are network-synced prefabs like anything
    // else placeable, so they live in ZNetScene.instance.m_prefabs
    // alongside items and creatures -- filtered here to anything with a
    // Piece component.
    public static class VanillaPieceCatalog
    {
        public class Entry
        {
            public string PrefabName;
        }

        public static List<Entry> GetAll()
        {
            var result = new List<Entry>();
            if (ZNetScene.instance == null) return result;

            foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
            {
                if (prefab == null) continue;
                if (prefab.GetComponent<Piece>() == null) continue;

                result.Add(new Entry { PrefabName = prefab.name });
            }

            result.Sort((a, b) => string.Compare(a.PrefabName, b.PrefabName, StringComparison.OrdinalIgnoreCase));
            return result;
        }
    }
}
