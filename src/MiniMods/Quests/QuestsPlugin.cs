using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Managers;
using Jotunn.Utils;
using ThunderFury.Core;
using ThunderFury.RarityLoot;

namespace ThunderFury.Quests
{
    // ---- Quests: Adventure Board + the first quest chain ----
    //
    // First real content in this mini-mod (was a pure stub before
    // 2026-09-10). Design settled this session: a buildable Adventure
    // Board (see AdventureBoard.cs) instead of a quest-giver NPC --
    // avoids the real technical/visual risk of stripping AI off a boss
    // creature to make it "friendly," while still giving the chain a
    // physical, built-by-the-player anchor point. Objective types kept to
    // three (KillCreature, DiscoverBiome, OwnLegendaryItem) -- no plain
    // fetch/delivery quest, matching the explicit "not just fetch-flavor"
    // direction from vision.md and this session both.
    //
    // Hard-depends on RarityLoot (first cross-mini-mod dependency in this
    // codebase -- see Quests.csproj's comment) because the "Prove Your
    // Legend" quest needs RarityLoot's own Legendary-tier concept.
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [BepInDependency(CorePlugin.PluginGUID)]
    [BepInDependency(RarityLootPlugin.PluginGUID)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class QuestsPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.ThunderFury.quests";
        public const string PluginName = "ThunderFury.Quests";
        public const string PluginVersion = "0.1.0";

        private readonly Harmony _harmony = new Harmony(PluginGUID);

        public static ConfigEntry<int> AdventureBoardWoodCost;

        private void Awake()
        {
            BindConfig();

            _harmony.PatchAll();

            PrefabManager.OnVanillaPrefabsAvailable += AdventureBoard.Register;

            Jotunn.Logger.LogInfo($"{PluginName} {PluginVersion} loaded");
        }

        private void BindConfig()
        {
            AdventureBoardWoodCost = Config.Bind(
                "AdventureBoard", "WoodCost", 2,
                "Wood required to build an Adventure Board at the Hammer.");
        }

        private void OnDestroy()
        {
            _harmony.UnpatchSelf();
        }
    }
}
