using System;
using System.Collections.Generic;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;
using ThunderFury.DevTool.Data;
using static ThunderFury.DevTool.UI.DevToolUiHelpers;

namespace ThunderFury.DevTool.UI
{
    // ---- Piece Creator tab ----
    //
    // Closes a real gap: before this, adding a new buildable object
    // (decoration, station, structure) meant hand-writing a new .cs file
    // the way AdventureBoard.cs was built for Quests -- there was no
    // general "create a new piece" tool the way Item Creator exists for
    // items. Same clone-a-vanilla-base-and-set-fields shape, using
    // Jotunn's own CustomPiece(name, baseName, PieceConfig) -- the exact
    // mechanism AdventureBoard.cs already proved out by hand.
    //
    // Category and PieceTable are both dropdowns sourced live from
    // Jotunn's own PieceCategories.GetNames()/PieceTables.GetNames()
    // (confirmed to exist, mirroring CraftingStations.GetNames() already
    // used elsewhere) rather than hand-typed lists.
    public class PieceCreatorTab : DevToolOverlay.ITab
    {
        public string Title => "Pieces";

        const int MaxRequirementSlots = 4;

        InputField _nameField;
        InputField _baseField;
        InputField _displayField;
        InputField _descField;
        Dropdown _categoryDropdown;
        string[] _categoryInternalNames;
        Dropdown _pieceTableDropdown;
        string[] _pieceTableInternalNames;
        Dropdown _stationDropdown;
        string[] _stationInternalNames;
        readonly InputField[] _requirementItemFields = new InputField[MaxRequirementSlots];
        readonly InputField[] _requirementAmountFields = new InputField[MaxRequirementSlots];
        Transform _listParent;

        public void Build(Transform contentParent)
        {
            GUIManager gui = GUIManager.Instance;

            TextRow(gui, contentParent, "Create / Edit a Piece", true);

            _nameField = FieldRow(gui, contentParent, "Prefab Name (unique, no spaces)", "");
            _baseField = FieldRowWithPiecePicker(gui, contentParent, "Base Vanilla Piece", "");
            _displayField = FieldRow(gui, contentParent, "Display Name (optional)", "");
            _descField = FieldRow(gui, contentParent, "Description", "");

            (_categoryInternalNames, List<string> categoryDisplayNames) = NamesFrom(PieceCategories.GetNames());
            _categoryDropdown = DropdownRow(gui, contentParent, "Category", categoryDisplayNames);

            (_pieceTableInternalNames, List<string> pieceTableDisplayNames) = NamesFrom(PieceTables.GetNames());
            _pieceTableDropdown = DropdownRow(gui, contentParent, "Piece Table (which tool builds it)", pieceTableDisplayNames);

            List<string> stationDisplayNamesWithNone = new List<string> { "None" };
            (_stationInternalNames, List<string> stationDisplayNames) = NamesFrom(CraftingStations.GetNames());
            stationDisplayNamesWithNone.AddRange(stationDisplayNames);
            string[] stationInternalWithNone = new string[_stationInternalNames.Length + 1];
            stationInternalWithNone[0] = "";
            Array.Copy(_stationInternalNames, 0, stationInternalWithNone, 1, _stationInternalNames.Length);
            _stationInternalNames = stationInternalWithNone;
            _stationDropdown = DropdownRow(gui, contentParent, "Requires Nearby Station (optional)", stationDisplayNamesWithNone);

            TextRow(gui, contentParent, "Requirements (leave item name blank to skip a slot)", false);
            for (int i = 0; i < MaxRequirementSlots; i++)
            {
                _requirementItemFields[i] = FieldRowWithPicker(gui, contentParent, $"Material {i + 1} Item Name", "");
                _requirementAmountFields[i] = FieldRow(gui, contentParent, $"Material {i + 1} Amount", "1");
            }

            GameObject saveButton = Button(gui, contentParent, "Save & Register");
            saveButton.GetComponent<Button>().onClick.AddListener(() => SavePiece(gui));

            GameObject clearButton = Button(gui, contentParent, "Clear Form (New Piece)");
            clearButton.GetComponent<Button>().onClick.AddListener(() => LoadIntoForm(new DevPieceDefinition()));

            TextRow(gui, contentParent, "Existing Pieces", true);
            GameObject listContainer = new GameObject("PieceList", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            listContainer.transform.SetParent(contentParent, false);
            VerticalLayoutGroup listLayout = listContainer.GetComponent<VerticalLayoutGroup>();
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;
            listLayout.childControlHeight = true;
            listContainer.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _listParent = listContainer.transform;

            RefreshList(gui);
        }

        static (string[] internalNames, List<string> displayNames) NamesFrom(Dictionary<string, string> namesMap)
        {
            string[] internalNames = new string[namesMap.Count];
            List<string> displayNames = new List<string>();
            int i = 0;
            foreach (var kv in namesMap)
            {
                internalNames[i] = kv.Value;
                displayNames.Add(kv.Key);
                i++;
            }
            return (internalNames, displayNames);
        }

        void SavePiece(GUIManager gui)
        {
            DevPieceDefinition def = new DevPieceDefinition
            {
                Name = _nameField.text.Trim(),
                BasePrefabName = _baseField.text.Trim(),
                DisplayName = _displayField.text.Trim(),
                Description = _descField.text.Trim(),
                Category = _categoryInternalNames[_categoryDropdown.value],
                PieceTable = _pieceTableInternalNames[_pieceTableDropdown.value],
                CraftingStation = _stationInternalNames[_stationDropdown.value],
            };

            for (int i = 0; i < MaxRequirementSlots; i++)
            {
                def.Requirements.Add(new DevRequirement
                {
                    ItemName = _requirementItemFields[i].text.Trim(),
                    Amount = ParseInt(_requirementAmountFields[i].text, 1),
                });
            }

            if (string.IsNullOrEmpty(def.Name) || string.IsNullOrEmpty(def.BasePrefabName))
            {
                Jotunn.Logger.LogWarning("DevTool Piece Creator: Prefab Name and Base Vanilla Piece are required");
                return;
            }

            DevPieceRegistry.Definitions.RemoveAll(existing => existing.Name == def.Name);
            DevPieceRegistry.Definitions.Add(def);
            DevPieceRegistry.Save();
            bool registered = DevPieceRegistry.Register(def);

            Player.m_localPlayer?.Message(MessageHud.MessageType.Center,
                registered
                    ? $"Saved '{def.Name}'. Live this session; a world reload picks it up cleanly everywhere."
                    : $"Saved '{def.Name}', but registration failed this session -- check the log (likely a bad base piece name).");

            RefreshList(gui);
        }

        void LoadIntoForm(DevPieceDefinition def)
        {
            _nameField.text = def.Name;
            _baseField.text = def.BasePrefabName;
            _displayField.text = def.DisplayName;
            _descField.text = def.Description;

            int categoryIndex = Array.IndexOf(_categoryInternalNames, def.Category);
            _categoryDropdown.value = categoryIndex >= 0 ? categoryIndex : 0;

            int pieceTableIndex = Array.IndexOf(_pieceTableInternalNames, def.PieceTable);
            _pieceTableDropdown.value = pieceTableIndex >= 0 ? pieceTableIndex : 0;

            int stationIndex = Array.IndexOf(_stationInternalNames, def.CraftingStation ?? "");
            _stationDropdown.value = stationIndex >= 0 ? stationIndex : 0;

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
                UnityEngine.Object.Destroy(_listParent.GetChild(i).gameObject);
            }

            foreach (DevPieceDefinition def in DevPieceRegistry.Definitions)
            {
                GameObject row = new GameObject("PieceRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
                row.transform.SetParent(_listParent, false);
                row.GetComponent<LayoutElement>().preferredHeight = RowHeight;
                row.GetComponent<HorizontalLayoutGroup>().childControlWidth = false;
                row.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
                row.GetComponent<HorizontalLayoutGroup>().spacing = 10f;

                gui.CreateText($"{def.Name}  (from {def.BasePrefabName})", row.transform,
                    new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero,
                    gui.AveriaSerif, 14, Color.white, false, Color.black, 400f, RowHeight, false);

                GameObject editButton = gui.CreateButton("Edit", row.transform, Vector2.zero, Vector2.zero, Vector2.zero, 90f, 28f);
                editButton.GetComponent<Button>().onClick.AddListener(() => LoadIntoForm(def));

                GameObject duplicateButton = gui.CreateButton("Duplicate", row.transform, Vector2.zero, Vector2.zero, Vector2.zero, 110f, 28f);
                duplicateButton.GetComponent<Button>().onClick.AddListener(() =>
                {
                    LoadIntoForm(def);
                    _nameField.text = "";
                });

                GameObject removeButton = gui.CreateButton("Remove", row.transform, Vector2.zero, Vector2.zero, Vector2.zero, 100f, 28f);
                removeButton.GetComponent<Button>().onClick.AddListener(() =>
                {
                    DevPieceRegistry.Definitions.Remove(def);
                    DevPieceRegistry.Save();
                    RefreshList(gui);
                });
            }
        }
    }
}
