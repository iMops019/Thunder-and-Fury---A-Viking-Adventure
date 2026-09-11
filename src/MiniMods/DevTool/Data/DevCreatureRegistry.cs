using System.IO;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace ThunderFury.DevTool.Data
{
    public static class DevCreatureRegistry
    {
        public static readonly System.Collections.Generic.List<DevCreatureDefinition> Definitions =
            new System.Collections.Generic.List<DevCreatureDefinition>();

        static string FilePath => Path.Combine(DevToolPaths.DataDirectory, "creatures.json");

        public static void Load()
        {
            Definitions.Clear();
            DevToolPaths.EnsureDataDirectory();
            if (!File.Exists(FilePath)) return;

            string json = File.ReadAllText(FilePath);
            DevCreatureDefinitionList wrapper = JsonUtility.FromJson<DevCreatureDefinitionList>(json);
            if (wrapper?.Creatures != null) Definitions.AddRange(wrapper.Creatures);
        }

        public static void Save()
        {
            DevToolPaths.EnsureDataDirectory();
            File.WriteAllText(FilePath, JsonUtility.ToJson(new DevCreatureDefinitionList { Creatures = Definitions }, true));
        }

        public static void LoadAndRegisterAll()
        {
            Load();
            foreach (DevCreatureDefinition def in Definitions)
            {
                Register(def);
            }
        }

        public static bool Register(DevCreatureDefinition def)
        {
            var creatureConfig = new CreatureConfig
            {
                Name = string.IsNullOrEmpty(def.DisplayName) ? def.Name : def.DisplayName,
            };

            var customCreature = new CustomCreature(def.Name, def.BasePrefabName, creatureConfig);
            if (!customCreature.IsValid())
            {
                Jotunn.Logger.LogError($"DevTool: creature '{def.Name}' (base '{def.BasePrefabName}') is not valid, skipping");
                return false;
            }

            if (!Mathf.Approximately(def.HealthMultiplier, 1f))
            {
                Character character = customCreature.Prefab.GetComponent<Character>();
                if (character != null)
                {
                    character.m_health *= def.HealthMultiplier;
                }
            }

            return CreatureManager.Instance.AddCreature(customCreature);
        }
    }
}
