using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Managers;
using Jotunn.Utils;
using VikingAdventure.Core.Patches;
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
        public static ConfigEntry<int> WoodcuttingMilestoneLevel;
        public static ConfigEntry<float> WoodcuttingMilestoneXpMultiplier;
        public static ConfigEntry<float> WoodcuttingMilestoneLogYieldBonusPercent;

        public static ConfigEntry<float> MiningDamagePerLevel;
        public static ConfigEntry<int> MiningMilestoneLevel;
        public static ConfigEntry<float> MiningMilestoneXpMultiplier;
        public static ConfigEntry<float> MiningMilestoneOreYieldBonusPercent;

        public static ConfigEntry<float> FishingBiteChanceBonusAtMaxLevel;

        public static ConfigEntry<float> SkinningCarcassVisualRotationX;
        public static ConfigEntry<float> SkinningCarcassVisualScale;

        public static ConfigEntry<float> CookingBurnPreventionChanceAtMaxLevel;
        public static ConfigEntry<float> CookingBurnPreventionRadius;

        private void Awake()
        {
            BindConfig();

            _harmony.PatchAll();

            PrefabManager.OnVanillaPrefabsAvailable += WoodcuttingSkill.Register;
            PrefabManager.OnVanillaPrefabsAvailable += MiningSkill.Register;
            PrefabManager.OnVanillaPrefabsAvailable += FishingSkill.Register;
            PrefabManager.OnVanillaPrefabsAvailable += SkinningSkill.Register;
            PrefabManager.OnVanillaPrefabsAvailable += SkinningSystem.RegisterDefaults;
            PrefabManager.OnVanillaPrefabsAvailable += SmithingSkill.Register;
            PrefabManager.OnVanillaPrefabsAvailable += CookingSkill.Register;
            PrefabManager.OnVanillaPrefabsAvailable += FletchingSkill.Register;
            PrefabManager.OnVanillaPrefabsAvailable += BuildingSkill.Register;

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

            SkinningCarcassVisualRotationX = Config.Bind(
                "Skinning", "CarcassVisualRotationX", 90f,
                "X-axis rotation (degrees) applied to a carcass piece's reused creature mesh. Needs live tuning once actually seen in-game -- the mesh renders in its rigged bind pose, not a real death pose, so this is a best-guess starting point for making it read as 'lying down' rather than 'standing.'");

            SkinningCarcassVisualScale = Config.Bind(
                "Skinning", "CarcassVisualScale", 1f,
                "Uniform scale applied to a carcass piece's reused creature mesh. Needs live tuning once actually seen in-game.");

            CookingBurnPreventionChanceAtMaxLevel = Config.Bind(
                "Cooking", "BurnPreventionChanceAtMaxLevel", 0.75f,
                "Chance to save food that would otherwise burn, at level 100 (max skill). 0.75 = 75% chance at level 100, scaling smoothly from 0 at level 0.");

            CookingBurnPreventionRadius = Config.Bind(
                "Cooking", "BurnPreventionRadius", 10f,
                "Radius (meters) around a cooking station to find the player whose Cooking level applies -- there's no per-slot ownership tracked by vanilla, so this uses the closest player, same approximation Fishing uses for bite chance.");
        }

        private void OnDestroy()
        {
            _harmony.UnpatchSelf();
        }
    }
}
