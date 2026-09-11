using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;
using ThunderFury.Core;
using ThunderFury.DevTool.Data;
using ThunderFury.DevTool.UI;

namespace ThunderFury.DevTool
{
    // ---- Dev Tool: in-game content editor overlay ----
    //
    // What this is for (2026-09-10 session): a single in-game panel,
    // toggled by a hotkey, that gives real editing control over the mod
    // without a recompile -- new items, new recipes, new skills built from
    // Core's already-proven generic mechanics, world/area difficulty
    // knobs, and every mod's tunable Config value in one place. Built on
    // Jotunn's GUIManager (Valheim-style wood panels/buttons/scroll views
    // -- confirmed by decompiling Jotunn.dll directly, this is the real
    // toolkit Jotunn mods use for in-game UI, not hand-rolled Unity UI)
    // rather than a separate desktop app, so it fits the same
    // tweak-save-test loop as actually playing.
    //
    // Deliberately built first, ahead of Quests and RarityLoot's ordinary-
    // gear tier list (user's call), specifically so those get wired into
    // this overlay as they're built rather than bolted on after.
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [BepInDependency(CorePlugin.PluginGUID)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class DevToolPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.ThunderFury.devtool";
        public const string PluginName = "ThunderFury.DevTool";
        public const string PluginVersion = "0.1.0";

        private readonly Harmony _harmony = new Harmony(PluginGUID);

        public static ConfigEntry<KeyboardShortcut> ToggleKey;

        private void Awake()
        {
            ToggleKey = Config.Bind(
                "General", "ToggleKey", new KeyboardShortcut(KeyCode.F9),
                "Opens/closes the Dev Tool overlay panel.");

            // Only patch needed so far: reapplying saved World/Areas
            // settings on Game.Start (see Patches/WorldSettingsReapplyPatch.cs).
            _harmony.PatchAll();

            GUIManager.OnCustomGUIAvailable += DevToolOverlay.OnCustomGUIAvailable;

            // Same hook every mini-mod's own content uses (Core's skills,
            // RarityLoot's items) -- fires once vanilla prefabs are
            // available to clone from. Subscribed here, after Core's own
            // Awake already subscribed its skills' Register() calls to
            // the same event (BepInDependency guarantees Core's Awake ran
            // first), so by the time this actually fires every skill's
            // Type is already populated for anything that needs it.
            PrefabManager.OnVanillaPrefabsAvailable += DevSkillRegistry.LoadAndRegisterAll;
            PrefabManager.OnVanillaPrefabsAvailable += DevItemRegistry.LoadAndRegisterAll;
            PrefabManager.OnVanillaPrefabsAvailable += DevPieceRegistry.LoadAndRegisterAll;
            PrefabManager.OnVanillaPrefabsAvailable += DevCreatureRegistry.LoadAndRegisterAll;
            PrefabManager.OnVanillaPrefabsAvailable += DevRecipeRegistry.LoadAndRegisterAll;
            PrefabManager.OnVanillaPrefabsAvailable += CreatureDropRegistry.LoadAndApplyAll;
            PrefabManager.OnVanillaPrefabsAvailable += DungeonLootRegistry.Load;
            PrefabManager.OnVanillaPrefabsAvailable += DevQuestRegistry.LoadAndApplyAll;
            PrefabManager.OnVanillaPrefabsAvailable += DevLegendaryDropSourceRegistry.LoadAndApplyAll;

            Jotunn.Logger.LogInfo($"{PluginName} {PluginVersion} loaded");
        }

        private void Update()
        {
            if (ToggleKey.Value.IsDown())
            {
                DevToolOverlay.Toggle();
            }
        }

        private void OnDestroy()
        {
            _harmony.UnpatchSelf();
        }
    }
}
