using HarmonyLib;
using Jotunn.Managers;
using UnityEngine;
using ThunderFury.DevTool.Data;

namespace ThunderFury.DevTool.Patches
{
    // ---- Dungeon-exclusive bonus loot ----
    //
    // Why this shape instead of injecting into vanilla's own dungeon
    // loot: confirmed via decompile that dungeon interiors are built by
    // DungeonGenerator assembling hand-authored ROOM prefabs, each with
    // its own baked-in spawn points -- there's no central "dungeon loot
    // table" to add an entry to, just hundreds of individual room
    // prefabs. Reverse-engineering and safely patching all of those is a
    // much bigger, riskier undertaking than this feature needs.
    //
    // Instead: an independent bonus roll layered on top of whatever a
    // dungeon container already holds, the first time it's opened.
    // "Inside a dungeon" is a proximity check (any DungeonGenerator
    // within range) rather than a precise zone-membership lookup --
    // same style of approximation ValheimQoL's CraftFromContainers
    // already uses for "nearby," good enough for a bonus-roll feature
    // that doesn't need pixel-perfect boundaries.
    //
    // Persistence: the "already rolled" flag lives on the container's
    // own ZDO (same real mechanism vanilla itself uses for e.g.
    // "has this player discovered this container" -- GetZDO().GetBool),
    // so a container doesn't re-roll every time it's reopened, including
    // across a save/reload.
    [HarmonyPatch(typeof(Container), nameof(Container.Interact))]
    public static class DungeonLootPatch
    {
        const string RolledKey = "ThunderFury_DungeonLootRolled";
        const float DungeonDetectionRadius = 50f;

        static void Prefix(Container __instance, bool hold)
        {
            if (hold) return;
            if (DungeonLootRegistry.Entries.Count == 0) return;

            ZNetView nview = __instance.GetComponent<ZNetView>();
            if (nview == null || nview.GetZDO() == null) return;
            if (nview.GetZDO().GetBool(RolledKey)) return;

            if (!IsInsideDungeon(__instance.transform.position)) return;

            nview.GetZDO().Set(RolledKey, true);
            RollBonusLoot(__instance);
        }

        static bool IsInsideDungeon(Vector3 position)
        {
            foreach (Collider collider in Physics.OverlapSphere(position, DungeonDetectionRadius))
            {
                if (collider.GetComponentInParent<DungeonGenerator>() != null) return true;
            }
            return false;
        }

        static void RollBonusLoot(Container container)
        {
            foreach (DungeonLootEntry entry in DungeonLootRegistry.Entries)
            {
                if (string.IsNullOrEmpty(entry.ItemName)) continue;
                if (Random.value * 100f > entry.ChancePercent) continue;

                GameObject prefab = PrefabManager.Instance.GetPrefab(entry.ItemName);
                if (prefab == null)
                {
                    Jotunn.Logger.LogWarning($"DevTool: dungeon loot pool item '{entry.ItemName}' could not be resolved, skipping this roll");
                    continue;
                }

                int amount = Random.Range(entry.MinAmount, entry.MaxAmount + 1);
                if (amount <= 0) continue;

                container.GetInventory().AddItem(prefab, amount);
            }
        }
    }
}
