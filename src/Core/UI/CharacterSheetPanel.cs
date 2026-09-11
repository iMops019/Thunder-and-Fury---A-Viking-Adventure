using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;
using ThunderFury.Core.SkillSystem;

namespace ThunderFury.Core.UI
{
    // ---- Pillar 2, first real deliverable: Character Sheet panel ----
    //
    // Scope call (locked in 2026-09-10, user's own call, made fast on
    // purpose): ADDITIVE panels on top of vanilla's UI, not a full
    // overhaul -- vision.md itself flags a full overhaul as the most
    // technically involved, highest-risk pillar in the whole mod, and
    // the additive approach reuses the exact GUIManager toolkit already
    // proven extensively building the Dev Tool overlay.
    //
    // Deliberately reads skills by NAME via Core's own SkillRegistry
    // rather than iterating Skills.GetSkillList() and trying to resolve
    // a display name from SkillType -- confirmed via decompile that
    // SkillDef has no display-name field at all for custom skills
    // (names are a Jotunn-internal localization detail this doesn't need
    // to reach into), and Core already has the canonical (name, type)
    // pairs for all 12 skills from building the Dev Tool's Recipe
    // Creator gate picker. Simpler and more robust than reverse-
    // engineering Jotunn's naming.
    //
    // Expanded inventory (also part of Pillar 2's original ask) is
    // deliberately NOT part of this pass -- it needs real surgery on
    // InventoryGui's own grid dimensions, a bigger, riskier undertaking
    // than a read-only display panel. Flagged as a separate follow-up,
    // not silently dropped.
    public static class CharacterSheetPanel
    {
        static GameObject _panel;
        static Transform _listParent;

        public static void OnCustomGUIAvailable()
        {
            _panel = null;
        }

        public static void Toggle()
        {
            if (Player.m_localPlayer == null) return;

            if (_panel == null)
            {
                if (GUIManager.CustomGUIFront == null) return;
                Build();
            }

            bool nowVisible = !_panel.activeSelf;
            _panel.SetActive(nowVisible);
            GUIManager.BlockInput(nowVisible);
            if (nowVisible) Refresh();
        }

        static void Build()
        {
            GUIManager gui = GUIManager.Instance;
            Transform root = GUIManager.CustomGUIFront.transform;

            _panel = gui.CreateWoodpanel(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, 480f, 620f);
            _panel.SetActive(false);

            gui.CreateText("Character Sheet", _panel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -30f), gui.AveriaSerifBold, 22, gui.ValheimOrange, true, Color.black,
                420f, 30f, false);

            GameObject closeButton = gui.CreateButton("X", _panel.transform,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -22f), 36f, 36f);
            closeButton.GetComponent<Button>().onClick.AddListener(() =>
            {
                _panel.SetActive(false);
                GUIManager.BlockInput(false);
            });

            GameObject scrollView = gui.CreateScrollView(_panel.transform, false, true, 10f, 2f,
                gui.ValheimScrollbarHandleColorBlock, new Color(0f, 0f, 0f, 0.5f), 440f, 540f);
            RectTransform scrollRect = scrollView.GetComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.5f, 1f);
            scrollRect.anchorMax = new Vector2(0.5f, 1f);
            scrollRect.pivot = new Vector2(0.5f, 1f);
            scrollRect.anchoredPosition = new Vector2(0f, -65f);

            _listParent = scrollView.GetComponentInChildren<ScrollRect>().content;
        }

        static void Refresh()
        {
            for (int i = _listParent.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(_listParent.GetChild(i).gameObject);
            }

            GUIManager gui = GUIManager.Instance;
            Player player = Player.m_localPlayer;

            foreach (string skillName in SkillRegistry.Names)
            {
                if (!SkillRegistry.TryGet(skillName, out global::Skills.SkillType type)) continue;

                float level = player.GetSkills().GetSkillLevel(type);
                BuildRow(gui, skillName, level);
            }
        }

        static void BuildRow(GUIManager gui, string label, float level)
        {
            GameObject row = new GameObject("SkillRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(_listParent, false);
            row.GetComponent<LayoutElement>().preferredHeight = 36f;
            HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = false;
            layout.childForceExpandWidth = false;
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleLeft;

            gui.CreateText(label, row.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero,
                gui.AveriaSerifBold, 16, gui.ValheimOrange, true, Color.black, 260f, 34f, false);

            gui.CreateText($"Level {(int)level}", row.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero,
                gui.AveriaSerif, 16, Color.white, false, Color.black, 140f, 34f, false);
        }
    }
}
