using System;
using System.Collections.Generic;
using System.Linq;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;
using ThunderFury.DevTool.Data;

namespace ThunderFury.DevTool.UI
{
    // ---- Shared searchable status effect picker ----
    //
    // Fourth of these pickers (Item, Creature, Piece, Status Effect) --
    // same shape each time. See VanillaStatusEffectCatalog.cs for why
    // this reuses existing vanilla effects rather than authoring new ones.
    public static class StatusEffectPickerPopup
    {
        const int MaxResultsShown = 150;

        static GameObject _panel;
        static Transform _listParent;
        static InputField _searchField;
        static List<VanillaStatusEffectCatalog.Entry> _allEffects = new List<VanillaStatusEffectCatalog.Entry>();
        static Action<string> _onSelected;

        public static void Open(Action<string> onSelected)
        {
            if (GUIManager.CustomGUIFront == null) return;

            _onSelected = onSelected;
            _allEffects = VanillaStatusEffectCatalog.GetAll();

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
                Vector2.zero, 480f, 600f);
            _panel.SetActive(false);

            gui.CreateText("Pick a Status Effect", _panel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -25f), gui.AveriaSerifBold, 20, gui.ValheimOrange, true, Color.black,
                420f, 30f, false);

            GameObject closeButton = gui.CreateButton("X", _panel.transform,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -22f), 36f, 36f);
            closeButton.GetComponent<Button>().onClick.AddListener(() => _panel.SetActive(false));

            GameObject searchGO = gui.CreateInputField(_panel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -65f), InputField.ContentType.Standard, "Search by name...", 16, 340f, 32f);
            _searchField = searchGO.GetComponent<InputField>();
            _searchField.onValueChanged.AddListener(_ => RefreshList());

            GameObject scrollView = gui.CreateScrollView(_panel.transform, false, true, 10f, 2f,
                gui.ValheimScrollbarHandleColorBlock, new Color(0f, 0f, 0f, 0.5f), 440f, 460f);
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
            IEnumerable<VanillaStatusEffectCatalog.Entry> query = _allEffects;
            if (!string.IsNullOrEmpty(filter))
            {
                query = query.Where(e => e.InternalName.ToLowerInvariant().Contains(filter));
            }

            GUIManager gui = GUIManager.Instance;
            int shown = 0;
            foreach (VanillaStatusEffectCatalog.Entry entry in query)
            {
                if (shown++ >= MaxResultsShown) break;

                GameObject row = gui.CreateButton(entry.InternalName, _listParent, Vector2.zero, Vector2.zero, Vector2.zero, 420f, 32f);
                row.AddComponent<LayoutElement>().preferredHeight = 34f;
                string effectName = entry.InternalName;
                row.GetComponent<Button>().onClick.AddListener(() =>
                {
                    _onSelected?.Invoke(effectName);
                    _panel.SetActive(false);
                });
            }

            if (shown == 0)
            {
                GameObject emptyRow = new GameObject("Empty", typeof(RectTransform), typeof(LayoutElement));
                emptyRow.transform.SetParent(_listParent, false);
                emptyRow.GetComponent<LayoutElement>().preferredHeight = 30f;

                GameObject emptyText = gui.CreateText("No matching status effects.", emptyRow.transform,
                    new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero,
                    gui.AveriaSerif, 14, Color.gray, false, Color.black, 420f, 30f, false);
                emptyText.GetComponent<RectTransform>().pivot = new Vector2(0f, 0.5f);
            }
        }
    }
}
