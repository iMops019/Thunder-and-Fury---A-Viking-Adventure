using System;
using System.Collections.Generic;
using System.Linq;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;
using ThunderFury.DevTool.Data;

namespace ThunderFury.DevTool.UI
{
    // ---- Shared searchable item picker ----
    //
    // Opened from any field that wants "pick an existing item" instead of
    // typing a raw prefab name by hand (user's own ask, 2026-09-10, after
    // hitting exactly the failure mode a hand-typed name risks: a stale
    // guess breaking silently). Backed by VanillaItemCatalog's live
    // ObjectDB read, so the list is always exactly what this game session
    // actually has, not a maintained/guessed list.
    //
    // Renders as a second wood panel stacked on top of DevToolOverlay's
    // own panel (same GUIManager.CustomGUIFront root, brought to front),
    // rather than replacing the tab's content area -- keeps the
    // triggering form visible underneath and the picker itself reusable
    // from any tab without that tab needing to know how it's built.
    public static class ItemPickerPopup
    {
        public enum Category
        {
            All,
            Weapons,
            Armor,
            Tools,
            Consumables,
            Materials,
        }

        const int MaxResultsShown = 150;

        static GameObject _panel;
        static Transform _listParent;
        static InputField _searchField;
        static Dropdown _categoryDropdown;
        static List<VanillaItemCatalog.Entry> _allItems = new List<VanillaItemCatalog.Entry>();
        static Action<string> _onSelected;

        public static void Open(Action<string> onSelected)
        {
            if (GUIManager.CustomGUIFront == null) return;

            _onSelected = onSelected;
            _allItems = VanillaItemCatalog.GetAll();

            GUIManager gui = GUIManager.Instance;
            if (_panel == null)
            {
                Build(gui);
            }

            _panel.transform.SetAsLastSibling();
            _panel.SetActive(true);
            _searchField.text = "";
            RefreshList();
        }

        static void Build(GUIManager gui)
        {
            Transform root = GUIManager.CustomGUIFront.transform;

            _panel = gui.CreateWoodpanel(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, 560f, 620f);
            _panel.SetActive(false);

            gui.CreateText("Pick an Item", _panel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -25f), gui.AveriaSerifBold, 20, gui.ValheimOrange, true, Color.black,
                500f, 30f, false);

            GameObject closeButton = gui.CreateButton("X", _panel.transform,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -22f), 36f, 36f);
            closeButton.GetComponent<Button>().onClick.AddListener(() => _panel.SetActive(false));

            GameObject searchGO = gui.CreateInputField(_panel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-70f, -65f), InputField.ContentType.Standard, "Search by name...", 16, 340f, 32f);
            _searchField = searchGO.GetComponent<InputField>();
            _searchField.onValueChanged.AddListener(_ => RefreshList());

            GameObject dropdownGO = gui.CreateDropDown(_panel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(150f, -65f), 14, 150f, 32f);
            _categoryDropdown = dropdownGO.GetComponent<Dropdown>();
            _categoryDropdown.AddOptions(new List<string> { "All", "Weapons", "Armor", "Tools", "Consumables", "Materials" });
            _categoryDropdown.onValueChanged.AddListener(_ => RefreshList());

            GameObject scrollView = gui.CreateScrollView(_panel.transform, false, true, 10f, 2f,
                gui.ValheimScrollbarHandleColorBlock, new Color(0f, 0f, 0f, 0.5f), 520f, 480f);
            RectTransform scrollRect = scrollView.GetComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.5f, 1f);
            scrollRect.anchorMax = new Vector2(0.5f, 1f);
            scrollRect.pivot = new Vector2(0.5f, 1f);
            scrollRect.anchoredPosition = new Vector2(0f, -105f);

            _listParent = scrollView.GetComponentInChildren<ScrollRect>().content;
        }

        static void RefreshList()
        {
            for (int i = _listParent.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(_listParent.GetChild(i).gameObject);
            }

            string filter = (_searchField.text ?? "").Trim().ToLowerInvariant();
            Category category = (Category)_categoryDropdown.value;

            IEnumerable<VanillaItemCatalog.Entry> query = _allItems;
            if (!string.IsNullOrEmpty(filter))
            {
                query = query.Where(e =>
                    e.DisplayName.ToLowerInvariant().Contains(filter) ||
                    e.PrefabName.ToLowerInvariant().Contains(filter));
            }

            query = category switch
            {
                Category.Weapons => query.Where(e => e.IsWeapon),
                Category.Armor => query.Where(e => e.IsArmor),
                Category.Tools => query.Where(e => e.ItemType == ItemDrop.ItemData.ItemType.Tool),
                Category.Consumables => query.Where(e => e.ItemType == ItemDrop.ItemData.ItemType.Consumable),
                Category.Materials => query.Where(e => e.ItemType == ItemDrop.ItemData.ItemType.Material),
                _ => query,
            };

            GUIManager gui = GUIManager.Instance;
            int shown = 0;
            foreach (VanillaItemCatalog.Entry entry in query)
            {
                if (shown++ >= MaxResultsShown) break;

                GameObject row = gui.CreateButton($"{entry.DisplayName}  ({entry.PrefabName})", _listParent,
                    Vector2.zero, Vector2.zero, Vector2.zero, 500f, 32f);
                row.AddComponent<LayoutElement>().preferredHeight = 34f;
                string prefabName = entry.PrefabName;
                row.GetComponent<Button>().onClick.AddListener(() =>
                {
                    _onSelected?.Invoke(prefabName);
                    _panel.SetActive(false);
                });
            }

            if (shown == 0)
            {
                GameObject emptyRow = new GameObject("Empty", typeof(RectTransform), typeof(LayoutElement));
                emptyRow.transform.SetParent(_listParent, false);
                emptyRow.GetComponent<LayoutElement>().preferredHeight = 30f;

                GameObject emptyText = gui.CreateText("No matching items.", emptyRow.transform,
                    new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero,
                    gui.AveriaSerif, 14, Color.gray, false, Color.black, 500f, 30f, false);
                emptyText.GetComponent<RectTransform>().pivot = new Vector2(0f, 0.5f);
            }
        }
    }
}
