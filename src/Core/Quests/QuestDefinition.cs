using System;

namespace ThunderFury.Core.Quests
{
    // ---- Quest data model ----
    //
    // Moved here from the Quests mini-mod (2026-09-10) so both Quests
    // (runtime behavior: tracking, the Adventure Board panel) and DevTool
    // (the new Quest Creator tab) can share one definition without either
    // depending on the other -- Core is already a dependency of both.
    //
    // [Serializable] + field-only shape so UnityEngine.JsonUtility (DevTool's
    // persistence layer, no new dependency) can (de)serialize it directly,
    // same reasoning as every other DevTool-editable data class.
    //
    // Objective types kept deliberately small (user's call, 2026-09-10):
    // KillCreature, DiscoverBiome, OwnLegendaryItem -- no plain item-
    // delivery/fetch quest, matching the explicit "not just fetch-flavor"
    // direction from vision.md and this session both.
    public enum QuestObjectiveType
    {
        KillCreature,
        DiscoverBiome,
        OwnLegendaryItem,
    }

    [Serializable]
    public class QuestDefinition
    {
        public string Id = "";
        public string Title = "";
        public string Description = "";

        public QuestObjectiveType ObjectiveType;

        // KillCreature: vanilla creature prefab name. DiscoverBiome:
        // global::Heightmap.Biome member name. OwnLegendaryItem: unused.
        public string Target = "";
        public int TargetCount = 1;

        // Area-level gate (user's own term) -- the real, buildable
        // version of "level gate" available today. A dedicated Player
        // Level system doesn't exist yet (still undesigned, see
        // docs/PROGRESS.md's Classes & Passive Trees entry); biome
        // progression is vision.md's own established stand-in for pacing
        // elsewhere (e.g. Cooking's design), reused here rather than
        // inventing a new gate. Heightmap.Biome member name, or empty
        // for "no biome requirement."
        public string RequiredBiome = "";

        public string RewardItemName = "";
        public int RewardItemAmount;
        public string RewardSkillName = "";
        public int RewardSkillXp;

        // Chain: the next quest to unlock the moment this one completes.
        // Replaces the original "quest giver spawns next to you" idea --
        // with a static Adventure Board instead of a following NPC, the
        // next quest just becomes available at the same board, announced
        // with a message, rather than the giver physically relocating.
        public string NextQuestId = "";
    }
}
