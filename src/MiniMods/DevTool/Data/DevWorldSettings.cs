using System;

namespace ThunderFury.DevTool.Data
{
    // ---- World/Areas tab's persisted values ----
    //
    // Confirmed against the real 1.0 decompile while wiring up
    // persistence: Game.m_worldLevel and its multiplier fields are read
    // ONCE at world start from a world-creation-time "modifiers" store
    // (Game's own globalKeysValues, populated from the world's chosen
    // difficulty preset) -- NOT a normal runtime-mutable save key the way
    // boss-kill flags are. Writing a live override back into that store
    // would mean reaching into Valheim's own world-save format directly,
    // real risk of corrupting a save for a "let me tune this while
    // testing" feature. Sidestepped entirely: Dev Tool remembers the last
    // values YOU set in its own JSON (same pattern as items/recipes/
    // skills) and silently reapplies them on every game start via a
    // Game.Start Postfix -- same practical effect (your tuned difficulty
    // sticks around) without touching Valheim's save data at all.
    //
    // Present = has this field been edited via the tab at least once. A
    // fresh install has no file and Apply() is a no-op, so vanilla
    // worlds are completely unaffected until the user actually opens the
    // World tab and changes something.
    [Serializable]
    public class DevWorldSettings
    {
        public bool Present;

        public int WorldLevel;
        public int EnemyBaseAC;
        public float EnemyHPMultiplier;
        public int EnemyBaseDamage;
        public float EnemyMoveSpeedMultiplier;
        public float EnemyLevelUpExponent;
        public int GearBaseAC;
        public int GearBaseDamage;
        public int PieceBaseDamage;
        public float PieceHPMultiplier;
        public float MineHPMultiplier;
    }
}
