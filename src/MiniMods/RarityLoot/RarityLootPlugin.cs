using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Managers;
using Jotunn.Utils;
using VikingAdventure.Core;
using VikingAdventure.RarityLoot.Items;

namespace VikingAdventure.RarityLoot
{
    // Magic/Rare/Legendary item tiers on top of vanilla items. The
    // general tier list and roll mechanics for ordinary vanilla gear are
    // still undesigned (see docs/PROGRESS.md#pillar-3-gear--materials)
    // and block most of this mini-mod, but two things don't depend on
    // that: the Stone Pickaxe (a standalone item), and the Legendary
    // named-hero pattern (Voltun's Set) -- built generic on purpose so a
    // second named set later is just data, not new code. Recommend
    // installing BepInEx.ConfigurationManager alongside this mod: every
    // config entry below (materials, amounts, multipliers) shows up in
    // its in-game F1 menu, so tuning Legendary recipes doesn't need a
    // recompile.
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [BepInDependency(CorePlugin.PluginGUID)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class RarityLootPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.vikingadventure.rarityloot";
        public const string PluginName = "VikingAdventure.RarityLoot";
        public const string PluginVersion = "0.1.0";

        private readonly Harmony _harmony = new Harmony(PluginGUID);

        public static ConfigEntry<float> StonePickaxeDamageMultiplier;
        public static ConfigEntry<int> StonePickaxeWoodCost;
        public static ConfigEntry<int> StonePickaxeStoneCost;

        public static ConfigEntry<int> LegendaryAffixCount;

        public static ConfigEntry<float> VoltunDamageMultiplier;
        public static ConfigEntry<float> VoltunSpeedMultiplier;
        public static ConfigEntry<int> VoltunHatchetWoodCost;
        public static ConfigEntry<int> VoltunHatchetCopperCost;
        public static ConfigEntry<int> VoltunHatchetBronzeCost;
        public static ConfigEntry<int> VoltunPickaxeWoodCost;
        public static ConfigEntry<int> VoltunPickaxeCopperCost;
        public static ConfigEntry<int> VoltunPickaxeBronzeCost;

        private void Awake()
        {
            BindConfig();

            _harmony.PatchAll();

            PrefabManager.OnVanillaPrefabsAvailable += StonePickaxe.Register;
            PrefabManager.OnVanillaPrefabsAvailable += VoltunsSet.Register;

            Jotunn.Logger.LogInfo($"{PluginName} {PluginVersion} loaded");
        }

        private void BindConfig()
        {
            StonePickaxeDamageMultiplier = Config.Bind(
                "StonePickaxe", "DamageMultiplier", 0.6f,
                "Mining damage relative to the vanilla Antler Pickaxe. 1.0 = same as Antler, lower = weaker/slower.");

            StonePickaxeWoodCost = Config.Bind(
                "StonePickaxe", "WoodCost", 5, "Wood required to craft a Stone Pickaxe.");

            StonePickaxeStoneCost = Config.Bind(
                "StonePickaxe", "StoneCost", 10, "Stone required to craft a Stone Pickaxe.");

            LegendaryAffixCount = Config.Bind(
                "RarityLoot", "LegendaryAffixCount", 2,
                "How many rolled affixes a Legendary item gets from the shared affix pool, on top of its fixed identity bonuses.");

            VoltunDamageMultiplier = Config.Bind(
                "VoltunsSet", "DamageMultiplier", 1.5f,
                "Voltun's items' chop/mining damage relative to the vanilla item they're based on. 1.0 = same as vanilla.");

            VoltunSpeedMultiplier = Config.Bind(
                "VoltunsSet", "SpeedMultiplier", 1.2f,
                "Voltun's items' swing speed factor relative to the vanilla item they're based on. 1.0 = same as vanilla.");

            VoltunHatchetWoodCost = Config.Bind(
                "VoltunsSet", "HatchetWoodCost", 20, "Wood required to craft Voltun's Hatchet.");
            VoltunHatchetCopperCost = Config.Bind(
                "VoltunsSet", "HatchetCopperCost", 10, "Copper required to craft Voltun's Hatchet.");
            VoltunHatchetBronzeCost = Config.Bind(
                "VoltunsSet", "HatchetBronzeCost", 5, "Bronze required to craft Voltun's Hatchet.");

            VoltunPickaxeWoodCost = Config.Bind(
                "VoltunsSet", "PickaxeWoodCost", 15, "Wood required to craft Voltun's Pickaxe.");
            VoltunPickaxeCopperCost = Config.Bind(
                "VoltunsSet", "PickaxeCopperCost", 15, "Copper required to craft Voltun's Pickaxe.");
            VoltunPickaxeBronzeCost = Config.Bind(
                "VoltunsSet", "PickaxeBronzeCost", 5, "Bronze required to craft Voltun's Pickaxe.");
        }

        private void OnDestroy()
        {
            _harmony.UnpatchSelf();
        }
    }
}
