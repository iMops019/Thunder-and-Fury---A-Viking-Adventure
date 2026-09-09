using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace ThunderFury.Core.SkillSystem
{
    // ---- Smithing ----
    //
    // Fifth skill, and like Skinning, has no vanilla equivalent to
    // redirect from -- Skills.SkillType has no "Smithing" entry. The
    // closest, "Crafting", was deliberately NOT used as a redirect
    // source: CraftingStation.m_craftingSkill defaults to
    // Skills.SkillType.Crafting in code, but whether any given vanilla
    // station's real prefab data actually overrides that default
    // couldn't be confirmed (that's serialized data in a Unity asset
    // bundle, not something visible in the decompiled C#) -- redirecting
    // the whole Crafting SkillType risked silently pulling in Cooking's
    // Cauldron or some other unrelated station if its real data left the
    // default in place. Setting our own skill directly on the specific
    // stations we want sidesteps that uncertainty entirely.
    //
    // Confirmed against the real 1.0 decompile: InventoryGui.DoCrafting
    // already calls
    // `Player.m_localPlayer.RaiseSkill(m_craftRecipe.m_craftingStation.m_craftingSkill, ...)`
    // whenever a recipe is crafted at a station -- a fully generic,
    // already-working "raise this skill when crafting here" mechanic
    // that just isn't pointed at anything meaningful by default. No
    // custom XP-granting code needed, same pattern as Skinning's reuse
    // of Pickable.m_pickRaiseSkill: just point
    // CraftingStation.m_craftingSkill at our skill.
    //
    // Also confirmed: DoCrafting handles Recipe objects (weapons, tools,
    // consumables) exclusively -- Piece objects (building pieces like
    // walls) go through a completely separate placement system with no
    // skill tied to it at all. So setting this on the Workbench doesn't
    // risk granting Smithing XP for building a wall.
    //
    // vision.md's "Smithing tier ladder (Stone/Bronze/Iron/etc.)" maps to
    // Workbench/Forge/Black Forge -- all three set here. Legendary
    // gear's level-gated crafting (also locked in by vision.md) lives in
    // RarityLoot instead (Patches/LegendaryCraftGatePatch.cs), since it
    // needs RarityLoot's own rarity-tier registry and Core can't depend
    // on RarityLoot.
    public static class SmithingSkill
    {
        public const string Identifier = "com.ThunderFury.core.skill.smithing";

        public static global::Skills.SkillType Type { get; private set; }

        public static void Register()
        {
            var config = new SkillConfig
            {
                Identifier = Identifier,
                Name = "Smithing",
                Description = "Forging weapons, tools, and armor at a Workbench, Forge, or Black Forge. Legendary gear requires a Smithing level to craft.",
                IncreaseStep = 1f,
            };

            Type = SkillManager.Instance.AddSkill(config);

            SetStationSkill(CraftingStations.Workbench);
            SetStationSkill(CraftingStations.Forge);
            SetStationSkill(CraftingStations.BlackForge);
        }

        static void SetStationSkill(string stationPrefabName)
        {
            GameObject station = PrefabManager.Instance.GetPrefab(stationPrefabName);
            CraftingStation craftingStation = station != null ? station.GetComponent<CraftingStation>() : null;
            if (craftingStation == null)
            {
                Jotunn.Logger.LogError($"Smithing: could not find a CraftingStation on '{stationPrefabName}', skipping");
                return;
            }

            craftingStation.m_craftingSkill = Type;
        }
    }
}
