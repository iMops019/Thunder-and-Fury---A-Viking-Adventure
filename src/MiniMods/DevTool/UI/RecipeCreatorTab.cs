using System.Collections.Generic;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;
using ThunderFury.Core.SkillSystem;
using ThunderFury.DevTool.Data;
using static ThunderFury.DevTool.UI.DevToolUiHelpers;

namespace ThunderFury.DevTool.UI
{
    // ---- Recipe Creator tab ----
    //
    // Attaches a recipe to any existing item -- vanilla, or one already
    // made with the Item Creator -- rather than creating a new item
    // itself. Covers "Cooking, Smithing, and anything that uses the
    // recipe system" (the ask this pillar was built for) by targeting any
    // Jotunn crafting station generically, not one skill's station
    // specifically. Optional skill+level gate reuses Core's
    // RecipeLevelGate/RecipeLevelGatePatch.
    //
    // "Edit" added 2026-09-10 alongside the same fix on the Item Creator
    // -- see that file's header comment for why.
    public class RecipeCreatorTab : DevToolOverlay.ITab
    {
        public string Title => "Recipes";

        const int MaxRequirementSlots = 4;

        InputField _itemNameField;
        InputField _amountField;
        InputField _minLevelField;
        InputField _gateLevelField;
        readonly InputField[] _requirementItemFields = new InputField[MaxRequirementSlots];
        readonly InputField[] _requirementAmountFields = new InputField[MaxRequirementSlots];
        Dropdown _stationDropdown;
        string[] _stationInternalNames;
        Dropdown _gateSkillDropdown;
        string[] _gateSkillNames;
        Transform _listParent;

        public void Build(Transform contentParent)
        {
            GUIManager gui = GUIManager.Instance;

            TextRow(gui, contentParent, "Create / Edit a Recipe", true);

            _itemNameField = FieldRowWithPicker(gui, contentParent, "Item to Craft", "");
            _amountField = FieldRow(gui, contentParent, "Amount Crafted", "1");

            Dictionary<string, string> stationNames = CraftingStations.GetNames();
            _stationInternalNames = new string[stationNames.Count];
            List<string> stationDisplayNames = new List<string>();
            int si = 0;
            foreach (var kv in stationNames)
            {
                _stationInternalNames[si] = kv.Value;
                stationDisplayNames.Add(kv.Key);
                si++;
            }
            _stationDropdown = DropdownRow(gui, contentParent, "Crafting Station", stationDisplayNames);
            _minLevelField = FieldRow(gui, contentParent, "Min Station Level", "1");

            TextRow(gui, contentParent, "Requirements (leave item name blank to skip a slot)", false);
            for (int i = 0; i < MaxRequirementSlots; i++)
            {
                _requirementItemFields[i] = FieldRowWithPicker(gui, contentParent, $"Material {i + 1} Item Name", "");
                _requirementAmountFields[i] = FieldRow(gui, contentParent, $"Material {i + 1} Amount", "1");
            }

            TextRow(gui, contentParent, "Optional Skill Gate", false);
            List<string> gateOptions = new List<string> { "None" };
            gateOptions.AddRange(SkillRegistry.Names);
            _gateSkillNames = gateOptions.ToArray();
            _gateSkillDropdown = DropdownRow(gui, contentParent, "Required Skill", gateOptions);
            _gateLevelField = FieldRow(gui, contentParent, "Required Level", "0");

            GameObject saveButton = Button(gui, contentParent, "Save & Register");
            saveButton.GetComponent<Button>().onClick.AddListener(() => SaveRecipe(gui));

            GameObject clearButton = Button(gui, contentParent, "Clear Form (New Recipe)");
            clearButton.GetComponent<Button>().onClick.AddListener(() => LoadIntoForm(new DevRecipeDefinition { Amount = 1, MinStationLevel = 1 }));

            TextRow(gui, contentParent, "Existing Recipes", true);
            GameObject listContainer = new GameObject("RecipeList", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            listContainer.transform.SetParent(contentParent, false);
            VerticalLayoutGroup listLayout = listContainer.GetComponent<VerticalLayoutGroup>();
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;
            listLayout.childControlHeight = true;
            listContainer.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _listParent = listContainer.transform;

            RefreshList(gui);
        }

        void SaveRecipe(GUIManager gui)
        {
            DevRecipeDefinition def = new DevRecipeDefinition
            {
                ItemName = _itemNameField.text.Trim(),
                Amount = ParseInt(_amountField.text, 1),
                CraftingStation = _stationInternalNames[_stationDropdown.value],
                MinStationLevel = ParseInt(_minLevelField.text, 1),
                GateSkillName = _gateSkillNames[_gateSkillDropdown.value] == "None" ? "" : _gateSkillNames[_gateSkillDropdown.value],
                GateLevel = ParseInt(_gateLevelField.text, 0),
            };

            for (int i = 0; i < MaxRequirementSlots; i++)
            {
                def.Requirements.Add(new DevRequirement
                {
                    ItemName = _requirementItemFields[i].text.Trim(),
                    Amount = ParseInt(_requirementAmountFields[i].text, 1),
                });
            }

            if (string.IsNullOrEmpty(def.ItemName))
            {
                Jotunn.Logger.LogWarning("DevTool Recipe Creator: Item to Craft is required");
                return;
            }

            DevRecipeRegistry.Definitions.RemoveAll(existing => existing.ItemName == def.ItemName);
            DevRecipeRegistry.Definitions.Add(def);
            DevRecipeRegistry.Save();
            bool registered = DevRecipeRegistry.Register(def);

            Player.m_localPlayer?.Message(MessageHud.MessageType.Center,
                registered
                    ? $"Saved recipe for '{def.ItemName}'. Live this session; a world reload picks it up cleanly everywhere."
                    : $"Saved recipe for '{def.ItemName}', but registration failed this session -- check the log (likely a bad item name).");

            RefreshList(gui);
        }

        void LoadIntoForm(DevRecipeDefinition def)
        {
            _itemNameField.text = def.ItemName;
            _amountField.text = def.Amount.ToString();
            _minLevelField.text = def.MinStationLevel.ToString();

            int stationIndex = System.Array.IndexOf(_stationInternalNames, def.CraftingStation);
            _stationDropdown.value = stationIndex >= 0 ? stationIndex : 0;

            int gateIndex = System.Array.IndexOf(_gateSkillNames, string.IsNullOrEmpty(def.GateSkillName) ? "None" : def.GateSkillName);
            _gateSkillDropdown.value = gateIndex >= 0 ? gateIndex : 0;
            _gateLevelField.text = def.GateLevel.ToString();

            for (int i = 0; i < MaxRequirementSlots; i++)
            {
                bool hasSlot = i < def.Requirements.Count;
                _requirementItemFields[i].text = hasSlot ? def.Requirements[i].ItemName : "";
                _requirementAmountFields[i].text = hasSlot ? def.Requirements[i].Amount.ToString() : "1";
            }
        }

        void RefreshList(GUIManager gui)
        {
            for (int i = _listParent.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(_listParent.GetChild(i).gameObject);
            }

            foreach (DevRecipeDefinition def in DevRecipeRegistry.Definitions)
            {
                GameObject row = new GameObject("RecipeRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
                row.transform.SetParent(_listParent, false);
                row.GetComponent<LayoutElement>().preferredHeight = RowHeight;
                row.GetComponent<HorizontalLayoutGroup>().childControlWidth = false;
                row.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
                row.GetComponent<HorizontalLayoutGroup>().spacing = 10f;

                string gateSuffix = string.IsNullOrEmpty(def.GateSkillName) ? "" : $"  [{def.GateSkillName} {def.GateLevel}]";
                gui.CreateText($"{def.ItemName} x{def.Amount}{gateSuffix}", row.transform,
                    new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero,
                    gui.AveriaSerif, 14, Color.white, false, Color.black, 400f, RowHeight, false);

                GameObject editButton = gui.CreateButton("Edit", row.transform, Vector2.zero, Vector2.zero, Vector2.zero, 90f, 28f);
                editButton.GetComponent<Button>().onClick.AddListener(() => LoadIntoForm(def));

                GameObject duplicateButton = gui.CreateButton("Duplicate", row.transform, Vector2.zero, Vector2.zero, Vector2.zero, 110f, 28f);
                duplicateButton.GetComponent<Button>().onClick.AddListener(() =>
                {
                    LoadIntoForm(def);
                    _itemNameField.text = "";
                });

                GameObject removeButton = gui.CreateButton("Remove", row.transform, Vector2.zero, Vector2.zero, Vector2.zero, 100f, 28f);
                removeButton.GetComponent<Button>().onClick.AddListener(() =>
                {
                    DevRecipeRegistry.Definitions.Remove(def);
                    DevRecipeRegistry.Save();
                    RecipeLevelGate.Unregister(def.ItemName);
                    RefreshList(gui);
                });
            }
        }
    }
}
