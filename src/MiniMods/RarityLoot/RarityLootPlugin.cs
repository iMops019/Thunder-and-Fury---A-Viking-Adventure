using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Managers;
using Jotunn.Utils;
using ThunderFury.Core;
using ThunderFury.RarityLoot.Items;

namespace ThunderFury.RarityLoot
{
    // Magic/Rare/Legendary item tiers on top of vanilla items. The
    // general tier list and roll mechanics for ordinary vanilla gear are
    // still undesigned (see docs/PROGRESS.md#pillar-3-gear--materials)
    // and block most of this mini-mod, but two things don't depend on
    // that: the Stone Pickaxe (a standalone item), and the Legendary
    // named-hero pattern (Voltun's Set) -- built generic on purpose so a
    // second named set later is just data, not new code. Recommend
    // installing BepInEx.ConfigurationManager alongside this mod: every
    // config entry below (materials, amounts, multipliers) shows up in
    // its in-game F1 menu, so tuning Legendary recipes doesn't need a
    // recompile.
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [BepInDependency(CorePlugin.PluginGUID)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class RarityLootPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.ThunderFury.rarityloot";
        public const string PluginName = "ThunderFury.RarityLoot";
        public const string PluginVersion = "0.1.0";

        private readonly Harmony _harmony = new Harmony(PluginGUID);

        public static ConfigEntry<float> StonePickaxeDamageMultiplier;
        public static ConfigEntry<int> StonePickaxeWoodCost;
        public static ConfigEntry<int> StonePickaxeStoneCost;

        public static ConfigEntry<int> LegendaryAffixCount;

        public static ConfigEntry<float> OrdinaryMagicChance;
        public static ConfigEntry<float> OrdinaryRareChance;
        public static ConfigEntry<int> OrdinaryMagicAffixCount;
        public static ConfigEntry<int> OrdinaryRareAffixCount;

        public static ConfigEntry<float> AmbientDropChance;
        public static ConfigEntry<float> AmbientLegendaryShare;
        public static ConfigEntry<float> AmbientRareShare;

        public static ConfigEntry<float> VoltunDamageMultiplier;
        public static ConfigEntry<float> VoltunSpeedMultiplier;
        public static ConfigEntry<float> VoltunLogYieldBonusPercent;
        public static ConfigEntry<int> VoltunHatchetWoodCost;
        public static ConfigEntry<int> VoltunHatchetCopperCost;
        public static ConfigEntry<int> VoltunHatchetBronzeCost;
        public static ConfigEntry<int> VoltunPickaxeWoodCost;
        public static ConfigEntry<int> VoltunPickaxeCopperCost;
        public static ConfigEntry<int> VoltunPickaxeBronzeCost;

        public static ConfigEntry<int> SkinningKnifeWoodCost;
        public static ConfigEntry<int> SkinningKnifeFlintCost;

        public static ConfigEntry<int> SmithingLegendaryCraftLevel;

        public static ConfigEntry<float> LightningSwordBonusDamage;
        public static ConfigEntry<float> LightningSwordDropChance;

        private void Awake()
        {
            BindConfig();
            LegendaryWeaponsBatch.BindConfig(Config);
            LegendaryArmorBatch.BindConfig(Config);
            LegendaryWeaponsBlackForest.BindConfig(Config);
            LegendaryArmorBlackForest.BindConfig(Config);
            LegendaryWeaponsSwamp.BindConfig(Config);
            LegendaryArmorSwamp.BindConfig(Config);
            LegendaryWeaponsMountain.BindConfig(Config);
            LegendaryArmorMountain.BindConfig(Config);
            LegendaryWeaponsPlains.BindConfig(Config);
            LegendaryArmorPlains.BindConfig(Config);
            LegendaryWeaponsMistlands.BindConfig(Config);
            LegendaryArmorMistlands.BindConfig(Config);
            LegendaryWeaponsAshlands.BindConfig(Config);
            LegendaryArmorAshlands.BindConfig(Config);
            LegendaryWeaponsDeepNorth.BindConfig(Config);
            LegendaryArmorDeepNorth.BindConfig(Config);

            _harmony.PatchAll();

            PrefabManager.OnVanillaPrefabsAvailable += StonePickaxe.Register;
            PrefabManager.OnVanillaPrefabsAvailable += VoltunsSet.Register;
            PrefabManager.OnVanillaPrefabsAvailable += SkinningKnife.Register;
            PrefabManager.OnVanillaPrefabsAvailable += LightningSword.Register;
            PrefabManager.OnVanillaPrefabsAvailable += LegendaryWeaponsBatch.Register;
            PrefabManager.OnVanillaPrefabsAvailable += LegendaryArmorBatch.Register;
            PrefabManager.OnVanillaPrefabsAvailable += LegendaryWeaponsBlackForest.Register;
            PrefabManager.OnVanillaPrefabsAvailable += LegendaryArmorBlackForest.Register;
            PrefabManager.OnVanillaPrefabsAvailable += LegendaryWeaponsSwamp.Register;
            PrefabManager.OnVanillaPrefabsAvailable += LegendaryArmorSwamp.Register;
            PrefabManager.OnVanillaPrefabsAvailable += LegendaryWeaponsMountain.Register;
            PrefabManager.OnVanillaPrefabsAvailable += LegendaryArmorMountain.Register;
            PrefabManager.OnVanillaPrefabsAvailable += LegendaryWeaponsPlains.Register;
            PrefabManager.OnVanillaPrefabsAvailable += LegendaryArmorPlains.Register;
            PrefabManager.OnVanillaPrefabsAvailable += LegendaryWeaponsMistlands.Register;
            PrefabManager.OnVanillaPrefabsAvailable += LegendaryArmorMistlands.Register;
            PrefabManager.OnVanillaPrefabsAvailable += LegendaryWeaponsAshlands.Register;
            PrefabManager.OnVanillaPrefabsAvailable += LegendaryArmorAshlands.Register;
            PrefabManager.OnVanillaPrefabsAvailable += LegendaryWeaponsDeepNorth.Register;
            PrefabManager.OnVanillaPrefabsAvailable += LegendaryArmorDeepNorth.Register;

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

            LegendaryAffixCount = Config.Bind(
                "RarityLoot", "LegendaryAffixCount", 2,
                "How many rolled affixes a Legendary item gets from the shared affix pool, on top of its fixed identity bonuses.");

            OrdinaryRareChance = Config.Bind(
                "RarityLoot", "OrdinaryRareChance", 0.015f,
                "Chance for ANY vanilla weapon or armor piece (crafted or looted -- vision.md left this fully open, this is a tunable starting guess, not a locked number) to roll Rare. 0.015 = 1.5%. Checked before Magic so its odds aren't shadowed.");

            OrdinaryMagicChance = Config.Bind(
                "RarityLoot", "OrdinaryMagicChance", 0.08f,
                "Chance for any vanilla weapon or armor piece to roll Magic, checked after Rare. 0.08 = 8%.");

            OrdinaryRareAffixCount = Config.Bind(
                "RarityLoot", "OrdinaryRareAffixCount", 2,
                "Rolled affixes on an ordinary item that rolled Rare.");

            OrdinaryMagicAffixCount = Config.Bind(
                "RarityLoot", "OrdinaryMagicAffixCount", 1,
                "Rolled affixes on an ordinary item that rolled Magic.");

            AmbientDropChance = Config.Bind(
                "RarityLoot", "AmbientDropChance", 0.03f,
                "Biome-aware ambient drop system (2026-09-10 redesign): chance PER KILL that anything at all drops from this mechanic. " +
                "Deliberately separate from OrdinaryRare/MagicChance above and from every creature's own normal loot table (trophies, meat, " +
                "materials) -- those are completely unaffected and uncapped. This roll, when it succeeds, spawns at most ONE extra weapon or " +
                "armor piece near the kill, by design (user's own words: 'not a POE loot explosion').");

            AmbientLegendaryShare = Config.Bind(
                "RarityLoot", "AmbientLegendaryShare", 0.02f,
                "Of the kills that pass AmbientDropChance, the odds the drop is Legendary tier instead of Rare/Magic -- only reachable at all " +
                "if the dying creature is on the DevTool Creature editor's 'Legendary Drop Sources' list. For any other creature this share " +
                "silently folds into the Rare odds below instead of being lost.");

            AmbientRareShare = Config.Bind(
                "RarityLoot", "AmbientRareShare", 0.18f,
                "Of the kills that pass AmbientDropChance, the odds the drop is Rare tier (checked after Legendary). Everything else that " +
                "passes AmbientDropChance rolls Magic.");

            VoltunDamageMultiplier = Config.Bind(
                "VoltunsSet", "DamageMultiplier", 1.5f,
                "Voltun's items' chop/mining damage relative to the vanilla item they're based on. 1.0 = same as vanilla.");

            VoltunSpeedMultiplier = Config.Bind(
                "VoltunsSet", "SpeedMultiplier", 1.2f,
                "Voltun's items' swing speed factor relative to the vanilla item they're based on. 1.0 = same as vanilla.");

            VoltunLogYieldBonusPercent = Config.Bind(
                "VoltunsSet", "HatchetLogYieldBonusPercent", 50f,
                "Extra Wood dropped per chopped log while wielding Voltun's Hatchet, as a percentage. 50 = +50% more Wood.");

            VoltunHatchetWoodCost = Config.Bind(
                "VoltunsSet", "HatchetWoodCost", 20, "Wood required to craft Voltun's Hatchet.");
            VoltunHatchetCopperCost = Config.Bind(
                "VoltunsSet", "HatchetCopperCost", 10, "Copper required to craft Voltun's Hatchet.");
            VoltunHatchetBronzeCost = Config.Bind(
                "VoltunsSet", "HatchetBronzeCost", 5, "Bronze required to craft Voltun's Hatchet.");

            VoltunPickaxeWoodCost = Config.Bind(
                "VoltunsSet", "PickaxeWoodCost", 15, "Wood required to craft Voltun's Pickaxe.");
            VoltunPickaxeCopperCost = Config.Bind(
                "VoltunsSet", "PickaxeCopperCost", 15, "Copper required to craft Voltun's Pickaxe.");
            VoltunPickaxeBronzeCost = Config.Bind(
                "VoltunsSet", "PickaxeBronzeCost", 5, "Bronze required to craft Voltun's Pickaxe.");

            SkinningKnifeWoodCost = Config.Bind(
                "SkinningKnife", "WoodCost", 3, "Wood required to craft a Skinning Knife.");
            SkinningKnifeFlintCost = Config.Bind(
                "SkinningKnife", "FlintCost", 3, "Flint required to craft a Skinning Knife.");

            SmithingLegendaryCraftLevel = Config.Bind(
                "RarityLoot", "SmithingLegendaryCraftLevel", 30,
                "Smithing level required to craft a Legendary item (vision.md: a deliberate hard gate on the top gear tier). Exact level wasn't decided in the design doc, so this is a tunable default.");

            LightningSwordBonusDamage = Config.Bind(
                "LightningSword", "BonusLightningDamage", 6f,
                "Flat lightning damage added on top of the base SwordBronze's own physical damage. Cut from an original 15 (2026-09-10, " +
                "confirmed in-game as one-shotting everything with zero gear) -- SwordBronze is already a Black Forest-tier weapon found as " +
                "a Meadows drop, so this bonus doesn't need to be large on top of that to still feel special. Also stacks with a rolled " +
                "Legendary affix (up to +20% total damage, see Affixes/AffixPool.cs's DamageAffixId Min/Max) -- both are independently " +
                "live-tunable without a rebuild if it still feels off.");

            LightningSwordDropChance = Config.Bind(
                "LightningSword", "DropChance", 0.01f,
                "Chance per kill for Boar or Neck to drop a Lightning Sword. 0.01 = 1%. Not craftable by design -- this is the only way to get one.");
        }

        private void OnDestroy()
        {
            _harmony.UnpatchSelf();
        }
    }
}
