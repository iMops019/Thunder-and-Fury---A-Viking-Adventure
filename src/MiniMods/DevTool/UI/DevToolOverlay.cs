using System.Collections.Generic;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace ThunderFury.DevTool.UI
{
    // ---- Dev Tool overlay shell ----
    //
    // Builds a draggable Valheim-style wood panel (GUIManager.CreateWoodpanel)
    // with a tab bar and a scrollable content area. Confirmed against the
    // real decompile: GUIManager tears down and rebuilds its
    // CustomGUIFront/CustomGUIBack containers on every scene change (its
    // own doc comment says so), which also destroys anything parented
    // under them -- rather than rebuild eagerly on every such event, this
    // just relies on Unity's overloaded null-check on a destroyed
    // GameObject (`_panel == null` correctly returns true once GUIManager
    // tears it down) and lazily rebuilds the next time the player actually
    // opens the panel.
    //
    // Tabs register themselves in the static constructor below -- adding
    // a new tab (Items, Recipes, Skills, World) as each is built is a
    // one-line addition here, nothing else in this file changes.
    public static class DevToolOverlay
    {
        public interface ITab
        {
            string Title { get; }
            void Build(Transform contentParent);
        }

        static GameObject _panel;
        static Transform _contentParent;
        static readonly List<GameObject> _tabButtons = new List<GameObject>();
        static readonly List<ITab> _tabs = new List<ITab>();
        static int _activeTab;

        // Widened 2026-09-10 after in-game testing showed description
        // text getting clipped at the original 900x600 -- see
        // DevToolUiHelpers.TextRow's own comment for the actual bug
        // (fixed-height rows truncating wrapped text), which this alone
        // doesn't fully fix, just gives more room to work with.
        // Widened again 2026-09-10 as tabs kept growing (5 -> 7 -> 8 -> 9).
        const float PanelWidth = 1100f;
        const float PanelHeight = 720f;

        static DevToolOverlay()
        {
            _tabs.Add(new ItemCreatorTab());
            _tabs.Add(new PieceCreatorTab());
            _tabs.Add(new CreatureCreatorTab());
            _tabs.Add(new RecipeCreatorTab());
            _tabs.Add(new SkillCreatorTab());
            _tabs.Add(new QuestCreatorTab());
            _tabs.Add(new WorldAreaTab());
            _tabs.Add(new BiomeTab());
            _tabs.Add(new DungeonTab());
            _tabs.Add(new SpawnTab());
            _tabs.Add(new ValuesTab());
        }

        // Doesn't need to do anything beyond clearing our own reference --
        // the panel GameObject itself was already destroyed by GUIManager
        // tearing down CustomGUIFront, and Toggle()'s null-check picks
        // that up on its own. Kept as an explicit hook for clarity and in
        // case a future tab needs to know when a rebuild happened.
        public static void OnCustomGUIAvailable()
        {
            _panel = null;
            _contentParent = null;
        }

        public static void Toggle()
        {
            if (_panel == null)
            {
                if (GUIManager.CustomGUIFront == null) return;
                BuildPanel();
            }

            bool nowVisible = !_panel.activeSelf;
            _panel.SetActive(nowVisible);
            GUIManager.BlockInput(nowVisible);
            if (nowVisible) ShowTab(_activeTab);
        }

        static void BuildPanel()
        {
            GUIManager gui = GUIManager.Instance;
            Transform root = GUIManager.CustomGUIFront.transform;

            _panel = gui.CreateWoodpanel(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, PanelWidth, PanelHeight);
            _panel.SetActive(false);

            gui.CreateText("Thunder & Fury - Dev Tool", _panel.transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f),
                gui.AveriaSerifBold, 22, gui.ValheimOrange, true, Color.black,
                PanelWidth - 80f, 30f, false);

            GameObject closeButton = gui.CreateButton("X", _panel.transform,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-35f, -25f), 40f, 40f);
            closeButton.GetComponent<Button>().onClick.AddListener(() =>
            {
                _panel.SetActive(false);
                GUIManager.BlockInput(false);
            });

            // Wraps into multiple rows once there are more tabs than fit
            // on one -- stopped just widening the panel every time a tab
            // got added (that stopped scaling around 9-10 tabs) in favor
            // of an approach that scales indefinitely.
            const int tabsPerRow = 6;
            const float tabWidth = 105f;
            const float tabStep = 118f;
            const float tabRowHeight = 40f;
            const float firstTabRowY = -75f;

            _tabButtons.Clear();
            for (int i = 0; i < _tabs.Count; i++)
            {
                int index = i;
                int row = i / tabsPerRow;
                int col = i % tabsPerRow;
                float tabX = 15f + col * tabStep;
                float tabY = firstTabRowY - row * tabRowHeight;

                GameObject tabButton = gui.CreateButton(_tabs[i].Title, _panel.transform,
                    new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(tabX, tabY), tabWidth, 35f);
                tabButton.GetComponent<Button>().onClick.AddListener(() => ShowTab(index));
                _tabButtons.Add(tabButton);
            }

            int rowCount = Mathf.CeilToInt(_tabs.Count / (float)tabsPerRow);
            float contentTopY = firstTabRowY - (rowCount - 1) * tabRowHeight - 45f;

            GameObject scrollView = gui.CreateScrollView(_panel.transform, false, true, 10f, 2f,
                gui.ValheimScrollbarHandleColorBlock, new Color(0f, 0f, 0f, 0.5f),
                PanelWidth - 40f, PanelHeight + contentTopY - 20f);
            RectTransform scrollRect = scrollView.GetComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.5f, 1f);
            scrollRect.anchorMax = new Vector2(0.5f, 1f);
            scrollRect.pivot = new Vector2(0.5f, 1f);
            scrollRect.anchoredPosition = new Vector2(0f, contentTopY);

            _contentParent = scrollView.GetComponentInChildren<ScrollRect>().content;
        }

        static void ShowTab(int index)
        {
            _activeTab = index;

            for (int i = _contentParent.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(_contentParent.GetChild(i).gameObject);
            }

            _tabs[index].Build(_contentParent);
        }
    }
}
