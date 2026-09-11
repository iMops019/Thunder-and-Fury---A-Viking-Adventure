using System.Collections.Generic;

namespace ThunderFury.Core.Loot
{
    // ---- Which creatures are allowed to source a Legendary-tier ambient drop ----
    //
    // Same "dumb registry" shape as SkillRegistry/QuestRegistry: Core just
    // holds the shared set, keyed by creature prefab name (works for
    // vanilla creatures and any DevTool-made one alike, no separate case
    // needed for either). DevTool owns persistence + the editor field;
    // RarityLoot's ambient biome-drop roll (2026-09-10, user's own request
    // to gate which creatures can drop legendaries) is the only reader.
    public static class LegendaryDropSourceRegistry
    {
        public static readonly HashSet<string> Eligible = new HashSet<string>();

        public static bool IsEligible(string creaturePrefabName)
        {
            return creaturePrefabName != null && Eligible.Contains(creaturePrefabName);
        }
    }
}
