using System;
using System.Collections.Generic;

namespace ThunderFury.DevTool.Data
{
    // ---- Dungeon-exclusive bonus loot pool ----
    //
    // User's own framing (2026-09-10): a CHANCE, not a guarantee, so
    // getting the item is an incentive to run more dungeons, not a
    // one-and-done unlock. Deliberately NOT trying to inject into
    // vanilla's own per-room loot tables (see DungeonLootPatch.cs's
    // header comment for why that's impractical) -- this is an
    // independent bonus roll on top of whatever a dungeon container
    // already contains.
    [Serializable]
    public class DungeonLootEntry
    {
        public string ItemName = "";
        public float ChancePercent = 5f;
        public int MinAmount = 1;
        public int MaxAmount = 1;
    }

    [Serializable]
    public class DungeonLootPoolList
    {
        public List<DungeonLootEntry> Entries = new List<DungeonLootEntry>();
    }
}
