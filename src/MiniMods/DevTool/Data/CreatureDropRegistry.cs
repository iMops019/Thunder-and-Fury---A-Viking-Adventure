using System.Collections.Generic;
using System.IO;
using Jotunn.Managers;
using UnityEngine;

namespace ThunderFury.DevTool.Data
{
    public static class CreatureDropRegistry
    {
        public static readonly List<CreatureDropEntry> Entries = new List<CreatureDropEntry>();

        static string FilePath => Path.Combine(DevToolPaths.DataDirectory, "creatureDrops.json");

        public static void Load()
        {
            Entries.Clear();
            DevToolPaths.EnsureDataDirectory();
            if (!File.Exists(FilePath)) return;

            string json = File.ReadAllText(FilePath);
            CreatureDropPoolList wrapper = JsonUtility.FromJson<CreatureDropPoolList>(json);
            if (wrapper?.Entries != null) Entries.AddRange(wrapper.Entries);
        }

        public static void Save()
        {
            DevToolPaths.EnsureDataDirectory();
            File.WriteAllText(FilePath, JsonUtility.ToJson(new CreatureDropPoolList { Entries = Entries }, true));
        }

        // Adds each entry to the target creature's own CharacterDrop.m_drops --
        // the exact same list every vanilla creature's meat/trophy/material
        // drops already live in, so a registered drop shows up in the death
        // loot roll for free via vanilla's own CharacterDrop.OnDeath logic,
        // no separate drop-rolling code needed here.
        //
        // Guarded against duplicate entries (checked by item prefab name on
        // the same creature) since m_drops is a bare List with no built-in
        // duplicate protection, unlike Jotunn's own item/recipe manager
        // dictionaries -- without this, re-registering on a second
        // PrefabManager.OnVanillaPrefabsAvailable firing in the same
        // session would stack the same drop entry repeatedly.
        public static void LoadAndApplyAll()
        {
            Load();

            foreach (CreatureDropEntry entry in Entries)
            {
                if (string.IsNullOrEmpty(entry.CreatureName) || string.IsNullOrEmpty(entry.ItemName)) continue;

                GameObject creaturePrefab = PrefabManager.Instance.GetPrefab(entry.CreatureName);
                if (creaturePrefab == null)
                {
                    Jotunn.Logger.LogWarning($"DevTool: could not resolve creature '{entry.CreatureName}' for a registered drop, skipping");
                    continue;
                }

                CharacterDrop drop = creaturePrefab.GetComponent<CharacterDrop>();
                if (drop == null) continue;

                GameObject itemPrefab = PrefabManager.Instance.GetPrefab(entry.ItemName);
                if (itemPrefab == null)
                {
                    Jotunn.Logger.LogWarning($"DevTool: could not resolve item '{entry.ItemName}' for a registered drop on '{entry.CreatureName}', skipping");
                    continue;
                }

                if (drop.m_drops.Exists(d => d.m_prefab != null && d.m_prefab.name == itemPrefab.name)) continue;

                drop.m_drops.Add(new CharacterDrop.Drop
                {
                    m_prefab = itemPrefab,
                    m_amountMin = entry.MinAmount,
                    m_amountMax = entry.MaxAmount,
                    m_chance = entry.ChancePercent / 100f,
                    m_onePerPlayer = entry.OnePerPlayer,
                });
            }
        }
    }
}
