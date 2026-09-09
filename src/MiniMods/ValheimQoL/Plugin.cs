using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

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
        public static ConfigEntry<float> PlaceDistanceMultiplier;
        public static ConfigEntry<float> AutoPickupRangeMultiplier;
        public static ConfigEntry<KeyboardShortcut> SortInventoryKey;
        public static ConfigEntry<KeyboardShortcut>[] QuickSlotKeys;
        public static ConfigEntry<float> CraftFromContainersRadius;

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

            PlaceDistanceMultiplier = Config.Bind(
                "Building", "PlaceDistanceMultiplier", 1.5f,
                "Multiplier on how far from the player pieces can be placed. Vanilla max is 5m. 1.0 = vanilla.");

            AutoPickupRangeMultiplier = Config.Bind(
                "Inventory", "AutoPickupRangeMultiplier", 1.5f,
                "Multiplier on vanilla's auto-pickup sweep radius. Vanilla range is 2m. 1.0 = vanilla.");

            SortInventoryKey = Config.Bind(
                "Inventory", "SortInventoryKey", new KeyboardShortcut(KeyCode.S, KeyCode.LeftAlt),
                "Hotkey to sort/stack-merge the inventory grid. Only works while the inventory screen is open.");

            QuickSlotKeys = new[]
            {
                Config.Bind("QuickSlots", "Slot1Key", new KeyboardShortcut(KeyCode.F1),
                    "Use/equip whatever's assigned to quick slot 1. Hold Ctrl + this key while holding an item in the inventory screen to assign it."),
                Config.Bind("QuickSlots", "Slot2Key", new KeyboardShortcut(KeyCode.F2),
                    "Use/equip whatever's assigned to quick slot 2. Hold Ctrl + this key while holding an item in the inventory screen to assign it."),
                Config.Bind("QuickSlots", "Slot3Key", new KeyboardShortcut(KeyCode.F3),
                    "Use/equip whatever's assigned to quick slot 3. Hold Ctrl + this key while holding an item in the inventory screen to assign it."),
                Config.Bind("QuickSlots", "Slot4Key", new KeyboardShortcut(KeyCode.F4),
                    "Use/equip whatever's assigned to quick slot 4. Hold Ctrl + this key while holding an item in the inventory screen to assign it."),
            };

            CraftFromContainersRadius = Config.Bind(
                "Crafting", "CraftFromContainersRadius", 10f,
                "Radius (meters) around the player to pull crafting/building materials from nearby chests. 0 disables.");
        }

        private void OnDestroy()
        {
            _harmony.UnpatchSelf();
        }
    }
}
