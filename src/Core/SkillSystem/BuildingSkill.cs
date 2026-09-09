using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace VikingAdventure.Core.SkillSystem
{
    // ---- Building ----
    //
    // Ninth skill. vision.md, locked in: "not gated behind a skill/level
    // at all -- building stays open and unrestricted, same as vanilla.
    // May still exist as a nominal skill, but no functional gating
    // planned." Built exactly to that: registration only, no custom
    // gameplay code.
    //
    // No vanilla equivalent to redirect from (no "Building" entry in
    // Skills.SkillType), but confirmed against the real 1.0 decompile:
    // Player already has a fully generic "raise this skill when placing
    // a piece from this table" mechanism --
    // `if (m_buildPieces.m_skill != Skills.SkillType.None) RaiseSkill(m_buildPieces.m_skill);`
    // right after a successful TryPlacePiece -- unused by any vanilla
    // PieceTable (there's no vanilla Building skill to assign it to).
    // Same reuse pattern as Skinning's Pickable.m_pickRaiseSkill and
    // Smithing's CraftingStation.m_craftingSkill: set
    // PieceTable.m_skill directly rather than writing any new
    // XP-granting code.
    //
    // Scoped to the Hammer's piece table only (structural building --
    // walls, floors, etc.) -- not the Cultivator, which is terraforming,
    // a separate, explicitly not-yet-designed feature per vision.md's
    // own Parking Lot section.
    //
    // One incidental, accepted side effect: Player.GetBuildStamina()
    // already reduces hammer-swing stamina cost by
    // `0.5 * GetSkillFactor(m_buildPieces.m_skill)` -- the exact same
    // "stamina efficiency" pattern Woodcutting/Mining reuse via
    // Attack.GetAttackStamina. This isn't a gate on anything (building
    // is never locked, matching vision.md), just the same free
    // efficiency bonus every other skill already gets from reusing a
    // vanilla mechanic, so it's left as-is rather than suppressed.
    //
    // Verification caveat, same shape as StonePickaxe's:
    // "_HammerPieceTable" is a standard, extremely well-established
    // Jotunn name (used in Jotunn's own official custom-piece tutorial),
    // not independently confirmed against this install's binary asset
    // data.
    public static class BuildingSkill
    {
        public const string Identifier = "com.vikingadventure.core.skill.building";

        public static global::Skills.SkillType Type { get; private set; }

        public static void Register()
        {
            var config = new SkillConfig
            {
                Identifier = Identifier,
                Name = "Building",
                Description = "Placing structural pieces with a Hammer. A nominal skill by design -- building itself is never gated by level.",
                IncreaseStep = 1f,
            };

            Type = SkillManager.Instance.AddSkill(config);

            GameObject hammerTable = PrefabManager.Instance.GetPrefab("_HammerPieceTable");
            PieceTable pieceTable = hammerTable != null ? hammerTable.GetComponent<PieceTable>() : null;
            if (pieceTable == null)
            {
                Jotunn.Logger.LogError("Building: could not find the Hammer's PieceTable ('_HammerPieceTable'), skipping XP wiring");
                return;
            }

            pieceTable.m_skill = Type;
        }
    }
}
