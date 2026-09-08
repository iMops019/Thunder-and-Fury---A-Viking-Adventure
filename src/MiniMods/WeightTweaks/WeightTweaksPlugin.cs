using BepInEx;
using Jotunn.Utils;

namespace VikingAdventure.WeightTweaks
{
    // QoL: config-adjustable carry weight per item (global multiplier +
    // optional per-item overrides), patched onto ItemDrop.ItemData weight
    // lookups at load. See docs/DESIGN.md#weighttweaks-mini-mod.
    // Simplest of the four systems — good first target to prove the
    // build-and-deploy pipeline once BepInEx/Jotunn are installed.
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class WeightTweaksPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.vikingadventure.weighttweaks";
        public const string PluginName = "VikingAdventure.WeightTweaks";
        public const string PluginVersion = "0.1.0";

        private void Awake()
        {
            Jotunn.Logger.LogInfo($"{PluginName} {PluginVersion} loaded");
        }
    }
}
