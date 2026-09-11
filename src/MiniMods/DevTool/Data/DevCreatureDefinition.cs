using System;
using System.Collections.Generic;

namespace ThunderFury.DevTool.Data
{
    // ---- Creature Creator's data model ----
    //
    // Deliberately narrower than Item/Piece Creator, scoped down on
    // purpose rather than half-faking the rest: clone + rename + HP
    // scaling only. Damage scaling was left out after checking how wildly
    // it varies across creature AI types (some use a single weapon-style
    // damage value, others multiple attacks with their own damage sets,
    // some ranged, some melee) -- there's no one generic field the way
    // items have SharedData.m_damages. Drops for a new creature are
    // already fully covered by the Item Creator's own "Drop Sources"
    // section (any creature, including one made here, can be picked
    // there once it's registered) -- not duplicated here.
    [Serializable]
    public class DevCreatureDefinition
    {
        public string Name = "";
        public string BasePrefabName = "";
        public string DisplayName = "";

        public float HealthMultiplier = 1f;
    }

    [Serializable]
    public class DevCreatureDefinitionList
    {
        public List<DevCreatureDefinition> Creatures = new List<DevCreatureDefinition>();
    }
}
