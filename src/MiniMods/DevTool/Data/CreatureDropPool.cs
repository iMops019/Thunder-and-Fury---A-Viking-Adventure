using System;
using System.Collections.Generic;

namespace ThunderFury.DevTool.Data
{
    // ---- Answers "where does this item drop from" ----
    //
    // The gap this closes (user's own framing, 2026-09-10): the Item
    // Creator only ever handled crafting -- nothing let an item be a
    // creature/boss drop instead of (or alongside) a recipe. This is a
    // genuinely separate vanilla mechanism (Character.CharacterDrop,
    // confirmed via decompile -- the same real loot-table every creature
    // in the game already uses for its own meat/trophies/materials), not
    // something the Recipe Creator could have covered.
    [Serializable]
    public class CreatureDropEntry
    {
        public string CreatureName = "";
        public string ItemName = "";
        public float ChancePercent = 100f;
        public int MinAmount = 1;
        public int MaxAmount = 1;
        public bool OnePerPlayer;
    }

    [Serializable]
    public class CreatureDropPoolList
    {
        public List<CreatureDropEntry> Entries = new List<CreatureDropEntry>();
    }
}
