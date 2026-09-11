using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ThunderFury.Core.Combat
{
    // ---- Chain Lightning: a real, reusable weapon proc ----
    //
    // Built for the "Lightning Sword" idea (2026-09-10, user's own
    // design) but deliberately generic -- any weapon can opt in by
    // setting WeaponSpecialEffects.EffectKey to "ChainLightning" in its
    // own ItemData.m_customData (the exact same "identity bonus baked
    // into per-instance custom data" pattern Voltun's Hatchet already
    // uses for its log-yield bonus), so this isn't a one-off hack tied to
    // a single hand-registered item.
    //
    // Visual effect is deliberately self-contained: rather than touching
    // the hit character's own renderer/shader/material (real risk --
    // Valheim's character shaders weren't decompiled/verified for this,
    // and a bad property name would either no-op silently or throw on
    // every hit), this spawns short-lived child GameObjects with their
    // own LineRenderer using Unity's always-available "Sprites/Default"
    // shader. Reads as a flickering electrical arc without depending on
    // anything about the target's own asset setup.
    public static class ChainLightningEffect
    {
        public static void TryTrigger(Character initialTarget, Character attacker)
        {
            if (initialTarget == null || attacker == null) return;
            if (Random.value > CorePlugin.ChainLightningChance.Value) return;

            // Logged so a proc is verifiable from the BepInEx log directly
            // (2026-09-10: user testing couldn't tell whether it was
            // firing at all) rather than relying on spotting a ~2-second
            // visual effect mid-combat.
            Jotunn.Logger.LogInfo($"Chain Lightning triggered from {attacker.GetHoverName()} on {initialTarget.GetHoverName()}");

            SpawnBodySparks(initialTarget);

            var alreadyHit = new HashSet<Character> { initialTarget };
            Character current = initialTarget;

            for (int jump = 0; jump < CorePlugin.ChainLightningMaxJumps.Value; jump++)
            {
                Character next = FindNearestUnhitEnemy(current, alreadyHit, CorePlugin.ChainLightningRange.Value);
                if (next == null) break;

                HitData chainHit = new HitData();
                chainHit.m_damage.m_lightning = CorePlugin.ChainLightningDamagePerJump.Value;
                chainHit.SetAttacker(attacker);
                next.Damage(chainHit);

                SpawnArc(current.GetCenterPoint(), next.GetCenterPoint());
                SpawnBodySparks(next);

                alreadyHit.Add(next);
                current = next;
            }
        }

        static Character FindNearestUnhitEnemy(Character from, HashSet<Character> alreadyHit, float range)
        {
            Character closest = null;
            float closestDist = float.MaxValue;

            foreach (Collider collider in Physics.OverlapSphere(from.GetCenterPoint(), range))
            {
                Character candidate = collider.GetComponentInParent<Character>();
                if (candidate == null || candidate == from) continue;
                if (alreadyHit.Contains(candidate)) continue;
                if (candidate.IsPlayer()) continue; // don't chain onto players -- allies, not targets
                if (candidate.IsDead()) continue;

                float dist = Vector3.Distance(from.GetCenterPoint(), candidate.GetCenterPoint());
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = candidate;
                }
            }

            return closest;
        }

        static void SpawnArc(Vector3 from, Vector3 to)
        {
            GameObject arcGO = new GameObject("LightningArc");
            arcGO.transform.position = from;

            LineRenderer line = arcGO.AddComponent<LineRenderer>();
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = new Color(0.6f, 0.85f, 1f, 1f);
            line.endColor = new Color(0.8f, 0.95f, 1f, 0.5f);
            line.startWidth = 0.06f;
            line.endWidth = 0.02f;
            line.positionCount = 4;

            for (int i = 0; i < 4; i++)
            {
                float t = i / 3f;
                Vector3 point = Vector3.Lerp(from, to, t);
                if (i > 0 && i < 3) point += Random.insideUnitSphere * 0.25f;
                line.SetPosition(i, point);
            }

            Object.Destroy(arcGO, 0.15f);
        }

        // "A couple seconds" of flickering sparks anchored around the
        // target -- a Coroutine needs a MonoBehaviour host, so this rides
        // on a throwaway GameObject rather than the target itself (never
        // touches the target's own components).
        static void SpawnBodySparks(Character target)
        {
            GameObject host = new GameObject("LightningSparkHost");
            SparkRunner runner = host.AddComponent<SparkRunner>();
            runner.StartCoroutine(runner.Run(target));
        }

        class SparkRunner : MonoBehaviour
        {
            public IEnumerator Run(Character target)
            {
                float elapsed = 0f;
                const float duration = 2f;
                while (elapsed < duration && target != null)
                {
                    Vector3 center = target.GetCenterPoint();
                    Vector3 a = center + Random.insideUnitSphere * 0.4f;
                    Vector3 b = center + Random.insideUnitSphere * 0.4f;
                    SpawnArc(a, b);

                    yield return new WaitForSeconds(0.12f);
                    elapsed += 0.12f;
                }

                Destroy(gameObject);
            }
        }
    }
}
