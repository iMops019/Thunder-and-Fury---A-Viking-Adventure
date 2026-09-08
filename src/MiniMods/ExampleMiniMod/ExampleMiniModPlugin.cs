using BepInEx;
using Jotunn.Utils;
using VikingAdventure.Core;

namespace VikingAdventure.ExampleMiniMod
{
    // Template for a mini-mod: depends on Core, ships one focused feature.
    // Copy this project folder to start a new mini-mod.
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [BepInDependency(CorePlugin.PluginGUID)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class ExampleMiniModPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.vikingadventure.exampleminimod";
        public const string PluginName = "VikingAdventure.ExampleMiniMod";
        public const string PluginVersion = "0.1.0";

        private void Awake()
        {
            Jotunn.Logger.LogInfo($"{PluginName} {PluginVersion} loaded");
        }
    }
}
