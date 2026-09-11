using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ThunderFury.Core.Quests;

namespace ThunderFury.DevTool.Data
{
    // JsonUtility needs a wrapper object at the root -- same pattern as
    // every other DevTool-persisted list.
    [Serializable]
    public class DevQuestList
    {
        public List<QuestDefinition> Quests = new List<QuestDefinition>();
    }

    // ---- Quest Creator: load, persist, and apply DevTool-authored quests ----
    //
    // Core.Quests.QuestRegistry.All is the live list Quests' own runtime
    // code (tracking patches, the Adventure Board panel) actually reads --
    // this is the editor-side owner of it, same shape as
    // DevItemRegistry/DevRecipeRegistry/DevSkillRegistry.
    //
    // First-run seeding: on a fresh install with no quests.json yet, this
    // writes out the original hand-authored 3-quest chain (Find the Black
    // Forest -> Find and Defeat The Elder -> Prove Your Legend) as the
    // STARTING content rather than losing it -- it used to be hardcoded
    // C# in the Quests mini-mod (QuestDatabase.cs, removed 2026-09-10 when
    // this tab was built) and is now just the initial value of the
    // editable list, exactly like every other piece of content this
    // session moved from "hand-written code" to "data."
    public static class DevQuestRegistry
    {
        static string FilePath => Path.Combine(DevToolPaths.DataDirectory, "quests.json");

        public static void LoadAndApplyAll()
        {
            DevToolPaths.EnsureDataDirectory();

            List<QuestDefinition> quests;
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                DevQuestList wrapper = JsonUtility.FromJson<DevQuestList>(json);
                quests = wrapper?.Quests ?? new List<QuestDefinition>();
            }
            else
            {
                quests = BuildDefaultChain();
                Save(quests);
            }

            QuestRegistry.All.Clear();
            QuestRegistry.All.AddRange(quests);
        }

        public static void Save(List<QuestDefinition> quests)
        {
            DevToolPaths.EnsureDataDirectory();
            File.WriteAllText(FilePath, JsonUtility.ToJson(new DevQuestList { Quests = quests }, true));
        }

        // Convenience for the tab: saves whatever's currently in
        // QuestRegistry.All (the tab mutates that list directly, same
        // "the live list IS the editable list" approach the UI already
        // uses for e.g. Biome/Dungeon location quantities).
        public static void SaveCurrent() => Save(QuestRegistry.All);

        static List<QuestDefinition> BuildDefaultChain()
        {
            const string findBlackForestId = "find_blackforest";
            const string huntElderId = "hunt_elder";
            const string legendaryCallingId = "legendary_calling";

            return new List<QuestDefinition>
            {
                new QuestDefinition
                {
                    Id = findBlackForestId,
                    Title = "Find the Black Forest",
                    Description = "Head out from the Meadows and set foot in the Black Forest for the first time.",
                    ObjectiveType = QuestObjectiveType.DiscoverBiome,
                    Target = global::Heightmap.Biome.BlackForest.ToString(),
                    TargetCount = 1,
                    RequiredBiome = "",
                    RewardItemName = "Coins",
                    RewardItemAmount = 20,
                    NextQuestId = huntElderId,
                },
                new QuestDefinition
                {
                    Id = huntElderId,
                    Title = "Find and Defeat The Elder",
                    Description = "The Black Forest's ancient guardian awaits. Track down and slay The Elder.",
                    ObjectiveType = QuestObjectiveType.KillCreature,
                    Target = "gd_king",
                    TargetCount = 1,
                    RequiredBiome = global::Heightmap.Biome.BlackForest.ToString(),
                    RewardItemName = "Coins",
                    RewardItemAmount = 100,
                    RewardSkillName = "Attack",
                    RewardSkillXp = 200,
                    NextQuestId = legendaryCallingId,
                },
                new QuestDefinition
                {
                    Id = legendaryCallingId,
                    Title = "Prove Your Legend",
                    Description = "Forge or find a weapon worthy of legend -- own any Legendary-tier item.",
                    ObjectiveType = QuestObjectiveType.OwnLegendaryItem,
                    RequiredBiome = global::Heightmap.Biome.BlackForest.ToString(),
                    RewardSkillName = "Smithing",
                    RewardSkillXp = 300,
                },
            };
        }
    }
}
