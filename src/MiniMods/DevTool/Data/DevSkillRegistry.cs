using System;
using System.Collections.Generic;
using System.IO;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;
using ThunderFury.Core.SkillSystem;

namespace ThunderFury.DevTool.Data
{
    // ---- Skill Creator: load, persist, and register DevTool-authored skills ----
    //
    // Registration mirrors WoodcuttingSkill.Register/etc. exactly:
    // SkillManager.Instance.AddSkill(config) for the skill itself, then
    // SkillXpRedirect.Register for the optional XP source -- the same two
    // calls every one of Core's 12 skills makes by hand, generalized into
    // data.
    public static class DevSkillRegistry
    {
        public static readonly List<DevSkillDefinition> Definitions = new List<DevSkillDefinition>();
        public static readonly Dictionary<string, global::Skills.SkillType> RegisteredTypes =
            new Dictionary<string, global::Skills.SkillType>();

        public static void Load()
        {
            Definitions.Clear();
            DevToolPaths.EnsureDataDirectory();
            string path = Path.Combine(DevToolPaths.DataDirectory, "skills.json");
            if (!File.Exists(path)) return;

            string json = File.ReadAllText(path);
            DevSkillDefinitionList wrapper = JsonUtility.FromJson<DevSkillDefinitionList>(json);
            if (wrapper?.Skills != null) Definitions.AddRange(wrapper.Skills);
        }

        public static void Save()
        {
            DevToolPaths.EnsureDataDirectory();
            string path = Path.Combine(DevToolPaths.DataDirectory, "skills.json");
            string json = JsonUtility.ToJson(new DevSkillDefinitionList { Skills = Definitions }, true);
            File.WriteAllText(path, json);
        }

        public static void LoadAndRegisterAll()
        {
            Load();
            foreach (DevSkillDefinition def in Definitions)
            {
                Register(def);
            }
        }

        public static bool Register(DevSkillDefinition def)
        {
            var config = new SkillConfig
            {
                Identifier = "com.ThunderFury.devtool.skill." + def.Name,
                Name = def.Name,
                Description = def.Description,
                IncreaseStep = 1f,
            };

            global::Skills.SkillType type = SkillManager.Instance.AddSkill(config);
            RegisteredTypes[def.Name] = type;

            if (!string.IsNullOrEmpty(def.XpSourceVanillaSkill) &&
                Enum.TryParse(def.XpSourceVanillaSkill, out global::Skills.SkillType vanillaType))
            {
                if (SkillXpRedirect.IsRedirected(vanillaType))
                {
                    Jotunn.Logger.LogWarning(
                        $"DevTool Skill Creator: '{def.XpSourceVanillaSkill}' is already redirected to another skill -- " +
                        $"'{def.Name}' was registered as an orphan skill instead, to avoid silently stealing another skill's XP.");
                    return true;
                }

                SkillXpRedirect.Register(vanillaType, type);
            }

            return true;
        }
    }
}
