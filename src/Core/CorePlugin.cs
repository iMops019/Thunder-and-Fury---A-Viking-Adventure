using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Managers;
using Jotunn.Utils;
using VikingAdventure.Core.SkillSystem;

namespace VikingAdventure.Core
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
        public const string PluginGUID = "com.vikingadventure.core";
        public const string PluginName = "VikingAdventure.Core";
        public const string PluginVersion = "0.1.0";

        private readonly Harmony _harmony = new Harmony(PluginGUID);

        public static ConfigEntry<float> WoodcuttingDamagePerLevel;
        public static ConfigEntry<float> WoodcuttingStaminaEfficiencyWeight;
        public static ConfigEntry<int> WoodcuttingMilestoneLevel;
        public static ConfigEntry<float> WoodcuttingMilestoneXpMultiplier;
        public static ConfigEntry<float> WoodcuttingMilestoneLogYieldBonusPercent;

        public static ConfigEntry<float> MiningDamagePerLevel;
        public static ConfigEntry<float> MiningStaminaEfficiencyWeight;
        public static ConfigEntry<int> MiningMilestoneLevel;
        public static ConfigEntry<float> MiningMilestoneXpMultiplier;
        public static ConfigEntry<float> MiningMilestoneOreYieldBonusPercent;

        private void Awake()
        {
            BindConfig();

            _harmony.PatchAll();

            PrefabManager.OnVanillaPrefabsAvailable += WoodcuttingSkill.Register;
            PrefabManager.OnVanillaPrefabsAvailable += MiningSkill.Register;

            Jotunn.Logger.LogInfo($"{PluginName} {PluginVersion} loaded");
        }

        private void BindConfig()
        {
            WoodcuttingDamagePerLevel = Config.Bind(
                "Woodcutting", "DamagePerLevel", 0.01f,
                "Extra chop damage per Woodcutting level, as a fraction. 0.01 = +1% per level, so level 50 = +50%.");

            WoodcuttingStaminaEfficiencyWeight = Config.Bind(
                "Woodcutting", "StaminaEfficiencyWeight", 0.33f,
                "How much Woodcutting level reduces chop stamina cost, at level 100 (max skill). 0.33 = -33% at level 100, matching vanilla's own equivalent formula for other skills. This is Woodcutting's stand-in for \"speed\": more chops per stamina bar rather than a faster swing animation -- see WoodcuttingPatches.cs for why.");

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

            MiningStaminaEfficiencyWeight = Config.Bind(
                "Mining", "StaminaEfficiencyWeight", 0.33f,
                "How much Mining level reduces pickaxe stamina cost, at level 100 (max skill). Same mechanic and reasoning as Woodcutting's stand-in for \"speed\" -- see WoodcuttingPatches.cs.");

            MiningMilestoneLevel = Config.Bind(
                "Mining", "MilestoneLevel", 15,
                "Mining level that unlocks the milestone bonus -- shares Woodcutting's milestone level and shape (double XP + bonus yield), applied to ore instead of logs.");

            MiningMilestoneXpMultiplier = Config.Bind(
                "Mining", "MilestoneXpMultiplier", 2f,
                "Mining XP multiplier once the milestone level is reached. 2.0 = double XP.");

            MiningMilestoneOreYieldBonusPercent = Config.Bind(
                "Mining", "MilestoneOreYieldBonusPercent", 25f,
                "Extra ore/stone dropped per destroyed rock node once the milestone level is reached, as a percentage. 25 = +25% more.");
        }

        private void OnDestroy()
        {
            _harmony.UnpatchSelf();
        }
    }
}
