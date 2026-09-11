using System;
using System.Collections.Generic;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;
using ThunderFury.DevTool.Data;
using static ThunderFury.DevTool.UI.DevToolUiHelpers;

namespace ThunderFury.DevTool.UI
{
    // ---- Biome Editor tab ----
    //
    // Pick a biome, see every non-dungeon location that can spawn there
    // (ZoneLocationCatalog, a live read of ZoneSystem's own data), adjust
    // how much of it there is. Dungeons are deliberately excluded here --
    // they get their own tab since they need the extra loot-pool section.
    //
    // Honest limitation, not hidden: a quantity change only affects
    // zones the world generates/explores from here on -- it can't
    // retroactively add more instances of something to terrain that's
    // already been explored (confirmed via decompile: ZoneSystem places
    // locations into a zone the first time that zone is generated, not
    // on a recurring schedule).
    public class BiomeTab : DevToolOverlay.ITab
    {
        public string Title => "Biomes";

        static readonly string[] BiomeNames =
        {
            "Meadows", "BlackForest", "Swamp", "Mountain",
            "Plains", "Mistlands", "AshLands", "DeepNorth", "Ocean",
        };

        Dropdown _biomeDropdown;
        Transform _listParent;
        List<ZoneLocationCatalog.Entry> _allLocations = new List<ZoneLocationCatalog.Entry>();

        public void Build(Transform contentParent)
        {
            GUIManager gui = GUIManager.Instance;

            if (ZoneSystem.instance == null)
            {
                TextRow(gui, contentParent, "Enter a world to browse biomes -- ZoneSystem.instance isn't available from the main menu.", true);
                return;
            }

            TextRow(gui, contentParent, "Biome Editor", true);
            TextRow(gui, contentParent,
                "Pick a biome to see everything that can spawn there and adjust how much of it there is. Applies to newly-generated/explored terrain going forward, not areas already explored.",
                false, 55f);

            _allLocations = ZoneLocationCatalog.GetAll();
            _biomeDropdown = DropdownRow(gui, contentParent, "Biome", new List<string>(BiomeNames));
            _biomeDropdown.onValueChanged.AddListener(_ => RefreshList(gui));

            GameObject listContainer = new GameObject("BiomeLocationList", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            listContainer.transform.SetParent(contentParent, false);
            VerticalLayoutGroup listLayout = listContainer.GetComponent<VerticalLayoutGroup>();
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;
            listLayout.childControlHeight = true;
            listContainer.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _listParent = listContainer.transform;

            RefreshList(gui);
        }

        void RefreshList(GUIManager gui)
        {
            for (int i = _listParent.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(_listParent.GetChild(i).gameObject);
            }

            Heightmap.Biome selected = (Heightmap.Biome)Enum.Parse(typeof(Heightmap.Biome), BiomeNames[_biomeDropdown.value]);

            int shown = 0;
            foreach (ZoneLocationCatalog.Entry entry in _allLocations)
            {
                if (entry.IsDungeon) continue;
                if ((entry.Biome & selected) == 0) continue;

                BuildLocationRow(gui, _listParent, entry);
                shown++;
            }

            if (shown == 0)
            {
                TextRow(gui, _listParent, "Nothing registered for this biome.", false);
            }
        }

        static void BuildLocationRow(GUIManager gui, Transform parent, ZoneLocationCatalog.Entry entry)
        {
            GameObject row = new GameObject("LocRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = false;
            layout.childForceExpandWidth = false;
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            row.GetComponent<LayoutElement>().preferredHeight = RowHeight;

            GameObject nameText = gui.CreateText(entry.PrefabName, row.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                Vector2.zero, gui.AveriaSerif, 14, Color.white, false, Color.black, 500f, RowHeight, false);
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
    }
}
