using BepInEx;
using BepInEx.Configuration;
using Jotunn.Managers;
using Jotunn.Utils;
using VikingAdventure.Core;
using VikingAdventure.RarityLoot.Items;

namespace VikingAdventure.RarityLoot
{
    // Magic/Rare/Legendary item tiers on top of vanilla items. The tier
    // list and roll mechanics are still undesigned (see
    // docs/PROGRESS.md#pillar-3-gear--materials) and block most of this
    // mini-mod, but the Stone Pickaxe (docs/valheim-mod-vision.md's first
    // planned Pillar 3 item) has a locked-in design and no dependency on
    // any of that, so it's wired in on its own.
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [BepInDependency(CorePlugin.PluginGUID)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class RarityLootPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.vikingadventure.rarityloot";
        public const string PluginName = "VikingAdventure.RarityLoot";
        public const string PluginVersion = "0.1.0";

        public static ConfigEntry<float> StonePickaxeDamageMultiplier;
        public static ConfigEntry<int> StonePickaxeWoodCost;
        public static ConfigEntry<int> StonePickaxeStoneCost;

        private void Awake()
        {
            BindConfig();

            PrefabManager.OnVanillaPrefabsAvailable += StonePickaxe.Register;

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
        }
    }
}
