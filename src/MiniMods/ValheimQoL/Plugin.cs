using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace ValheimQoL
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class ValheimQoLPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.vikingadventure.qol";
        public const string PluginName = "ValheimQoL";
        public const string PluginVersion = "0.1.0";

        private readonly Harmony _harmony = new Harmony(PluginGUID);

        // ---- Config entries, grouped by feature ----
        public static ConfigEntry<float> MaterialWeightMultiplier;
        public static ConfigEntry<int> StackSizeMultiplier;
        public static ConfigEntry<float> StaminaDrainMultiplier;
        public static ConfigEntry<float> StaminaRegenMultiplier;
        public static ConfigEntry<float> BuildSnapTolerance;

        private void Awake()
        {
            BindConfig();

            _harmony.PatchAll();

            Logger.LogInfo($"{PluginName} {PluginVersion} loaded");
        }

        private void BindConfig()
        {
            MaterialWeightMultiplier = Config.Bind(
                "Inventory", "MaterialWeightMultiplier", 0.5f,
                "Multiplier applied to raw material weight. 0.5 = half weight. 1.0 = vanilla.");

            StackSizeMultiplier = Config.Bind(
                "Inventory", "StackSizeMultiplier", 2,
                "Multiplies vanilla max stack sizes. 2 = double stacks.");

            StaminaDrainMultiplier = Config.Bind(
                "Combat", "StaminaDrainMultiplier", 0.85f,
                "Multiplier on stamina cost for actions. Lower = less drain.");

            StaminaRegenMultiplier = Config.Bind(
                "Combat", "StaminaRegenMultiplier", 1.15f,
                "Multiplier on stamina regen rate. Higher = faster regen.");

            BuildSnapTolerance = Config.Bind(
                "Building", "SnapTolerance", 1.5f,
                "Multiplier on how forgiving snap-point placement is. 1.0 = vanilla.");
        }

        private void OnDestroy()
        {
            _harmony.UnpatchSelf();
        }
    }
}
