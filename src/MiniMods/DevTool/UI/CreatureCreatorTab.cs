using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;
using ThunderFury.DevTool.Data;
using static ThunderFury.DevTool.UI.DevToolUiHelpers;

namespace ThunderFury.DevTool.UI
{
    // ---- Creature Creator tab ----
    //
    // Deliberately narrower than Item/Piece Creator -- clone + rename +
    // HP scaling only, using the exact same CustomCreature(name, baseName,
    // CreatureConfig) clone-by-name pattern Jotunn already provides for
    // items/pieces/skills. Damage scaling is left out on purpose: it
    // varies too much across creature AI types (see
    // DevCreatureDefinition.cs) to generalize safely in one field the way
    // items' SharedData.m_damages does. Loot for a creature made here is
    // already fully covered by the Item Creator's own Drop Sources
    // section, not duplicated.
    public class CreatureCreatorTab : DevToolOverlay.ITab
    {
        public string Title => "Creatures";

        InputField _nameField;
        InputField _baseField;
        InputField _displayField;
        InputField _healthField;
        Transform _listParent;

        InputField _legendaryCreatureField;
        Transform _legendaryListParent;

        public void Build(Transform contentParent)
        {
            GUIManager gui = GUIManager.Instance;

            TextRow(gui, contentParent, "Create / Edit a Creature", true);
            TextRow(gui, contentParent,
                "Clone + rename + HP scaling only -- damage varies too much across creature AI types to generalize safely. " +
                "Use the Items tab's Drop Sources section to give this creature loot once it's registered.",
                false, 55f);

            _nameField = FieldRow(gui, contentParent, "Prefab Name (unique, no spaces)", "");
            _baseField = FieldRowWithCreaturePicker(gui, contentParent, "Base Vanilla Creature", "");
            _displayField = FieldRow(gui, contentParent, "Display Name (optional)", "");
            _healthField = FieldRow(gui, contentParent, "Health Multiplier (1 = same as base)", "1");

            GameObject saveButton = Button(gui, contentParent, "Save & Register");
            saveButton.GetComponent<Button>().onClick.AddListener(() => SaveCreature(gui));

            GameObject clearButton = Button(gui, contentParent, "Clear Form (New Creature)");
            clearButton.GetComponent<Button>().onClick.AddListener(() => LoadIntoForm(new DevCreatureDefinition { HealthMultiplier = 1f }));

            TextRow(gui, contentParent, "Existing Creatures", true);
            GameObject listContainer = new GameObject("CreatureList", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            listContainer.transform.SetParent(contentParent, false);
            VerticalLayoutGroup listLayout = listContainer.GetComponent<VerticalLayoutGroup>();
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;
            listLayout.childControlHeight = true;
            listContainer.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _listParent = listContainer.transform;

            RefreshList(gui);

            // ---- Legendary Drop Sources ----
            // User's own request (2026-09-10): "add a field to what can
            // drop the legendaries." Creature-level rather than per-item
            // since RarityLoot's ambient biome drop system rolls a random
            // base item at kill time, not a fixed one -- eligibility has
            // to live on the creature, not any single item. Works for
            // any creature by prefab name, vanilla or DevTool-made alike.
            TextRow(gui, contentParent, "Legendary Drop Sources (which creatures can roll a Legendary from the ambient biome drop system)", true);
            TextRow(gui, contentParent,
                "Any creature not listed here can still drop Magic/Rare from that same system, just never Legendary.",
                false, 40f);

            _legendaryCreatureField = FieldRowWithCreaturePicker(gui, contentParent, "Creature", "");

            GameObject addLegendaryButton = Button(gui, contentParent, "Add to Legendary Drop Sources");
            addLegendaryButton.GetComponent<Button>().onClick.AddListener(() => SaveLegendarySource(gui));

            TextRow(gui, contentParent, "Current Legendary Drop Sources", true);
            GameObject legendaryListContainer = new GameObject("LegendaryList", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            legendaryListContainer.transform.SetParent(contentParent, false);
            VerticalLayoutGroup legendaryListLayout = legendaryListContainer.GetComponent<VerticalLayoutGroup>();
            legendaryListLayout.childForceExpandWidth = true;
            legendaryListLayout.childForceExpandHeight = false;
            legendaryListLayout.childControlHeight = true;
            legendaryListContainer.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _legendaryListParent = legendaryListContainer.transform;

            RefreshLegendaryList(gui);
        }

        void SaveLegendarySource(GUIManager gui)
        {
            string creatureName = _legendaryCreatureField.text.Trim();
            if (string.IsNullOrEmpty(creatureName))
            {
                Jotunn.Logger.LogWarning("DevTool Creature Creator: a Creature is required to add a Legendary drop source");
                return;
            }

            if (DevLegendaryDropSourceRegistry.Entries.Exists(e => e.CreatureName == creatureName))
            {
                Jotunn.Logger.LogWarning($"DevTool Creature Creator: '{creatureName}' is already a Legendary drop source");
                return;
            }

            DevLegendaryDropSourceRegistry.Entries.Add(new LegendaryDropSourceEntry { CreatureName = creatureName });
            DevLegendaryDropSourceRegistry.Save();
            DevLegendaryDropSourceRegistry.LoadAndApplyAll();

            Player.m_localPlayer?.Message(MessageHud.MessageType.Center,
                $"'{creatureName}' can now roll a Legendary from the ambient biome drop system.");

            _legendaryCreatureField.text = "";
            RefreshLegendaryList(gui);
        }

        void RefreshLegendaryList(GUIManager gui)
        {
            for (int i = _legendaryListParent.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(_legendaryListParent.GetChild(i).gameObject);
            }

            foreach (LegendaryDropSourceEntry entry in DevLegendaryDropSourceRegistry.Entries)
            {
                GameObject row = new GameObject("LegendaryRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
                row.transform.SetParent(_legendaryListParent, false);
                row.GetComponent<LayoutElement>().preferredHeight = RowHeight;
                row.GetComponent<HorizontalLayoutGroup>().childControlWidth = false;
                row.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
                row.GetComponent<HorizontalLayoutGroup>().spacing = 10f;

                gui.CreateText(entry.CreatureName, row.transform,
                    new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero,
                    gui.AveriaSerif, 14, Color.white, false, Color.black, 400f, RowHeight, false);

                GameObject removeButton = gui.CreateButton("Remove", row.transform, Vector2.zero, Vector2.zero, Vector2.zero, 100f, 28f);
                removeButton.GetComponent<Button>().onClick.AddListener(() =>
                {
                    DevLegendaryDropSourceRegistry.Entries.Remove(entry);
                    DevLegendaryDropSourceRegistry.Save();
                    DevLegendaryDropSourceRegistry.LoadAndApplyAll();
                    RefreshLegendaryList(gui);
                });
            }
        }

        void SaveCreature(GUIManager gui)
        {
            DevCreatureDefinition def = new DevCreatureDefinition
            {
                Name = _nameField.text.Trim(),
                BasePrefabName = _baseField.text.Trim(),
                DisplayName = _displayField.text.Trim(),
                HealthMultiplier = ParseFloat(_healthField.text, 1f),
            };

            if (string.IsNullOrEmpty(def.Name) || string.IsNullOrEmpty(def.BasePrefabName))
            {
                Jotunn.Logger.LogWarning("DevTool Creature Creator: Prefab Name and Base Vanilla Creature are required");
                return;
            }

            DevCreatureRegistry.Definitions.RemoveAll(existing => existing.Name == def.Name);
            DevCreatureRegistry.Definitions.Add(def);
            DevCreatureRegistry.Save();
            bool registered = DevCreatureRegistry.Register(def);

            Player.m_localPlayer?.Message(MessageHud.MessageType.Center,
                registered
                    ? $"Saved '{def.Name}'. A world reload picks it up cleanly; use Items > Drop Sources to give it loot."
                    : $"Saved '{def.Name}', but registration failed this session -- check the log (likely a bad base creature name).");

            RefreshList(gui);
        }

        void LoadIntoForm(DevCreatureDefinition def)
        {
            _nameField.text = def.Name;
            _baseField.text = def.BasePrefabName;
            _displayField.text = def.DisplayName;
            _healthField.text = def.HealthMultiplier.ToString();
        }

        void RefreshList(GUIManager gui)
        {
            for (int i = _listParent.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(_listParent.GetChild(i).gameObject);
            }

            foreach (DevCreatureDefinition def in DevCreatureRegistry.Definitions)
            {
                GameObject row = new GameObject("CreatureRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
                row.transform.SetParent(_listParent, false);
                row.GetComponent<LayoutElement>().preferredHeight = RowHeight;
                row.GetComponent<HorizontalLayoutGroup>().childControlWidth = false;
                row.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
                row.GetComponent<HorizontalLayoutGroup>().spacing = 10f;

                gui.CreateText($"{def.Name}  (from {def.BasePrefabName}, HP x{def.HealthMultiplier})", row.transform,
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
                    DevCreatureRegistry.Definitions.Remove(def);
                    DevCreatureRegistry.Save();
                    RefreshList(gui);
                });
            }
        }
    }
}
