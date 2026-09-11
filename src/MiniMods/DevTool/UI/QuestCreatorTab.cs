using System;
using System.Collections.Generic;
using System.Linq;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;
using ThunderFury.Core.Quests;
using ThunderFury.Core.SkillSystem;
using ThunderFury.DevTool.Data;
using static ThunderFury.DevTool.UI.DevToolUiHelpers;

namespace ThunderFury.DevTool.UI
{
    // ---- Quest Creator tab ----
    //
    // Closes the biggest remaining create/change/modify gap (user's own
    // audit, 2026-09-10): the whole quest chain used to be hardcoded C#
    // (QuestDatabase.cs, removed the same session this tab was built).
    // QuestDefinition/QuestObjectiveType moved to Core.Quests so both
    // this tab and the Quests mini-mod's own runtime code (tracking
    // patches, the Adventure Board panel) share one live list
    // (Core.Quests.QuestRegistry.All) without either depending on the
    // other. Same create/edit/remove shape as Items/Recipes.
    //
    // Target field's meaning depends on Objective Type (KillCreature:
    // creature prefab name; DiscoverBiome: a Heightmap.Biome member name;
    // OwnLegendaryItem: unused) -- offering a creature Browse button
    // covers the least-guessable case; biome names are short, well-known
    // words already spelled out in the hint text.
    public class QuestCreatorTab : DevToolOverlay.ITab
    {
        public string Title => "Quests";

        static readonly string[] BiomeNames =
        {
            "None", "Meadows", "BlackForest", "Swamp", "Mountain",
            "Plains", "Mistlands", "AshLands", "DeepNorth", "Ocean",
        };

        InputField _idField;
        InputField _titleField;
        InputField _descField;
        Dropdown _objectiveDropdown;
        InputField _targetField;
        InputField _targetCountField;
        Dropdown _requiredBiomeDropdown;
        InputField _rewardItemField;
        InputField _rewardItemAmountField;
        Dropdown _rewardSkillDropdown;
        List<string> _rewardSkillOptions;
        InputField _rewardSkillXpField;
        Dropdown _nextQuestDropdown;
        List<string> _nextQuestIds;
        Transform _listParent;

        public void Build(Transform contentParent)
        {
            GUIManager gui = GUIManager.Instance;

            TextRow(gui, contentParent, "Create / Edit a Quest", true);
            TextRow(gui, contentParent,
                "Target's meaning depends on Objective Type -- KillCreature: creature name (use Browse). DiscoverBiome: type a biome name exactly (Meadows/BlackForest/Swamp/Mountain/Plains/Mistlands/AshLands/DeepNorth/Ocean). OwnLegendaryItem: leave blank.",
                false, 60f);

            _idField = FieldRow(gui, contentParent, "Quest ID (unique, no spaces)", "");
            _titleField = FieldRow(gui, contentParent, "Title", "");
            _descField = FieldRow(gui, contentParent, "Description", "");

            _objectiveDropdown = DropdownRow(gui, contentParent, "Objective Type",
                new List<string> { "KillCreature", "DiscoverBiome", "OwnLegendaryItem" });

            _targetField = FieldRowWithCreaturePicker(gui, contentParent, "Target", "");
            _targetCountField = FieldRow(gui, contentParent, "Target Count (e.g. kill N)", "1");

            _requiredBiomeDropdown = DropdownRow(gui, contentParent, "Required Biome Gate (optional)", new List<string>(BiomeNames));

            _rewardItemField = FieldRowWithPicker(gui, contentParent, "Reward Item (optional)", "");
            _rewardItemAmountField = FieldRow(gui, contentParent, "Reward Item Amount", "0");

            _rewardSkillOptions = new List<string> { "None" };
            _rewardSkillOptions.AddRange(SkillRegistry.Names);
            _rewardSkillDropdown = DropdownRow(gui, contentParent, "Reward Skill (optional)", _rewardSkillOptions);
            _rewardSkillXpField = FieldRow(gui, contentParent, "Reward Skill XP", "0");

            _nextQuestIds = new List<string> { "None" };
            _nextQuestDropdown = DropdownRow(gui, contentParent, "Next Quest (unlocks on completion)", _nextQuestIds);

            GameObject saveButton = Button(gui, contentParent, "Save & Register");
            saveButton.GetComponent<Button>().onClick.AddListener(() => SaveQuest(gui));

            GameObject clearButton = Button(gui, contentParent, "Clear Form (New Quest)");
            clearButton.GetComponent<Button>().onClick.AddListener(() => LoadIntoForm(new QuestDefinition { TargetCount = 1 }));

            TextRow(gui, contentParent, "Existing Quests", true);
            GameObject listContainer = new GameObject("QuestList", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            listContainer.transform.SetParent(contentParent, false);
            VerticalLayoutGroup listLayout = listContainer.GetComponent<VerticalLayoutGroup>();
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;
            listLayout.childControlHeight = true;
            listContainer.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _listParent = listContainer.transform;

            RefreshNextQuestOptions();
            RefreshList(gui);
        }

        void RefreshNextQuestOptions()
        {
            _nextQuestIds = new List<string> { "None" };
            _nextQuestIds.AddRange(QuestRegistry.All.Select(q => q.Id));

            _nextQuestDropdown.ClearOptions();
            _nextQuestDropdown.AddOptions(_nextQuestIds);
        }

        void SaveQuest(GUIManager gui)
        {
            string id = _idField.text.Trim();
            if (string.IsNullOrEmpty(id))
            {
                Jotunn.Logger.LogWarning("DevTool Quest Creator: Quest ID is required");
                return;
            }

            string nextId = _nextQuestDropdown.value == 0 ? "" : _nextQuestIds[_nextQuestDropdown.value];
            string requiredBiome = _requiredBiomeDropdown.value == 0 ? "" : BiomeNames[_requiredBiomeDropdown.value];
            string rewardSkill = _rewardSkillDropdown.value == 0 ? "" : _rewardSkillOptions[_rewardSkillDropdown.value];

            QuestDefinition def = new QuestDefinition
            {
                Id = id,
                Title = _titleField.text.Trim(),
                Description = _descField.text.Trim(),
                ObjectiveType = (QuestObjectiveType)_objectiveDropdown.value,
                Target = _targetField.text.Trim(),
                TargetCount = ParseInt(_targetCountField.text, 1),
                RequiredBiome = requiredBiome,
                RewardItemName = _rewardItemField.text.Trim(),
                RewardItemAmount = ParseInt(_rewardItemAmountField.text, 0),
                RewardSkillName = rewardSkill,
                RewardSkillXp = ParseInt(_rewardSkillXpField.text, 0),
                NextQuestId = nextId,
            };

            QuestRegistry.All.RemoveAll(existing => existing.Id == def.Id);
            QuestRegistry.All.Add(def);
            DevQuestRegistry.SaveCurrent();

            Player.m_localPlayer?.Message(MessageHud.MessageType.Center,
                $"Saved quest '{def.Title}'. Live immediately for anyone at the Adventure Board this session.");

            RefreshNextQuestOptions();
            RefreshList(gui);
        }

        void LoadIntoForm(QuestDefinition def)
        {
            _idField.text = def.Id;
            _titleField.text = def.Title;
            _descField.text = def.Description;
            _objectiveDropdown.value = (int)def.ObjectiveType;
            _targetField.text = def.Target;
            _targetCountField.text = def.TargetCount.ToString();

            int biomeIndex = string.IsNullOrEmpty(def.RequiredBiome) ? 0 : Array.IndexOf(BiomeNames, def.RequiredBiome);
            _requiredBiomeDropdown.value = biomeIndex >= 0 ? biomeIndex : 0;

            _rewardItemField.text = def.RewardItemName;
            _rewardItemAmountField.text = def.RewardItemAmount.ToString();

            int skillIndex = string.IsNullOrEmpty(def.RewardSkillName) ? 0 : _rewardSkillOptions.IndexOf(def.RewardSkillName);
            _rewardSkillDropdown.value = skillIndex >= 0 ? skillIndex : 0;
            _rewardSkillXpField.text = def.RewardSkillXp.ToString();

            int nextIndex = string.IsNullOrEmpty(def.NextQuestId) ? 0 : _nextQuestIds.IndexOf(def.NextQuestId);
            _nextQuestDropdown.value = nextIndex >= 0 ? nextIndex : 0;
        }

        void RefreshList(GUIManager gui)
        {
            for (int i = _listParent.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(_listParent.GetChild(i).gameObject);
            }

            foreach (QuestDefinition def in QuestRegistry.All)
            {
                GameObject row = new GameObject("QuestRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
                row.transform.SetParent(_listParent, false);
                row.GetComponent<LayoutElement>().preferredHeight = RowHeight;
                row.GetComponent<HorizontalLayoutGroup>().childControlWidth = false;
                row.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
                row.GetComponent<HorizontalLayoutGroup>().spacing = 10f;

                string nextSuffix = string.IsNullOrEmpty(def.NextQuestId) ? "" : $"  -> {def.NextQuestId}";
                gui.CreateText($"{def.Id}: {def.Title} [{def.ObjectiveType}]{nextSuffix}", row.transform,
                    new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero,
                    gui.AveriaSerif, 14, Color.white, false, Color.black, 500f, RowHeight, false);

                GameObject editButton = gui.CreateButton("Edit", row.transform, Vector2.zero, Vector2.zero, Vector2.zero, 90f, 28f);
                editButton.GetComponent<Button>().onClick.AddListener(() => LoadIntoForm(def));

                GameObject duplicateButton = gui.CreateButton("Duplicate", row.transform, Vector2.zero, Vector2.zero, Vector2.zero, 110f, 28f);
                duplicateButton.GetComponent<Button>().onClick.AddListener(() =>
                {
                    LoadIntoForm(def);
                    _idField.text = "";
                });

                GameObject removeButton = gui.CreateButton("Remove", row.transform, Vector2.zero, Vector2.zero, Vector2.zero, 100f, 28f);
                removeButton.GetComponent<Button>().onClick.AddListener(() =>
                {
                    QuestRegistry.All.Remove(def);
                    DevQuestRegistry.SaveCurrent();
                    RefreshNextQuestOptions();
                    RefreshList(gui);
                });
            }
        }
    }
}
