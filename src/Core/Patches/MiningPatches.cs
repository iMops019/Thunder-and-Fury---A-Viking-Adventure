using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using VikingAdventure.Core.SkillSystem;

namespace VikingAdventure.Core.Patches
{
    // ---- Mining gameplay effects ----
    //
    // Same two mechanics as WoodcuttingPatches.cs, same reasoning --
    // "damage" is a smooth per-level HitData.m_damage scale, "speed" is
    // reframed as attack-stamina efficiency since vanilla has no
    // scriptable swing-speed stat (see WoodcuttingPatches.cs's header for
    // why). Ore/rock damage flows through two different components
    // depending on rock type -- MineRock5.RPC_Damage (newer, multi-area
    // rocks) and MineRock.RPC_Hit (older, single-area) -- so both are
    // patched to cover all ore nodes.
    public static class OreDamageBoost
    {
        public static void Apply(HitData hit)
        {
            if (hit == null) return;
            if (!(hit.GetAttacker() is Player player)) return;

            float level = player.GetSkills().GetSkillLevel(MiningSkill.Type);
            if (level <= 0f) return;

            float mult = 1f + level * CorePlugin.MiningDamagePerLevel.Value;
            hit.m_damage.m_damage *= mult;
            hit.m_damage.m_blunt *= mult;
            hit.m_damage.m_slash *= mult;
            hit.m_damage.m_pierce *= mult;
            hit.m_damage.m_chop *= mult;
            hit.m_damage.m_pickaxe *= mult;
            hit.m_damage.m_fire *= mult;
            hit.m_damage.m_frost *= mult;
            hit.m_damage.m_lightning *= mult;
            hit.m_damage.m_poison *= mult;
        }
    }

    [HarmonyPatch(typeof(MineRock5), nameof(MineRock5.RPC_Damage))]
    public static class MineRock5DamagePatch
    {
        static void Prefix(HitData hit) => OreDamageBoost.Apply(hit);
    }

    [HarmonyPatch(typeof(MineRock), nameof(MineRock.RPC_Hit))]
    public static class MineRockDamagePatch
    {
        static void Prefix(HitData hit) => OreDamageBoost.Apply(hit);
    }

    [HarmonyPatch(typeof(Attack), nameof(Attack.GetAttackStamina))]
    public static class MiningStaminaEfficiencyPatch
    {
        static void Postfix(Attack __instance, ref float __result)
        {
            if (__instance.m_weapon == null) return;
            if (__instance.m_weapon.m_shared.m_skillType != global::Skills.SkillType.Pickaxes) return;
            if (!(__instance.m_character is Player player)) return;

            float skillFactor = player.GetSkillFactor(MiningSkill.Type);
            __result -= __result * CorePlugin.MiningStaminaEfficiencyWeight.Value * skillFactor;
        }
    }

    // Mining shares Woodcutting's level-15 milestone (2x XP lives in
    // MiningSkill.AdjustXp; this is the ore-yield half). Same
    // independently-roll-bonus-drops technique as Woodcutting's log
    // yield, but detecting "this hit just destroyed the node" differs
    // per rock type since neither method's own return value distinguishes
    // "destroyed" from "merely damaged":
    //  - MineRock5 tracks each hit area's health as a plain field
    //    (HitArea.m_health), so a Postfix on DamageArea can just
    //    re-check it after the original runs -- <= 0 means this call
    //    was the one that zeroed it (an already-destroyed area returns
    //    early before reaching the health subtraction at all, so this
    //    can't double-fire on a dead area).
    //  - MineRock (the older single-area variant) tracks health in the
    //    ZDO instead (key "Health"+index), same idea: re-read it after
    //    RPC_Hit runs.
    public static class OreYieldBoost
    {
        public static void SpawnBonus(DropTable dropTable, Vector3 position)
        {
            List<GameObject> dropList = dropTable.GetDropList();
            if (dropList.Count == 0) return;

            int extra = Mathf.RoundToInt(dropList.Count * CorePlugin.MiningMilestoneOreYieldBonusPercent.Value / 100f);
            for (int i = 0; i < extra; i++)
            {
                GameObject prefab = dropList[i % dropList.Count];
                Vector3 pos = position + Random.insideUnitSphere * 0.3f;
                ItemDrop.OnCreateNew(Object.Instantiate(prefab, pos, Quaternion.identity));
            }
        }

        public static bool MeetsMilestone(HitData hit, out Vector3 attackerPos)
        {
            attackerPos = default;
            if (hit == null) return false;
            if (!(hit.GetAttacker() is Player player)) return false;

            float level = player.GetSkills().GetSkillLevel(MiningSkill.Type);
            if (level < CorePlugin.MiningMilestoneLevel.Value) return false;

            attackerPos = player.transform.position;
            return true;
        }
    }

    [HarmonyPatch(typeof(MineRock5), nameof(MineRock5.DamageArea))]
    public static class MineRock5MilestoneYieldPatch
    {
        static void Postfix(MineRock5 __instance, int hitAreaIndex, HitData hit)
        {
            if (!OreYieldBoost.MeetsMilestone(hit, out _)) return;

            MineRock5.HitArea hitArea = __instance.GetHitArea(hitAreaIndex);
            if (hitArea == null || hitArea.m_health > 0f) return;

            OreYieldBoost.SpawnBonus(__instance.m_dropItems, __instance.transform.position);
        }
    }

    [HarmonyPatch(typeof(MineRock), nameof(MineRock.RPC_Hit))]
    public static class MineRockMilestoneYieldPatch
    {
        static void Postfix(MineRock __instance, HitData hit, int hitAreaIndex)
        {
            if (!OreYieldBoost.MeetsMilestone(hit, out Vector3 attackerPos)) return;

            float health = __instance.m_nview.GetZDO().GetFloat("Health" + hitAreaIndex, __instance.GetHealth());
            if (health > 0f) return;

            OreYieldBoost.SpawnBonus(__instance.m_dropItems, attackerPos);
        }
    }
}
