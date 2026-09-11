using System;
using System.Collections.Generic;

namespace ThunderFury.DevTool.Data
{
    // ---- Skill Creator's data model ----
    //
    // Deliberately narrow, matching the ceiling locked in for this pillar
    // (docs/PROGRESS.md): only composes mechanics Core has already proven
    // fully generic. Today that's exactly one thing -- redirecting a
    // vanilla skill's XP into a brand new custom skill via
    // SkillXpRedirect, the same mechanism every one of the 12 built-in
    // skills already uses. Station-based crafting XP and
    // Woodcutting/Mining-shaped damage-per-level scaling are NOT offered
    // here yet -- both need one more round of Core generalization first
    // (shared stations need per-recipe interception the way Fletching's
    // redirect does, not a blunt CraftingStation.m_craftingSkill
    // overwrite; the damage patches are hardcoded to one skill each
    // today). Flagged rather than half-built.
    [Serializable]
    public class DevSkillDefinition
    {
        public string Name = "";
        public string Description = "";

        // Empty = orphan skill (no vanilla action feeds it XP, same shape
        // as Skinning/Building). Otherwise a global::Skills.SkillType
        // enum member name, e.g. "Sneak".
        public string XpSourceVanillaSkill = "";
    }

    [Serializable]
    public class DevSkillDefinitionList
    {
        public List<DevSkillDefinition> Skills = new List<DevSkillDefinition>();
    }
}
