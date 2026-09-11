using System;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;
using ThunderFury.DevTool.Data;
using static ThunderFury.DevTool.UI.DevToolUiHelpers;

namespace ThunderFury.DevTool.UI
{
    // ---- Values tab: every Config.Bind entry, from every loaded mod ----
    //
    // Deliberately generic: reads BepInEx.Bootstrap.Chainloader.PluginInfos
    // directly rather than each mod registering its config with DevTool by
    // hand, so a new mini-mod's tunables show up here automatically with
    // zero wiring on that mod's side -- confirmed BaseUnityPlugin.Config
    // (a ConfigFile, itself a keyed collection of ConfigDefinition ->
    // ConfigEntryBase) is public on every plugin instance already sitting
    // in the chainloader's plugin list. This is a superset of what the
    // third-party BepInEx.ConfigurationManager mod does (see
    // docs/PROGRESS.md's old recommendation) -- one less companion mod to
    // tell players to install.
    //
    // First-cut scope, called out rather than faked: bool/int/float/string
    // entries are live-editable (ConfigEntryBase.BoxedValue's setter
    // raises the same SettingChanged plumbing a manual .Value assignment
    // would, so patches reading .Value elsewhere pick the change up
    // immediately, no restart). Everything else (enum, KeyboardShortcut,
    // Color, etc.) is shown read-only for now -- a real remap/color-picker
    // control per BepInEx setting type is its own follow-up pass.
    public class ValuesTab : DevToolOverlay.ITab
    {
        public string Title => "Values";

        const float RowHeight = 34f;
        const float LabelWidth = 380f;
        const float ControlWidth = 160f;

        public void Build(Transform contentParent)
        {
            GUIManager gui = GUIManager.Instance;

            // ---- Backup ----
            // "Before I do anything drastic" safety net -- copies every
            // JSON file in DevToolData/ (items, recipes, skills, pieces,
            // creatures, quests, loot pools, world/zone overrides) into a
            // timestamped subfolder. See DevToolBackup.cs for why this is
            // deliberately just a file copy rather than a designed
            // export/import format.
            TextRow(gui, contentParent, "Backup", true);
            GameObject backupButton = Button(gui, contentParent, "Backup All Dev Tool Data Now");
            backupButton.GetComponent<Button>().onClick.AddListener(() =>
            {
                string backupDir = DevToolBackup.CreateBackup();
                Player.m_localPlayer?.Message(MessageHud.MessageType.Center, $"Backed up to {backupDir}");
            });

            TextRow(gui, contentParent, "Config Values", true);

            foreach (var pluginEntry in Chainloader.PluginInfos)
            {
                var pluginInfo = pluginEntry.Value;
                var plugin = pluginInfo.Instance;
                if (plugin == null || plugin.Config.Count == 0) continue;

                BuildHeader(gui, contentParent, pluginInfo.Metadata.Name);

                foreach (ConfigDefinition def in plugin.Config.Keys)
                {
                    BuildRow(gui, contentParent, def, plugin.Config[def]);
                }
            }
        }

        static void BuildHeader(GUIManager gui, Transform parent, string title)
        {
            GameObject header = new GameObject("Header", typeof(RectTransform), typeof(LayoutElement));
            header.transform.SetParent(parent, false);
            header.GetComponent<LayoutElement>().preferredHeight = 32f;

            GameObject titleGO = gui.CreateText(title, header.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                Vector2.zero, gui.AveriaSerifBold, 18, gui.ValheimYellow, true, Color.black,
                LabelWidth + ControlWidth, 30f, false);

            // Same left-edge-anchor/center-pivot mismatch as
            // DevToolUiHelpers.TextRow -- see that method's comment.
            // This header wrapper has no layout group either, so it
            // needs the same direct fix.
            titleGO.GetComponent<RectTransform>().pivot = new Vector2(0f, 0.5f);
        }

        static void BuildRow(GUIManager gui, Transform parent, ConfigDefinition def, ConfigEntryBase entry)
        {
            GameObject row = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            row.GetComponent<LayoutElement>().preferredHeight = RowHeight;

            HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleLeft;

            string label = $"[{def.Section}] {def.Key}";
            gui.CreateText(label, row.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                Vector2.zero, gui.AveriaSerif, 14, Color.white, false, Color.black, LabelWidth, RowHeight, false);

            Type settingType = entry.SettingType;

            if (settingType == typeof(bool))
            {
                GameObject toggle = gui.CreateToggle(row.transform, 24f, 24f);
                Toggle toggleComponent = toggle.GetComponent<Toggle>();
                toggleComponent.SetIsOnWithoutNotify((bool)entry.BoxedValue);
                toggleComponent.onValueChanged.AddListener(value => entry.BoxedValue = value);
                return;
            }

            if (settingType == typeof(int) || settingType == typeof(float) ||
                settingType == typeof(double) || settingType == typeof(string))
            {
                GameObject field = gui.CreateInputField(row.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    Vector2.zero, InputField.ContentType.Standard, null, 14, ControlWidth, RowHeight - 4f);
                InputField inputField = field.GetComponent<InputField>();
                inputField.text = Convert.ToString(entry.BoxedValue);
                inputField.onEndEdit.AddListener(text => ApplyValue(entry, settingType, text));
                return;
            }

            gui.CreateText(Convert.ToString(entry.BoxedValue), row.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                Vector2.zero, gui.AveriaSerif, 14, Color.gray, false, Color.black, ControlWidth, RowHeight, false);
        }

        static void ApplyValue(ConfigEntryBase entry, Type settingType, string text)
        {
            try
            {
                if (settingType == typeof(int) && int.TryParse(text, out int i)) entry.BoxedValue = i;
                else if (settingType == typeof(float) && float.TryParse(text, out float f)) entry.BoxedValue = f;
                else if (settingType == typeof(double) && double.TryParse(text, out double d)) entry.BoxedValue = d;
                else if (settingType == typeof(string)) entry.BoxedValue = text;
            }
            catch (Exception e)
            {
                Jotunn.Logger.LogWarning($"DevTool: failed to apply '{text}' to {entry.Definition}: {e.Message}");
            }
        }
    }
}
