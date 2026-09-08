using BepInEx;
using Jotunn.Utils;
using VikingAdventure.Core;

namespace VikingAdventure.RarityLoot
{
    // Magic/Rare/Legendary item tiers on top of vanilla items. See
    // docs/DESIGN.md#rarityloot-mini-mod for the open design questions
    // (tier list, what a rarity roll grants) that need answers before
    // this does anything beyond loading.
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [BepInDependency(CorePlugin.PluginGUID)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class RarityLootPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.vikingadventure.rarityloot";
        public const string PluginName = "VikingAdventure.RarityLoot";
        public const string PluginVersion = "0.1.0";

        private void Awake()
        {
            Jotunn.Logger.LogInfo($"{PluginName} {PluginVersion} loaded");
        }
    }
}
