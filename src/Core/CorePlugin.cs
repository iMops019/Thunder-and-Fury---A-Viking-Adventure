using BepInEx;
using Jotunn.Utils;

namespace VikingAdventure.Core
{
    // Shared framework mod: config, shared prefabs/data, and any cross-cutting
    // hooks the mini-mods below depend on. Mini-mods declare a
    // [BepInDependency(CorePlugin.PluginGUID)] on this and reference Core.dll.
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class CorePlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.vikingadventure.core";
        public const string PluginName = "VikingAdventure.Core";
        public const string PluginVersion = "0.1.0";

        private void Awake()
        {
            Jotunn.Logger.LogInfo($"{PluginName} {PluginVersion} loaded");
        }
    }
}
