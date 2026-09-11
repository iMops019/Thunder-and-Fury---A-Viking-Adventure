using System.Collections.Generic;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;
using ThunderFury.DevTool.Data;
using static ThunderFury.DevTool.UI.DevToolUiHelpers;

namespace ThunderFury.DevTool.UI
{
    // ---- Item Creator tab ----
    //
    // Clones a vanilla base item by name, sets stat multipliers, and
    // auto-generates its recipe -- the same shape as StonePickaxe.cs and
    // VoltunsSet.cs, generalized into a form. First-cut scope, flagged
    // rather than hidden: capped at a fixed number of requirement slots
    // (see MaxRequirementSlots) instead of a fully dynamic add/remove
    // list -- covers every real Valheim recipe seen in this codebase so
    // far (none use more than 3), and a dynamic list is real UI work for
    // comparatively little gain.
    //
    // "Edit" added 2026-09-10 (user's own audit: every list needs
    // create/change/modify, not just create+remove) -- clicking it loads
    // that entry's saved values back into the form above instead of
    // forcing a blind remove-and-retype. All form fields are instance
    // fields for exactly this reason (RefreshList's row buttons need to
    // reach back into them).
    public class ItemCreatorTab : DevToolOverlay.ITab
    {
        public string Title => "Items";

        const int MaxRequirementSlots = 4;

        InputField _nameField;
        InputField _baseField;
        InputField _displayField;
        InputField _descField;
        InputField _damageField;
        InputField _lightningField;
        InputField _fireField;
        InputField _frostField;
        InputField _poisonField;
        InputField _armorField;
        InputField _weightField;
        InputField _durabilityField;
        InputField _speedField;
        InputField _statusEffectField;
        Dropdown _specialEffectDropdown;
        static readonly string[] SpecialEffectOptions = { "None", "ChainLightning" };
        InputField _minLevelField;
        readonly InputField[] _requirementItemFields = new InputField[MaxRequirementSlots];
        readonly InputField[] _requirementAmountFields = new InputField[MaxRequirementSlots];
        Dropdown _stationDropdown;
        string[] _stationInternalNames;
        Transform _listParent;
        Text _previewText;

        InputField _dropItemField;
        InputField _dropCreatureField;
        InputField _dropChanceField;
        InputField _dropMinField;
        InputField _dropMaxField;
        Transform _dropListParent;

        public void Build(Transform contentParent)
        {
            GUIManager gui = GUIManager.Instance;

            TextRow(gui, contentParent, "Create / Edit an Item", true);

            _nameField = FieldRow(gui, contentParent, "Prefab Name (unique, no spaces)", "");
            _baseField = FieldRowWithPicker(gui, contentParent, "Base Vanilla Item", "");
            _displayField = FieldRow(gui, contentParent, "Display Name (optional)", "");
            _descField = FieldRow(gui, contentParent, "Description", "");

            _damageField = FieldRow(gui, contentParent, "Damage Multiplier (1 = same as base)", "1");
            _lightningField = FieldRow(gui, contentParent, "Bonus Lightning Damage (added, not multiplied)", "0");
            _fireField = FieldRow(gui, contentParent, "Bonus Fire Damage (added, not multiplied)", "0");
            _frostField = FieldRow(gui, contentParent, "Bonus Frost Damage (added, not multiplied)", "0");
            _poisonField = FieldRow(gui, contentParent, "Bonus Poison Damage (added, not multiplied)", "0");
            _armorField = FieldRow(gui, contentParent, "Armor Multiplier", "1");
            _weightField = FieldRow(gui, contentParent, "Weight Multiplier", "1");
            _durabilityField = FieldRow(gui, contentParent, "Durability Multiplier", "1");
            _speedField = FieldRow(gui, contentParent, "Attack Speed Multiplier", "1");
            _statusEffectField = FieldRowWithStatusEffectPicker(gui, contentParent, "Status Effect (optional, food/potions or equip buffs)", "");
            _specialEffectDropdown = DropdownRow(gui, contentParent, "Special On-Hit Effect (optional weapon proc)", new List<string>(SpecialEffectOptions));

            // ---- Live stat preview ----
            // Closes a real "flying blind" gap: multipliers alone don't
            // tell you what a base item's REAL numbers are, so you never
            // knew the actual resulting damage/armor/etc. without saving,
            // alt-tabbing into the game, and checking. Reads the base
            // item's live template values (PrefabManager.GetPrefab --
            // the un-cloned prefab still sitting in ObjectDB, never
            // mutated) and multiplies them the exact same way
            // DevItemRegistry.ApplyStatOverrides does, so what's shown
            // here always matches what Save & Register will actually
            // produce.
            GameObject previewRow = new GameObject("Preview", typeof(RectTransform), typeof(LayoutElement));
            previewRow.transform.SetParent(contentParent, false);
            previewRow.GetComponent<LayoutElement>().preferredHeight = 90f;
            GameObject previewTextGO = gui.CreateText("Type a Base Vanilla Item above to preview its real stats.",
                previewRow.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero,
                gui.AveriaSerif, 14, gui.ValheimYellow, true, Color.black, 920f, 90f, false);
            previewTextGO.GetComponent<RectTransform>().pivot = new Vector2(0f, 0.5f);
            _previewText = previewTextGO.GetComponent<Text>();

            _baseField.onValueChanged.AddListener(_ => RefreshPreview());
            _damageField.onValueChanged.AddListener(_ => RefreshPreview());
            _lightningField.onValueChanged.AddListener(_ => RefreshPreview());
            _fireField.onValueChanged.AddListener(_ => RefreshPreview());
            _frostField.onValueChanged.AddListener(_ => RefreshPreview());
            _poisonField.onValueChanged.AddListener(_ => RefreshPreview());
            _armorField.onValueChanged.AddListener(_ => RefreshPreview());
            _weightField.onValueChanged.AddListener(_ => RefreshPreview());
            _durabilityField.onValueChanged.AddListener(_ => RefreshPreview());
            _speedField.onValueChanged.AddListener(_ => RefreshPreview());

            Dictionary<string, string> stationNames = CraftingStations.GetNames();
            _stationInternalNames = new string[stationNames.Count];
            List<string> stationDisplayNames = new List<string>();
            int si = 0;
            foreach (var kv in stationNames)
            {
                _stationInternalNames[si] = kv.Value;
                stationDisplayNames.Add(kv.Key);
                si++;
            }
            _stationDropdown = DropdownRow(gui, contentParent, "Crafting Station", stationDisplayNames);
            _minLevelField = FieldRow(gui, contentParent, "Min Station Level", "1");

            TextRow(gui, contentParent, "Requirements (leave item name blank to skip a slot)", false);
            for (int i = 0; i < MaxRequirementSlots; i++)
            {
                _requirementItemFields[i] = FieldRowWithPicker(gui, contentParent, $"Material {i + 1} Item Name", "");
                _requirementAmountFields[i] = FieldRow(gui, contentParent, $"Material {i + 1} Amount", "1");
            }

            GameObject saveButton = Button(gui, contentParent, "Save & Register");
            saveButton.GetComponent<Button>().onClick.AddListener(() => SaveItem(gui));

            GameObject clearButton = Button(gui, contentParent, "Clear Form (New Item)");
            clearButton.GetComponent<Button>().onClick.AddListener(() => LoadIntoForm(new DevItemDefinition
            {
                DamageMultiplier = 1f, ArmorMultiplier = 1f, WeightMultiplier = 1f, DurabilityMultiplier = 1f, SpeedMultiplier = 1f, MinStationLevel = 1,
            }));

            TextRow(gui, contentParent, "Existing Items", true);
            GameObject listContainer = new GameObject("ItemList", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            listContainer.transform.SetParent(contentParent, false);
            VerticalLayoutGroup listLayout = listContainer.GetComponent<VerticalLayoutGroup>();
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;
            listLayout.childControlHeight = true;
            listContainer.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _listParent = listContainer.transform;

            RefreshList(gui);

            // ---- Drop Sources ----
            // Closes a real gap flagged by the user (2026-09-10): the
            // form above only ever handles CRAFTING. There was no way at
            // all to make an item a creature/boss drop instead of (or
            // alongside) a recipe -- a genuinely separate vanilla
            // mechanism (CharacterDrop, see CreatureDropRegistry.cs) that
            // nothing here touched before.
            TextRow(gui, contentParent, "Drop Sources (make any item drop from a creature or boss)", true);
            TextRow(gui, contentParent,
                "Works for vanilla items too, not just ones made above -- pick any item and any creature.",
                false, 40f);

            _dropItemField = FieldRowWithPicker(gui, contentParent, "Item", "");
            _dropCreatureField = FieldRowWithCreaturePicker(gui, contentParent, "Creature", "");
            _dropChanceField = FieldRow(gui, contentParent, "Drop Chance % (per kill)", "100");
            _dropMinField = FieldRow(gui, contentParent, "Min Amount", "1");
            _dropMaxField = FieldRow(gui, contentParent, "Max Amount", "1");

            GameObject addDropButton = Button(gui, contentParent, "Save Drop Source");
            addDropButton.GetComponent<Button>().onClick.AddListener(() => SaveDrop(gui));

            TextRow(gui, contentParent, "Current Drop Sources", true);
            GameObject dropListContainer = new GameObject("DropList", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            dropListContainer.transform.SetParent(contentParent, false);
            VerticalLayoutGroup dropListLayout = dropListContainer.GetComponent<VerticalLayoutGroup>();
            dropListLayout.childForceExpandWidth = true;
            dropListLayout.childForceExpandHeight = false;
            dropListLayout.childControlHeight = true;
            dropListContainer.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _dropListParent = dropListContainer.transform;

            RefreshDropList(gui);
        }

        void RefreshPreview()
        {
            string baseName = _baseField.text.Trim();
            if (string.IsNullOrEmpty(baseName))
            {
                _previewText.text = "Type a Base Vanilla Item above to preview its real stats.";
                return;
            }

            GameObject basePrefab = PrefabManager.Instance.GetPrefab(baseName);
            ItemDrop.ItemData.SharedData shared = basePrefab?.GetComponent<ItemDrop>()?.m_itemData?.m_shared;
            if (shared == null)
            {
                _previewText.text = $"'{baseName}' not found (yet) -- can't preview. It may still resolve fine at registration time.";
                return;
            }

            float damageMult = ParseFloat(_damageField.text, 1f);
            float armorMult = ParseFloat(_armorField.text, 1f);
            float weightMult = ParseFloat(_weightField.text, 1f);
            float durabilityMult = ParseFloat(_durabilityField.text, 1f);
            float speedMult = ParseFloat(_speedField.text, 1f);
            float bonusLightning = ParseFloat(_lightningField.text, 0f);
            float bonusFire = ParseFloat(_fireField.text, 0f);
            float bonusFrost = ParseFloat(_frostField.text, 0f);
            float bonusPoison = ParseFloat(_poisonField.text, 0f);

            float basePhysicalDamage = shared.m_damages.m_damage + shared.m_damages.m_blunt + shared.m_damages.m_slash +
                shared.m_damages.m_pierce + shared.m_damages.m_chop + shared.m_damages.m_pickaxe;
            float baseElementalDamage = shared.m_damages.m_fire + shared.m_damages.m_frost + shared.m_damages.m_lightning + shared.m_damages.m_poison;
            float resultingElementalDamage = (shared.m_damages.m_fire * damageMult + bonusFire) +
                (shared.m_damages.m_frost * damageMult + bonusFrost) +
                (shared.m_damages.m_lightning * damageMult + bonusLightning) +
                (shared.m_damages.m_poison * damageMult + bonusPoison);
            float baseSpeed = shared.m_attack?.m_speedFactor ?? 0f;

            _previewText.text =
                $"'{baseName}' real stats -> resulting stats:\n" +
                $"Physical Damage: {basePhysicalDamage:0.#} -> {basePhysicalDamage * damageMult:0.#}   |   Elemental Damage: {baseElementalDamage:0.#} -> {resultingElementalDamage:0.#}\n" +
                $"Armor: {shared.m_armor:0.#} -> {shared.m_armor * armorMult:0.#}   |   Weight: {shared.m_weight:0.#} -> {shared.m_weight * weightMult:0.#}   |   Durability: {shared.m_maxDurability:0.#} -> {shared.m_maxDurability * durabilityMult:0.#}" +
                (shared.m_attack != null ? $"   |   Attack Speed Factor: {baseSpeed:0.##} -> {baseSpeed * speedMult:0.##}" : "");
        }

        void SaveItem(GUIManager gui)
        {
            DevItemDefinition def = new DevItemDefinition
            {
                Name = _nameField.text.Trim(),
                BasePrefabName = _baseField.text.Trim(),
                DisplayName = _displayField.text.Trim(),
                Description = _descField.text.Trim(),
                DamageMultiplier = ParseFloat(_damageField.text, 1f),
                BonusLightningDamage = ParseFloat(_lightningField.text, 0f),
                BonusFireDamage = ParseFloat(_fireField.text, 0f),
                BonusFrostDamage = ParseFloat(_frostField.text, 0f),
                BonusPoisonDamage = ParseFloat(_poisonField.text, 0f),
                ArmorMultiplier = ParseFloat(_armorField.text, 1f),
                WeightMultiplier = ParseFloat(_weightField.text, 1f),
                DurabilityMultiplier = ParseFloat(_durabilityField.text, 1f),
                SpeedMultiplier = ParseFloat(_speedField.text, 1f),
                StatusEffectName = _statusEffectField.text.Trim(),
                SpecialEffect = SpecialEffectOptions[_specialEffectDropdown.value],
                CraftingStation = _stationInternalNames[_stationDropdown.value],
                MinStationLevel = ParseInt(_minLevelField.text, 1),
            };

            for (int i = 0; i < MaxRequirementSlots; i++)
            {
                def.Requirements.Add(new DevRequirement
                {
                    ItemName = _requirementItemFields[i].text.Trim(),
                    Amount = ParseInt(_requirementAmountFields[i].text, 1),
                });
            }

            if (string.IsNullOrEmpty(def.Name) || string.IsNullOrEmpty(def.BasePrefabName))
            {
                Jotunn.Logger.LogWarning("DevTool Item Creator: Prefab Name and Base Vanilla Item are required");
                return;
            }

            DevItemRegistry.Definitions.RemoveAll(existing => existing.Name == def.Name);
            DevItemRegistry.Definitions.Add(def);
            DevItemRegistry.Save();
            bool registered = DevItemRegistry.Register(def);

            Player.m_localPlayer?.Message(MessageHud.MessageType.Center,
                registered
                    ? $"Saved '{def.Name}'. Live this session; a world reload picks it up cleanly everywhere."
                    : $"Saved '{def.Name}', but registration failed this session -- check the log (likely a bad base item name).");

            RefreshList(gui);
        }

        // Pushes a definition's values back into the form -- the actual
        // "Edit" flow. Re-clicking "Save & Register" afterward overwrites
        // the same entry (matched by Name), it doesn't create a duplicate.
        void LoadIntoForm(DevItemDefinition def)
        {
            _nameField.text = def.Name;
            _baseField.text = def.BasePrefabName;
            _displayField.text = def.DisplayName;
            _descField.text = def.Description;
            _damageField.text = def.DamageMultiplier.ToString();
            _lightningField.text = def.BonusLightningDamage.ToString();
            _fireField.text = def.BonusFireDamage.ToString();
            _frostField.text = def.BonusFrostDamage.ToString();
            _poisonField.text = def.BonusPoisonDamage.ToString();
            _armorField.text = def.ArmorMultiplier.ToString();
            _weightField.text = def.WeightMultiplier.ToString();
            _durabilityField.text = def.DurabilityMultiplier.ToString();
            _speedField.text = def.SpeedMultiplier.ToString();
            _statusEffectField.text = def.StatusEffectName;
            int specialEffectIndex = System.Array.IndexOf(SpecialEffectOptions, string.IsNullOrEmpty(def.SpecialEffect) ? "None" : def.SpecialEffect);
            _specialEffectDropdown.value = specialEffectIndex >= 0 ? specialEffectIndex : 0;
            _minLevelField.text = def.MinStationLevel.ToString();

            int stationIndex = System.Array.IndexOf(_stationInternalNames, def.CraftingStation);
            _stationDropdown.value = stationIndex >= 0 ? stationIndex : 0;

            for (int i = 0; i < MaxRequirementSlots; i++)
            {
                bool hasSlot = i < def.Requirements.Count;
                _requirementItemFields[i].text = hasSlot ? def.Requirements[i].ItemName : "";
                _requirementAmountFields[i].text = hasSlot ? def.Requirements[i].Amount.ToString() : "1";
            }
        }

        void SaveDrop(GUIManager gui)
        {
            string itemName = _dropItemField.text.Trim();
            string creatureName = _dropCreatureField.text.Trim();
            if (string.IsNullOrEmpty(itemName) || string.IsNullOrEmpty(creatureName))
            {
                Jotunn.Logger.LogWarning("DevTool Item Creator: both Item and Creature are required to add a drop source");
                return;
            }

            CreatureDropRegistry.Entries.RemoveAll(e => e.ItemName == itemName && e.CreatureName == creatureName);
            CreatureDropRegistry.Entries.Add(new CreatureDropEntry
            {
                ItemName = itemName,
                CreatureName = creatureName,
                ChancePercent = ParseFloat(_dropChanceField.text, 100f),
                MinAmount = ParseInt(_dropMinField.text, 1),
                MaxAmount = ParseInt(_dropMaxField.text, 1),
            });
            CreatureDropRegistry.Save();

            Player.m_localPlayer?.Message(MessageHud.MessageType.Center,
                $"'{itemName}' will now drop from {creatureName}. Live for creatures already loaded; a world reload picks it up everywhere.");

            CreatureDropRegistry.LoadAndApplyAll();
            RefreshDropList(gui);
        }

        void RefreshList(GUIManager gui)
        {
            for (int i = _listParent.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(_listParent.GetChild(i).gameObject);
            }

            foreach (DevItemDefinition def in DevItemRegistry.Definitions)
            {
                GameObject row = new GameObject("ItemRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
                row.transform.SetParent(_listParent, false);
                row.GetComponent<LayoutElement>().preferredHeight = RowHeight;
                row.GetComponent<HorizontalLayoutGroup>().childControlWidth = false;
                row.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
                row.GetComponent<HorizontalLayoutGroup>().spacing = 10f;

                gui.CreateText($"{def.Name}  (from {def.BasePrefabName})", row.transform,
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
                    DevItemRegistry.Definitions.Remove(def);
                    DevItemRegistry.Save();
                    RefreshList(gui);
                });
            }
        }

        void RefreshDropList(GUIManager gui)
        {
            for (int i = _dropListParent.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(_dropListParent.GetChild(i).gameObject);
            }

            foreach (CreatureDropEntry entry in CreatureDropRegistry.Entries)
            {
                GameObject row = new GameObject("DropRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
                row.transform.SetParent(_dropListParent, false);
                row.GetComponent<LayoutElement>().preferredHeight = RowHeight;
                row.GetComponent<HorizontalLayoutGroup>().childControlWidth = false;
                row.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
                row.GetComponent<HorizontalLayoutGroup>().spacing = 10f;

                gui.CreateText($"{entry.ItemName}  <-  {entry.CreatureName}  ({entry.ChancePercent}%, x{entry.MinAmount}-{entry.MaxAmount})", row.transform,
                    new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero,
                    gui.AveriaSerif, 14, Color.white, false, Color.black, 450f, RowHeight, false);

                GameObject editButton = gui.CreateButton("Edit", row.transform, Vector2.zero, Vector2.zero, Vector2.zero, 90f, 28f);
                editButton.GetComponent<Button>().onClick.AddListener(() =>
                {
                    _dropItemField.text = entry.ItemName;
                    _dropCreatureField.text = entry.CreatureName;
                    _dropChanceField.text = entry.ChancePercent.ToString();
                    _dropMinField.text = entry.MinAmount.ToString();
                    _dropMaxField.text = entry.MaxAmount.ToString();
                });

                GameObject removeButton = gui.CreateButton("Remove", row.transform, Vector2.zero, Vector2.zero, Vector2.zero, 100f, 28f);
                removeButton.GetComponent<Button>().onClick.AddListener(() =>
                {
                    CreatureDropRegistry.Entries.Remove(entry);
                    CreatureDropRegistry.Save();
                    RefreshDropList(gui);
                });
            }
        }
    }
}
