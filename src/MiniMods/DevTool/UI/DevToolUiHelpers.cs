using System.Collections.Generic;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace ThunderFury.DevTool.UI
{
    // Shared row-building helpers for the form-style tabs (Items, Recipes,
    // Skills). Every tab's content area is the same vertical-layout scroll
    // view content (see DevToolOverlay), so a "row" here is just a
    // horizontal strip added as one more child of whatever Transform is
    // passed in.
    public static class DevToolUiHelpers
    {
        public const float RowHeight = 34f;
        public const float LabelWidth = 220f;
        public const float FieldWidth = 220f;

        // height: only matters for non-header (description/note) rows --
        // Unity's Text wraps horizontally by default but TRUNCATES
        // (silently drops) whatever doesn't fit vertically, which is
        // exactly the "text doesn't fit the screen" bug reported in-game
        // (2026-09-10): a fixed 24px row only ever had room for one line,
        // clipping every multi-sentence note in this file. Callers with a
        // longer note should pass a taller height (~44 for ~2 lines, ~60
        // for ~3) rather than relying on the old one-line default.
        public static void TextRow(GUIManager gui, Transform parent, string text, bool header, float height = 0f)
        {
            float resolvedHeight = height > 0f ? height : (header ? 30f : 40f);

            GameObject go = new GameObject("Label", typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<LayoutElement>().preferredHeight = resolvedHeight;

            GameObject textGO = gui.CreateText(text, go.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero,
                header ? gui.AveriaSerifBold : gui.AveriaSerif, header ? 18 : 13,
                header ? gui.ValheimYellow : Color.gray, true, Color.black, 920f, resolvedHeight, false);

            FixLeftPivot(textGO);
        }

        // CreateText anchors a left-edge point (0, 0.5) but leaves
        // Unity's default (0.5, 0.5) CENTER pivot -- with no layout
        // group repositioning the child (unlike a HorizontalLayoutGroup
        // row, which recomputes child positions itself and happens to
        // paper over this), that mismatch renders the text centered ON
        // the left edge, pushing roughly half of it off-panel to the
        // left. Confirmed in-game 2026-09-10 (screenshot showed World
        // tab's header/note text cut off on the left). Setting the pivot
        // to match the anchor point makes it genuinely left-aligned.
        static void FixLeftPivot(GameObject textGO)
        {
            RectTransform rect = textGO.GetComponent<RectTransform>();
            rect.pivot = new Vector2(0f, 0.5f);
        }

        public static InputField FieldRow(GUIManager gui, Transform parent, string label, string defaultValue)
        {
            GameObject row = NewRow(parent);

            gui.CreateText(label, row.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero,
                gui.AveriaSerif, 14, Color.white, false, Color.black, LabelWidth, RowHeight, false);

            GameObject field = gui.CreateInputField(row.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                Vector2.zero, InputField.ContentType.Standard, null, 14, FieldWidth, RowHeight - 4f);
            InputField inputField = field.GetComponent<InputField>();
            inputField.text = defaultValue;
            return inputField;
        }

        // options: display text -> value returned via the dropdown's selected index.
        public static Dropdown DropdownRow(GUIManager gui, Transform parent, string label,
            IReadOnlyList<string> displayNames)
        {
            GameObject row = NewRow(parent);

            gui.CreateText(label, row.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero,
                gui.AveriaSerif, 14, Color.white, false, Color.black, LabelWidth, RowHeight, false);

            GameObject dropdownGO = gui.CreateDropDown(row.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                Vector2.zero, 14, FieldWidth, RowHeight - 4f);
            Dropdown dropdown = dropdownGO.GetComponent<Dropdown>();
            dropdown.AddOptions(new List<string>(displayNames));
            return dropdown;
        }

        // A text field plus a "Browse..." button that opens the shared
        // ItemPickerPopup (searchable, live from ObjectDB -- see
        // ItemPickerPopup.cs) and writes the chosen prefab name into the
        // field. Used anywhere a form wants "pick an existing item"
        // instead of typing a raw prefab name by hand.
        public static InputField FieldRowWithPicker(GUIManager gui, Transform parent, string label, string defaultValue)
        {
            GameObject row = NewRow(parent);

            gui.CreateText(label, row.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero,
                gui.AveriaSerif, 14, Color.white, false, Color.black, LabelWidth, RowHeight, false);

            GameObject field = gui.CreateInputField(row.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                Vector2.zero, InputField.ContentType.Standard, null, 14, FieldWidth - 90f, RowHeight - 4f);
            InputField inputField = field.GetComponent<InputField>();
            inputField.text = defaultValue;

            GameObject browseButton = gui.CreateButton("Browse...", row.transform, Vector2.zero, Vector2.zero, Vector2.zero, 100f, RowHeight - 4f);
            browseButton.GetComponent<Button>().onClick.AddListener(() =>
            {
                ItemPickerPopup.Open(chosenName => inputField.text = chosenName);
            });

            return inputField;
        }

        // Same idea as FieldRowWithPicker, but opens CreaturePickerPopup
        // instead -- used for "which creature does this item drop from."
        public static InputField FieldRowWithCreaturePicker(GUIManager gui, Transform parent, string label, string defaultValue)
        {
            GameObject row = NewRow(parent);

            gui.CreateText(label, row.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero,
                gui.AveriaSerif, 14, Color.white, false, Color.black, LabelWidth, RowHeight, false);

            GameObject field = gui.CreateInputField(row.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                Vector2.zero, InputField.ContentType.Standard, null, 14, FieldWidth - 90f, RowHeight - 4f);
            InputField inputField = field.GetComponent<InputField>();
            inputField.text = defaultValue;

            GameObject browseButton = gui.CreateButton("Browse...", row.transform, Vector2.zero, Vector2.zero, Vector2.zero, 100f, RowHeight - 4f);
            browseButton.GetComponent<Button>().onClick.AddListener(() =>
            {
                CreaturePickerPopup.Open(chosenName => inputField.text = chosenName);
            });

            return inputField;
        }

        // Same idea as FieldRowWithPicker/FieldRowWithCreaturePicker, but
        // opens PiecePickerPopup -- used for "which vanilla piece is this
        // built from."
        public static InputField FieldRowWithPiecePicker(GUIManager gui, Transform parent, string label, string defaultValue)
        {
            GameObject row = NewRow(parent);

            gui.CreateText(label, row.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero,
                gui.AveriaSerif, 14, Color.white, false, Color.black, LabelWidth, RowHeight, false);

            GameObject field = gui.CreateInputField(row.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                Vector2.zero, InputField.ContentType.Standard, null, 14, FieldWidth - 90f, RowHeight - 4f);
            InputField inputField = field.GetComponent<InputField>();
            inputField.text = defaultValue;

            GameObject browseButton = gui.CreateButton("Browse...", row.transform, Vector2.zero, Vector2.zero, Vector2.zero, 100f, RowHeight - 4f);
            browseButton.GetComponent<Button>().onClick.AddListener(() =>
            {
                PiecePickerPopup.Open(chosenName => inputField.text = chosenName);
            });

            return inputField;
        }

        // Same idea as the other FieldRowWith*Picker helpers, opens
        // StatusEffectPickerPopup.
        public static InputField FieldRowWithStatusEffectPicker(GUIManager gui, Transform parent, string label, string defaultValue)
        {
            GameObject row = NewRow(parent);

            gui.CreateText(label, row.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero,
                gui.AveriaSerif, 14, Color.white, false, Color.black, LabelWidth, RowHeight, false);

            GameObject field = gui.CreateInputField(row.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                Vector2.zero, InputField.ContentType.Standard, null, 14, FieldWidth - 90f, RowHeight - 4f);
            InputField inputField = field.GetComponent<InputField>();
            inputField.text = defaultValue;

            GameObject browseButton = gui.CreateButton("Browse...", row.transform, Vector2.zero, Vector2.zero, Vector2.zero, 100f, RowHeight - 4f);
            browseButton.GetComponent<Button>().onClick.AddListener(() =>
            {
                StatusEffectPickerPopup.Open(chosenName => inputField.text = chosenName);
            });

            return inputField;
        }

        public static GameObject Button(GUIManager gui, Transform parent, string text, float height = 40f)
        {
            GameObject button = gui.CreateButton(text, parent, Vector2.zero, Vector2.zero, Vector2.zero, 220f, 36f);
            button.AddComponent<LayoutElement>().preferredHeight = height;
            return button;
        }

        static GameObject NewRow(Transform parent)
        {
            GameObject row = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            row.GetComponent<LayoutElement>().preferredHeight = RowHeight;
            HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = false;
            layout.childForceExpandWidth = false;
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            return row;
        }

        public static float ParseFloat(string text, float fallback) => float.TryParse(text, out float v) ? v : fallback;
        public static int ParseInt(string text, int fallback) => int.TryParse(text, out int v) ? v : fallback;
    }
}
