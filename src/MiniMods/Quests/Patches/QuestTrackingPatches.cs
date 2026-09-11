using System.Collections.Generic;
using HarmonyLib;
using ThunderFury.Core.Quests;
using ThunderFury.Quests.Data;

namespace ThunderFury.Quests.Patches
{
    // ---- KillCreature objective tracking ----
    //
    // Character.OnDeath() (confirmed via the real 1.0 decompile, same
    // conclusion Skinning's own carcass patch already reached) takes no
    // arguments, so there's no direct "who killed this" to read there --
    // needs its own tiny bit of state. Character.RPC_Damage(long,
    // HitData) is the same hook Defense's damage-reduction patch already
    // uses (docs/PROGRESS.md's Combat stats entry); HitData.GetAttacker()
    // there is the exact call TreeDamageBoost/RarityLoot's affix patches
    // already use to identify an attacking Player. Recording the last
    // Player to hit a Character there, then reading it back in an
    // OnDeath Postfix, is a small, self-contained addition -- doesn't
    // touch or reinterpret anything Defense's own patch does, multiple
    // Prefixes/Postfixes on the same method already coexist elsewhere in
    // this codebase (Harmony chains them).
    public static class KillTracking
    {
        static readonly Dictionary<Character, Player> LastAttacker = new Dictionary<Character, Player>();

        [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
        public static class RecordLastAttackerPatch
        {
            static void Prefix(Character __instance, HitData hit)
            {
                if (hit?.GetAttacker() is Player player)
                {
                    LastAttacker[__instance] = player;
                }
            }
        }

        [HarmonyPatch(typeof(Character), nameof(Character.OnDeath))]
        public static class GrantKillCreditPatch
        {
            static void Postfix(Character __instance)
            {
                if (!LastAttacker.TryGetValue(__instance, out Player killer))
                {
                    return;
                }
                LastAttacker.Remove(__instance);

                string creatureName = ThunderFury.Core.Utils.PrefabNameHelper.GetPrefabName(__instance);
                if (creatureName == null) return;

                foreach (QuestDefinition quest in QuestRegistry.All)
                {
                    if (quest.ObjectiveType != QuestObjectiveType.KillCreature) continue;
                    if (quest.Target != creatureName) continue;
                    if (QuestPlayerState.GetStatus(killer, quest.Id) != QuestStatus.Active) continue;

                    int progress = QuestPlayerState.GetProgress(killer, quest.Id) + 1;
                    QuestPlayerState.SetProgress(killer, quest.Id, progress);

                    if (progress >= quest.TargetCount)
                    {
                        QuestPlayerState.CompleteAndAdvance(killer, quest);
                    }
                }
            }
        }
    }

    // ---- DiscoverBiome objective tracking ----
    //
    // Originally targeted Player.AddKnownBiome(Heightmap.Biome) directly
    // (the real vanilla method that fires the "Found new land: X"
    // banner). Confirmed in-game (2026-09-10) that a same-session Valheim
    // update changed that method to private and switched its parameter
    // to a new BiomeSector type, breaking the patch (HarmonyX produced
    // invalid IL trying to reconcile the mismatch). Rebuilt on
    // Player.UpdateBiome instead -- also private now, but Harmony patches
    // by reflection regardless of accessibility, and this method's own
    // job (deciding m_currentBiome once a second) is exactly "did the
    // biome just change," making it a stable place to piggyback a check
    // via the still-public GetCurrentBiome() getter rather than reading
    // any of the private/actively-changing internals directly.
    [HarmonyPatch(typeof(Player), "UpdateBiome")]
    public static class DiscoverBiomePatch
    {
        static void Postfix(Player __instance)
        {
            string biomeName = __instance.GetCurrentBiome().ToString();
            if (QuestPlayerState.HasVisitedBiome(__instance, biomeName)) return;

            QuestPlayerState.MarkBiomeVisited(__instance, biomeName);

            foreach (QuestDefinition quest in QuestRegistry.All)
            {
                if (quest.ObjectiveType != QuestObjectiveType.DiscoverBiome) continue;
                if (quest.Target != biomeName) continue;
                if (QuestPlayerState.GetStatus(__instance, quest.Id) != QuestStatus.Active) continue;

                QuestPlayerState.CompleteAndAdvance(__instance, quest);
            }
        }
    }
}
