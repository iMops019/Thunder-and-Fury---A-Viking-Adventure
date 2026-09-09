using BepInEx;
using Jotunn.Utils;
using ThunderFury.Core;

namespace ThunderFury.Quests
{
    // Objective-based quest system + quest giver NPC(s). See
    // docs/DESIGN.md#quests-mini-mod for the open design questions
    // (quest list, giver placement, reward structure) that need answers
    // before this does anything beyond loading.
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [BepInDependency(CorePlugin.PluginGUID)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class QuestsPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.ThunderFury.quests";
        public const string PluginName = "ThunderFury.Quests";
        public const string PluginVersion = "0.1.0";

        private void Awake()
        {
            Jotunn.Logger.LogInfo($"{PluginName} {PluginVersion} loaded");
        }
    }
}
