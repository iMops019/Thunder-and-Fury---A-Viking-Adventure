using System.Collections.Generic;
using System.Globalization;
using HarmonyLib;
using UnityEngine;

namespace ThunderFury.RarityLoot.Patches
{
    // ---- Log yield bonus (Voltun's Hatchet's deferred "+50% log yield") ----
    //
    // Researched against the real 1.0 decompile. A chopped-down tree
    // drops one physical log entity (TreeBase.SpawnLog), which the player
    // then chops on the ground -- TreeLog.RPC_Damage tracks that log's
    // own health and calls TreeLog.Destroy(HitData) once it hits 0. That
    // method is where "Wood" items actually spawn:
    //   List<GameObject> dropList = m_dropWhenDestroyed.GetDropList();
    //   ... foreach entry, instantiate `dropCount` copies (dropCount is 1
    //   unless a damage-type item-conversion rule applies, e.g. fire
    //   converting a drop into coal -- unrelated to yield bonuses) ...
    // dropCount is a local variable with no accessor, so it can't be
    // reached from a plain Prefix/Postfix parameter or __result the way
    // the earlier QoL patches could -- reaching it directly would need a
    // Transpiler. Sidestepped that entirely: since GetDropList() and the
    // tree's position are both reachable through TreeLog's own public
    // field/component (not the local variable), a Prefix independently
    // rolls its own bonus drops using the same drop table and spawns them
    // before vanilla's Destroy() runs, rather than trying to inflate
    // vanilla's own count. Net effect on the player is identical (more
    // Wood on the ground when the log's cleared); the extra items are
    // just a second, independent spawn rather than a modified original
    // one.
    //
    // Deliberately a Prefix, not a Postfix: Destroy()'s first line
    // destroys the TreeLog's GameObject, and Unity's Destroy() makes
    // further field access on it unreliable within the same call --
    // reading __instance.transform.position and m_dropWhenDestroyed has
    // to happen before the original runs.
    [HarmonyPatch(typeof(TreeLog), nameof(TreeLog.Destroy))]
    public static class LogYieldPatch
    {
        // Generic key, not Voltun-specific -- any future item can grant
        // this same bonus by setting this key in its own m_customData,
        // the same way VoltunsSet bakes it in as a fixed identity bonus
        // via ItemData.Clone()'s deep-copy (see ItemRoller's header
        // comment on why m_customData is the safe place for per-item
        // data like this).
        public const string LogYieldBonusKey = "rarityloot_logyield_pct";

        static void Prefix(TreeLog __instance, HitData hitData)
        {
            if (hitData == null) return;
            if (!(hitData.GetAttacker() is Player player)) return;
            if (!TryGetLogYieldBonus(player, out float bonusPct) || bonusPct <= 0f) return;

            List<GameObject> dropList = __instance.m_dropWhenDestroyed.GetDropList();
            if (dropList.Count == 0) return;

            int extra = Mathf.RoundToInt(dropList.Count * bonusPct / 100f);
            Vector3 basePos = __instance.transform.position;

            for (int i = 0; i < extra; i++)
            {
                GameObject prefab = dropList[i % dropList.Count];
                Vector3 pos = basePos + Random.insideUnitSphere * 0.5f + Vector3.up * 0.3f;
                ItemDrop.OnCreateNew(Object.Instantiate(prefab, pos, Quaternion.identity));
            }
        }

        static bool TryGetLogYieldBonus(Player player, out float bonusPct)
        {
            bonusPct = 0f;
            ItemDrop.ItemData weapon = player.m_rightItem;
            if (weapon?.m_customData == null) return false;
            if (!weapon.m_customData.TryGetValue(LogYieldBonusKey, out string raw)) return false;
            return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out bonusPct);
        }
    }
}
