using System;
using System.Collections.Generic;

namespace ThunderFury.DevTool.Data
{
    // ---- Item Creator's data model ----
    //
    // Field-only + [Serializable] so UnityEngine.JsonUtility (already
    // available via the Unity/Jotunn reference, no new dependency) can
    // (de)serialize it directly. Stat fields are multipliers relative to
    // whatever the cloned base item's real value turns out to be at
    // runtime -- same "relative to the real cloned value" pattern
    // StonePickaxe/VoltunsSet already use by hand, deliberately kept
    // consistent rather than inventing absolute-number stats here.
    [Serializable]
    public class DevItemDefinition
    {
        public string Name = "";
        public string BasePrefabName = "";
        public string DisplayName = "";
        public string Description = "";

        public float DamageMultiplier = 1f;
        // Additive, not a multiplier -- a plain sword has 0 base lightning
        // (or fire/frost/poison) damage, so multiplying by anything stays
        // 0. This is how an item gets an elemental damage type it didn't
        // already have, on top of whatever its base physical damage is.
        public float BonusLightningDamage = 0f;
        public float BonusFireDamage = 0f;
        public float BonusFrostDamage = 0f;
        public float BonusPoisonDamage = 0f;
        public float ArmorMultiplier = 1f;
        public float WeightMultiplier = 1f;
        // Lower confidence than the other three -- "m_maxDurability" is a
        // well-established Valheim field name but wasn't independently
        // confirmed against this project's own decompile the way
        // m_damages/m_armor/m_weight were (m_weight is already referenced
        // in docs/DESIGN.md's own modding-limits notes). Safe failure mode
        // if wrong: this field simply has no effect, nothing crashes.
        public float DurabilityMultiplier = 1f;
        public float SpeedMultiplier = 1f;

        public string CraftingStation = "";
        public int MinStationLevel = 1;

        // Closes the "cooking special buff" gap (docs/PROGRESS.md's
        // Cooking entry) -- reuses one of the game's OWN existing status
        // effects (see VanillaStatusEffectCatalog.cs) rather than
        // authoring a new one. Applied to both m_consumeStatusEffect
        // (food/potions) and m_equipStatusEffect (weapons/armor) on
        // registration -- setting the field a given item type doesn't
        // read is harmless, so one field covers both cases without
        // needing to know the item's type up front.
        public string StatusEffectName = "";

        // Generic on-hit proc hook (Core/Combat/WeaponSpecialEffectPatch.cs)
        // -- "" or "None" = no special effect. Built for the Lightning
        // Sword idea (2026-09-10) but any weapon can opt in; extending the
        // list later is a new case in that patch's switch, not a new
        // mechanism.
        public string SpecialEffect = "";

        public List<DevRequirement> Requirements = new List<DevRequirement>();
    }

    [Serializable]
    public class DevItemDefinitionList
    {
        public List<DevItemDefinition> Items = new List<DevItemDefinition>();
    }
}
