using System.Collections.Generic;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;
using ThunderFury.DevTool.Data;
using static ThunderFury.DevTool.UI.DevToolUiHelpers;

namespace ThunderFury.DevTool.UI
{
    // ---- Dungeon Editor tab ----
    //
    // Same ZoneLocationCatalog data as the Biome tab, filtered to
    // locations whose prefab actually has a DungeonGenerator component --
    // confirmed via decompile that's the real distinction vanilla itself
    // uses, no separate "is dungeon" flag exists. Adds the dungeon-
    // exclusive bonus loot pool below the location list -- see
    // Patches/DungeonLootPatch.cs for the actual mechanic and why it's a
    // bonus roll layered on top of vanilla loot rather than an injection
    // into it.
    public class DungeonTab : DevToolOverlay.ITab
    {
        public string Title => "Dungeons";

        Transform _locationListParent;
        Transform _lootListParent;
        InputField _itemField;
        InputField _chanceField;
        InputField _minField;
        InputField _maxField;

        public void Build(Transform contentParent)
        {
            GUIManager gui = GUIManager.Instance;

            if (ZoneSystem.instance == null)
            {
                TextRow(gui, contentParent, "Enter a world to browse dungeons -- ZoneSystem.instance isn't available from the main menu.", true);
                return;
            }

            TextRow(gui, contentParent, "Dungeon Editor", true);
            TextRow(gui, contentParent,
                "Every location whose prefab has a DungeonGenerator -- adjust how many of each spawn in the world. Same 'affects new terrain only' caveat as the Biome tab.",
                false, 55f);

            GameObject locationListContainer = new GameObject("DungeonList", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            locationListContainer.transform.SetParent(contentParent, false);
            SetupListLayout(locationListContainer);
            _locationListParent = locationListContainer.transform;

            foreach (ZoneLocationCatalog.Entry entry in ZoneLocationCatalog.GetAll())
            {
                if (!entry.IsDungeon) continue;
                BuildLocationRow(gui, _locationListParent, entry);
            }

            TextRow(gui, contentParent, "Dungeon-Exclusive Bonus Loot Pool", true);
            TextRow(gui, contentParent,
                "Each entry rolls independently the first time a container is opened inside an active dungeon -- a CHANCE, not a guarantee, so running more dungeons is genuinely worth it.",
                false, 55f);

            _itemField = FieldRowWithPicker(gui, contentParent, "Item", "");
            _chanceField = FieldRow(gui, contentParent, "Drop Chance % (per roll)", "5");
            _minField = FieldRow(gui, contentParent, "Min Amount", "1");
            _maxField = FieldRow(gui, contentParent, "Max Amount", "1");

            GameObject addButton = Button(gui, contentParent, "Save to Loot Pool");
            addButton.GetComponent<Button>().onClick.AddListener(() =>
            {
                string itemName = _itemField.text.Trim();
                if (string.IsNullOrEmpty(itemName))
                {
                    Jotunn.Logger.LogWarning("DevTool Dungeon Editor: an item is required to add to the loot pool");
                    return;
                }

                DungeonLootRegistry.Entries.RemoveAll(e => e.ItemName == itemName);
                DungeonLootRegistry.Entries.Add(new DungeonLootEntry
                {
                    ItemName = itemName,
                    ChancePercent = ParseFloat(_chanceField.text, 5f),
                    MinAmount = ParseInt(_minField.text, 1),
                    MaxAmount = ParseInt(_maxField.text, 1),
                });
                DungeonLootRegistry.Save();

                Player.m_localPlayer?.Message(MessageHud.MessageType.Center, $"Saved '{itemName}' in the dungeon loot pool.");
                RefreshLootList(gui);
            });

            TextRow(gui, contentParent, "Current Loot Pool", true);
            GameObject lootListContainer = new GameObject("LootPoolList", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            lootListContainer.transform.SetParent(contentParent, false);
            SetupListLayout(lootListContainer);
            _lootListParent = lootListContainer.transform;

            RefreshLootList(gui);
        }

        static void SetupListLayout(GameObject container)
        {
            VerticalLayoutGroup layout = container.GetComponent<VerticalLayoutGroup>();
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childControlHeight = true;
            container.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        static void BuildLocationRow(GUIManager gui, Transform parent, ZoneLocationCatalog.Entry entry)
        {
            GameObject row = new GameObject("DungeonRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            row.GetComponent<LayoutElement>().preferredHeight = RowHeight;
            HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = false;
            layout.childForceExpandWidth = false;
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleLeft;

            GameObject nameText = gui.CreateText($"{entry.PrefabName}  ({entry.Biome})", row.transform,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero,
                gui.AveriaSerif, 14, Color.white, false, Color.black, 500f, RowHeight, false);
            nameText.GetComponent<RectTransform>().pivot = new Vector2(0f, 0.5f);

            GameObject field = gui.CreateInputField(row.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                Vector2.zero, InputField.ContentType.IntegerNumber, null, 14, 100f, RowHeight - 4f);
            InputField inputField = field.GetComponent<InputField>();
            inputField.text = entry.Location.m_quantity.ToString();
            inputField.onEndEdit.AddListener(text =>
            {
                if (int.TryParse(text, out int qty))
                {
                    entry.Location.m_quantity = qty;
                    DevZoneSettingsRegistry.SetQuantity(entry.PrefabName, qty);
                    DevZoneSettingsRegistry.Save();
                }
            });
        }

        void RefreshLootList(GUIManager gui)
        {
            for (int i = _lootListParent.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(_lootListParent.GetChild(i).gameObject);
            }

            foreach (DungeonLootEntry entry in DungeonLootRegistry.Entries)
            {
                GameObject row = new GameObject("LootRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
                row.transform.SetParent(_lootListParent, false);
                row.GetComponent<LayoutElement>().preferredHeight = RowHeight;
                HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
                layout.childControlWidth = false;
                layout.childForceExpandWidth = false;
                layout.spacing = 10f;

                GameObject text = gui.CreateText($"{entry.ItemName}  {entry.ChancePercent}%  x{entry.MinAmount}-{entry.MaxAmount}", row.transform,
                    new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero,
                    gui.AveriaSerif, 14, Color.white, false, Color.black, 400f, RowHeight, false);
                text.GetComponent<RectTransform>().pivot = new Vector2(0f, 0.5f);

                GameObject editButton = gui.CreateButton("Edit", row.transform, Vector2.zero, Vector2.zero, Vector2.zero, 90f, 28f);
                editButton.GetComponent<Button>().onClick.AddListener(() =>
                {
                    _itemField.text = entry.ItemName;
                    _chanceField.text = entry.ChancePercent.ToString();
                    _minField.text = entry.MinAmount.ToString();
                    _maxField.text = entry.MaxAmount.ToString();
                });

                GameObject removeButton = gui.CreateButton("Remove", row.transform, Vector2.zero, Vector2.zero, Vector2.zero, 100f, 28f);
                removeButton.GetComponent<Button>().onClick.AddListener(() =>
                {
                    DungeonLootRegistry.Entries.Remove(entry);
                    DungeonLootRegistry.Save();
                    RefreshLootList(gui);
                });
            }
        }
    }
}
