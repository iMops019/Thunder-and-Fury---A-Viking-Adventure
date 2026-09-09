using HarmonyLib;
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
}
