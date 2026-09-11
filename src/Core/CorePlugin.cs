using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;
using ThunderFury.Core.Patches;
using ThunderFury.Core.SkillSystem;
using ThunderFury.Core.UI;

namespace ThunderFury.Core
{
    // Shared framework mod: config, shared prefabs/data, and any cross-cutting
    // hooks the mini-mods below depend on. Mini-mods declare a
    // [BepInDependency(CorePlugin.PluginGUID)] on this and reference Core.dll.
    //
    // Custom skill system lives here (docs/valheim-mod-vision.md Pillar 1)
    // -- Woodcutting is the first skill, built end-to-end as the pattern
    // the other 8 will follow. See SkillSystem/ and Patches/.
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class CorePlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.ThunderFury.core";
        public const string PluginName = "ThunderFury.Core";
        public const string PluginVersion = "0.1.0";

        private readonly Harmony _harmony = new Harmony(PluginGUID);

        public static ConfigEntry<float> WoodcuttingDamagePerLevel;
        public static ConfigEntry<int> WoodcuttingMilestoneLevel;
        public static ConfigEntry<float> WoodcuttingMilestoneXpMultiplier;
        public static ConfigEntry<float> WoodcuttingMilestoneLogYieldBonusPercent;

        public static ConfigEntry<float> MiningDamagePerLevel;
        public static ConfigEntry<int> MiningMilestoneLevel;
        public static ConfigEntry<float> MiningMilestoneXpMultiplier;
        public static ConfigEntry<float> MiningMilestoneOreYieldBonusPercent;

        public static ConfigEntry<float> FishingBiteChanceBonusAtMaxLevel;

        public static ConfigEntry<float> SkinningChannelDuration;

        public static ConfigEntry<float> CookingBurnPreventionChanceAtMaxLevel;
        public static ConfigEntry<float> CookingBurnPreventionRadius;

        public static ConfigEntry<float> StrengthXpShareOfAttack;
        public static ConfigEntry<float> DefenseDamageReductionPerLevel;
        public static ConfigEntry<float> DefenseMinDamageMultiplier;

        public static ConfigEntry<KeyboardShortcut> CharacterSheetToggleKey;

        // ---- Chain Lightning (generic weapon special effect) ----
        // Built for the "Lightning Sword" idea (2026-09-10) but usable by
        // any weapon -- see Combat/WeaponSpecialEffectPatch.cs. User's own
        // framing: "not OP but fun," so these default modest.
        public static ConfigEntry<float> ChainLightningChance;
        public static ConfigEntry<int> ChainLightningMaxJumps;
        public static ConfigEntry<float> ChainLightningRange;
        public static ConfigEntry<float> ChainLightningDamagePerJump;

        private void Awake()
        {
            BindConfig();

            _harmony.PatchAll();

            GUIManager.OnCustomGUIAvailable += CharacterSheetPanel.OnCustomGUIAvailable;

            PrefabManager.OnVanillaPrefabsAvailable += WoodcuttingSkill.Register;
            PrefabManager.OnVanillaPrefabsAvailable += MiningSkill.Register;
            PrefabManager.OnVanillaPrefabsAvailable += FishingSkill.Register;
            PrefabManager.OnVanillaPrefabsAvailable += SkinningSkill.Register;
            PrefabManager.OnVanillaPrefabsAvailable += SkinningSystem.RegisterDefaults;
            PrefabManager.OnVanillaPrefabsAvailable += SmithingSkill.Register;
            PrefabManager.OnVanillaPrefabsAvailable += CookingSkill.Register;
            PrefabManager.OnVanillaPrefabsAvailable += FletchingSkill.Register;
            PrefabManager.OnVanillaPrefabsAvailable += BuildingSkill.Register;
            PrefabManager.OnVanillaPrefabsAvailable += CraftingSkill.Register;
            PrefabManager.OnVanillaPrefabsAvailable += AttackSkill.Register;
            PrefabManager.OnVanillaPrefabsAvailable += StrengthSkill.Register;
            PrefabManager.OnVanillaPrefabsAvailable += DefenseSkill.Register;

            Jotunn.Logger.LogInfo($"{PluginName} {PluginVersion} loaded");
        }

        private void BindConfig()
        {
            WoodcuttingDamagePerLevel = Config.Bind(
                "Woodcutting", "DamagePerLevel", 0.01f,
                "Extra chop damage per Woodcutting level, as a fraction. 0.01 = +1% per level, so level 50 = +50%.");

            WoodcuttingMilestoneLevel = Config.Bind(
                "Woodcutting", "MilestoneLevel", 15,
                "Woodcutting level that unlocks the milestone bonus (vision.md: double XP + bonus log yield from normal trees).");

            WoodcuttingMilestoneXpMultiplier = Config.Bind(
                "Woodcutting", "MilestoneXpMultiplier", 2f,
                "Woodcutting XP multiplier once the milestone level is reached. 2.0 = double XP.");

            WoodcuttingMilestoneLogYieldBonusPercent = Config.Bind(
                "Woodcutting", "MilestoneLogYieldBonusPercent", 25f,
                "Extra Wood dropped per chopped log once the milestone level is reached, as a percentage. 25 = +25% more Wood.");

            MiningDamagePerLevel = Config.Bind(
                "Mining", "DamagePerLevel", 0.01f,
                "Extra mining damage per Mining level, as a fraction. 0.01 = +1% per level, so level 50 = +50%.");

            MiningMilestoneLevel = Config.Bind(
                "Mining", "MilestoneLevel", 15,
                "Mining level that unlocks the milestone bonus -- shares Woodcutting's milestone level and shape (double XP + bonus yield), applied to ore instead of logs.");

            MiningMilestoneXpMultiplier = Config.Bind(
                "Mining", "MilestoneXpMultiplier", 2f,
                "Mining XP multiplier once the milestone level is reached. 2.0 = double XP.");

            MiningMilestoneOreYieldBonusPercent = Config.Bind(
                "Mining", "MilestoneOreYieldBonusPercent", 25f,
                "Extra ore/stone dropped per destroyed rock node once the milestone level is reached, as a percentage. 25 = +25% more.");

            FishingBiteChanceBonusAtMaxLevel = Config.Bind(
                "Fishing", "BiteChanceBonusAtMaxLevel", 0.5f,
                "Relative increase to a fish's chance to bite your line, at level 100 (max skill). 0.5 = +50% relative bite chance at level 100, scaling smoothly from 0 at level 0.");

            SkinningChannelDuration = Config.Bind(
                "Skinning", "ChannelDurationSeconds", 2.5f,
                "How long you must hold [E] on a carcass before it gives up its hide/meat. Pure flavor (user's own framing, 2026-09-10) -- not a difficulty gate.");

            CookingBurnPreventionChanceAtMaxLevel = Config.Bind(
                "Cooking", "BurnPreventionChanceAtMaxLevel", 0.75f,
                "Chance to save food that would otherwise burn, at level 100 (max skill). 0.75 = 75% chance at level 100, scaling smoothly from 0 at level 0.");

            CookingBurnPreventionRadius = Config.Bind(
                "Cooking", "BurnPreventionRadius", 10f,
                "Radius (meters) around a cooking station to find the player whose Cooking level applies -- there's no per-slot ownership tracked by vanilla, so this uses the closest player, same approximation Fishing uses for bite chance.");

            StrengthXpShareOfAttack = Config.Bind(
                "Strength", "XpShareOfAttack", 1f,
                "Fraction of Attack's XP that Strength also gains from the same weapon hit. 1.0 = Strength levels at the same rate as Attack.");

            DefenseDamageReductionPerLevel = Config.Bind(
                "Defense", "DamageReductionPerLevel", 0.01f,
                "Incoming damage reduction per Defense level, as a fraction. 0.01 = -1% per level, so level 50 = -50% (before the floor below applies).");

            DefenseMinDamageMultiplier = Config.Bind(
                "Defense", "MinDamageMultiplier", 0.1f,
                "Floor on the damage multiplier DefenseDamageReductionPerLevel can reach, so high Defense can't be tuned into literal invincibility. 0.1 = incoming damage can never be reduced below 10% of its original value.");

            ChainLightningChance = Config.Bind(
                "ChainLightning", "TriggerChance", 0.2f,
                "Chance per hit for a weapon with the Chain Lightning special effect to trigger it. 0.2 = 20%.");

            ChainLightningMaxJumps = Config.Bind(
                "ChainLightning", "MaxJumps", 3,
                "Maximum number of additional targets a triggered Chain Lightning can jump to.");

            ChainLightningRange = Config.Bind(
                "ChainLightning", "JumpRange", 8f,
                "Range (meters) Chain Lightning searches for the next target from the last one hit.");

            ChainLightningDamagePerJump = Config.Bind(
                "ChainLightning", "DamagePerJump", 10f,
                "Lightning damage dealt to each additional target Chain Lightning jumps to.");

            CharacterSheetToggleKey = Config.Bind(
                "UI", "CharacterSheetToggleKey", new KeyboardShortcut(KeyCode.C),
                "Opens/closes the Character Sheet panel (all 12 skills and their levels in one place).");
        }

        private void Update()
        {
            // "C" is a very ordinary character to type into chat --
            // guard against toggling mid-message. Chat.HasFocus() and
            // TextInput.IsVisible() are the two real vanilla "a text box
            // currently has focus" checks (confirmed via decompile;
            // GUIManager's own internal input-blocking patches
            // TextInput.IsVisible for the same reason).
            if (global::Chat.instance != null && global::Chat.instance.HasFocus()) return;
            if (TextInput.IsVisible()) return;

            if (CharacterSheetToggleKey.Value.IsDown())
            {
                CharacterSheetPanel.Toggle();
            }
        }

        private void OnDestroy()
        {
            _harmony.UnpatchSelf();
        }
    }
}
