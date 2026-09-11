using System.Collections.Generic;
using System.Linq;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;
using ThunderFury.DevTool.Data;
using static ThunderFury.DevTool.UI.DevToolUiHelpers;

namespace ThunderFury.DevTool.UI
{
    // ---- Skill Creator tab ----
    //
    // Scope locked in this session: only composes mechanics Core has
    // already proven generic. Today that's XP-redirect-from-a-vanilla-
    // skill only (SkillXpRedirect, the exact mechanism every one of the
    // 12 built-in skills already uses) -- station-based crafting XP and
    // Woodcutting/Mining-shaped damage scaling need one more round of
    // Core generalization first (see DevSkillDefinition.cs and
    // docs/PROGRESS.md). Shipping this narrower rather than half-faking
    // the other two.
    //
    // Edit/Remove added 2026-09-10 (user's own audit found this tab was
    // the one gap even behind the others -- no way to change or remove a
    // skill at all before this). Genuinely more limited than Items/
    // Recipes though, flagged honestly in the UI itself, not just a code
    // comment: a vanilla skill redirect can't be cleanly un-registered
    // mid-session (SkillXpRedirect has no unregister, and Jotunn skills
    // aren't designed to be removed once added), so Remove only stops it
    // from being RE-registered on the next reload -- it stays live for
    // the rest of THIS session either way.
    public class SkillCreatorTab : DevToolOverlay.ITab
    {
        public string Title => "Skills";

        InputField _nameField;
        InputField _descField;
        Dropdown _xpDropdown;
        List<string> _xpOptions;
        Transform _listParent;

        public void Build(Transform contentParent)
        {
            GUIManager gui = GUIManager.Instance;

            TextRow(gui, contentParent, "Create / Edit a Skill", true);
            TextRow(gui, contentParent,
                "Scope: composes mechanics Core already proved generic. Only XP-redirect-from-a-vanilla-skill is offered here " +
                "for now -- station-based crafting XP and resource-damage scaling need more Core work first.",
                false, 60f);

            _nameField = FieldRow(gui, contentParent, "Skill Name (unique)", "");
            _descField = FieldRow(gui, contentParent, "Description", "");

            _xpOptions = new List<string> { "None (orphan skill)" };
            _xpOptions.AddRange(System.Enum.GetNames(typeof(global::Skills.SkillType))
                .Where(n => n != "None" && n != "All"));
            _xpDropdown = DropdownRow(gui, contentParent, "XP Source (redirect this vanilla skill's XP)", _xpOptions);

            GameObject saveButton = Button(gui, contentParent, "Save & Register");
            saveButton.GetComponent<Button>().onClick.AddListener(() => SaveSkill(gui));

            GameObject clearButton = Button(gui, contentParent, "Clear Form (New Skill)");
            clearButton.GetComponent<Button>().onClick.AddListener(() => LoadIntoForm(new DevSkillDefinition()));

            TextRow(gui, contentParent, "Existing Dev-Created Skills", true);
            TextRow(gui, contentParent,
                "Remove only stops a skill from re-registering on the next reload -- it can't be un-registered live mid-session.",
                false, 40f);
            GameObject listContainer = new GameObject("SkillList", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            listContainer.transform.SetParent(contentParent, false);
            VerticalLayoutGroup listLayout = listContainer.GetComponent<VerticalLayoutGroup>();
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;
            listLayout.childControlHeight = true;
            listContainer.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _listParent = listContainer.transform;

            RefreshList(gui);
        }

        void SaveSkill(GUIManager gui)
        {
            string name = _nameField.text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                Jotunn.Logger.LogWarning("DevTool Skill Creator: Skill Name is required");
                return;
            }

            DevSkillDefinition def = new DevSkillDefinition
            {
                Name = name,
                Description = _descField.text.Trim(),
                XpSourceVanillaSkill = _xpDropdown.value == 0 ? "" : _xpOptions[_xpDropdown.value],
            };

            DevSkillRegistry.Definitions.RemoveAll(existing => existing.Name == def.Name);
            DevSkillRegistry.Definitions.Add(def);
            DevSkillRegistry.Save();
            bool registered = DevSkillRegistry.Register(def);

            Player.m_localPlayer?.Message(MessageHud.MessageType.Center,
                registered
                    ? $"Saved skill '{def.Name}'. Live this session; a world reload registers it cleanly everywhere."
                    : $"Saved skill '{def.Name}', but registration failed this session -- check the log.");

            RefreshList(gui);
        }

        void LoadIntoForm(DevSkillDefinition def)
        {
            _nameField.text = def.Name;
            _descField.text = def.Description;

            int xpIndex = string.IsNullOrEmpty(def.XpSourceVanillaSkill) ? 0 : _xpOptions.IndexOf(def.XpSourceVanillaSkill);
            _xpDropdown.value = xpIndex >= 0 ? xpIndex : 0;
        }

        void RefreshList(GUIManager gui)
        {
            for (int i = _listParent.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(_listParent.GetChild(i).gameObject);
            }

            foreach (DevSkillDefinition def in DevSkillRegistry.Definitions)
            {
                GameObject row = new GameObject("SkillRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
                row.transform.SetParent(_listParent, false);
                row.GetComponent<LayoutElement>().preferredHeight = RowHeight;
                row.GetComponent<HorizontalLayoutGroup>().childControlWidth = false;
                row.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
                row.GetComponent<HorizontalLayoutGroup>().spacing = 10f;

                string xpSuffix = string.IsNullOrEmpty(def.XpSourceVanillaSkill) ? " (orphan)" : $" (from {def.XpSourceVanillaSkill})";
                gui.CreateText(def.Name + xpSuffix, row.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero,
                    gui.AveriaSerif, 14, Color.white, false, Color.black, 400f, RowHeight, false);

                GameObject editButton = gui.CreateButton("Edit", row.transform, Vector2.zero, Vector2.zero, Vector2.zero, 90f, 28f);
                editButton.GetComponent<Button>().onClick.AddListener(() => LoadIntoForm(def));

                GameObject removeButton = gui.CreateButton("Remove", row.transform, Vector2.zero, Vector2.zero, Vector2.zero, 100f, 28f);
                removeButton.GetComponent<Button>().onClick.AddListener(() =>
                {
                    DevSkillRegistry.Definitions.Remove(def);
                    DevSkillRegistry.Save();
                    Player.m_localPlayer?.Message(MessageHud.MessageType.Center,
                        $"'{def.Name}' won't re-register on the next reload, but is still live for the rest of this session.");
                    RefreshList(gui);
                });
            }
        }
    }
}
