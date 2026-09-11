using System.IO;
using UnityEngine;

namespace ThunderFury.DevTool.Data
{
    public static class DevWorldSettingsRegistry
    {
        public static DevWorldSettings Current = new DevWorldSettings();

        static string FilePath => Path.Combine(DevToolPaths.DataDirectory, "world.json");

        public static void Load()
        {
            DevToolPaths.EnsureDataDirectory();
            if (!File.Exists(FilePath))
            {
                Current = new DevWorldSettings();
                return;
            }

            string json = File.ReadAllText(FilePath);
            Current = JsonUtility.FromJson<DevWorldSettings>(json) ?? new DevWorldSettings();
        }

        public static void Save()
        {
            DevToolPaths.EnsureDataDirectory();
            Current.Present = true;
            File.WriteAllText(FilePath, JsonUtility.ToJson(Current, true));
        }

        // Called on every Game.Start -- a no-op until the user has saved
        // at least once via the World tab (Current.Present stays false on
        // a fresh install, so vanilla/undecided worlds are untouched).
        public static void Apply()
        {
            Load();
            if (!Current.Present) return;
            if (Game.instance == null) return;

            Game.m_worldLevel = Mathf.Clamp(Current.WorldLevel, 0, 10);
            Game.instance.m_worldLevelEnemyBaseAC = Current.EnemyBaseAC;
            Game.instance.m_worldLevelEnemyHPMultiplier = Current.EnemyHPMultiplier;
            Game.instance.m_worldLevelEnemyBaseDamage = Current.EnemyBaseDamage;
            Game.instance.m_worldLevelEnemyMoveSpeedMultiplier = Current.EnemyMoveSpeedMultiplier;
            Game.instance.m_worldLevelEnemyLevelUpExponent = Current.EnemyLevelUpExponent;
            Game.instance.m_worldLevelGearBaseAC = Current.GearBaseAC;
            Game.instance.m_worldLevelGearBaseDamage = Current.GearBaseDamage;
            Game.instance.m_worldLevelPieceBaseDamage = Current.PieceBaseDamage;
            Game.instance.m_worldLevelPieceHPMultiplier = Current.PieceHPMultiplier;
            Game.instance.m_worldLevelMineHPMultiplier = Current.MineHPMultiplier;

            Jotunn.Logger.LogInfo("DevTool: reapplied saved World/Area settings for this session.");
        }

        // Snapshots the CURRENT live Game values into Current before the
        // World tab's fields are first drawn, so editing one field
        // doesn't silently reset every other field back to a stale
        // previously-saved value (or 0) the next time Save() runs.
        public static void SyncFromLiveGame()
        {
            if (Game.instance == null) return;

            Current.WorldLevel = Game.m_worldLevel;
            Current.EnemyBaseAC = Game.instance.m_worldLevelEnemyBaseAC;
            Current.EnemyHPMultiplier = Game.instance.m_worldLevelEnemyHPMultiplier;
            Current.EnemyBaseDamage = Game.instance.m_worldLevelEnemyBaseDamage;
            Current.EnemyMoveSpeedMultiplier = Game.instance.m_worldLevelEnemyMoveSpeedMultiplier;
            Current.EnemyLevelUpExponent = Game.instance.m_worldLevelEnemyLevelUpExponent;
            Current.GearBaseAC = Game.instance.m_worldLevelGearBaseAC;
            Current.GearBaseDamage = Game.instance.m_worldLevelGearBaseDamage;
            Current.PieceBaseDamage = Game.instance.m_worldLevelPieceBaseDamage;
            Current.PieceHPMultiplier = Game.instance.m_worldLevelPieceHPMultiplier;
            Current.MineHPMultiplier = Game.instance.m_worldLevelMineHPMultiplier;
        }
    }
}
