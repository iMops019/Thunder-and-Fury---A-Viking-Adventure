using System.Collections.Generic;
using System.Linq;

namespace ThunderFury.Core.Quests
{
    // ---- The live, shared quest list ----
    //
    // Deliberately just a blackboard -- no file I/O, no default content,
    // no game logic. Quests' own runtime code (tracking patches, the
    // Adventure Board panel) reads this; DevTool's Quest Creator tab
    // writes to it (via its own JSON persistence layer, same pattern as
    // every other DevTool-editable list). Keeping Core itself "dumb" here
    // matches how SkillRegistry/RecipeLevelGate already work -- Core
    // hosts the shared mechanism, mini-mods own the actual behavior/data
    // lifecycle.
    public static class QuestRegistry
    {
        public static readonly List<QuestDefinition> All = new List<QuestDefinition>();

        public static QuestDefinition Get(string id) => All.FirstOrDefault(q => q.Id == id);
    }
}
