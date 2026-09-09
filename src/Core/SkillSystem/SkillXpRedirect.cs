using System;
using System.Collections.Generic;
using HarmonyLib;

namespace VikingAdventure.Core.SkillSystem
{
    // ---- Generic vanilla-skill -> custom-skill XP redirect ----
    //
    // Confirmed against the real 1.0 decompile: Player.RaiseSkill(SkillType, float)
    // is the single choke point every XP gain funnels through -- weapon
    // attacks (Attack.cs's m_raiseSkillAmount, keyed off the weapon's own
    // m_shared.m_skillType), running, swimming, sneaking, blocking,
    // dodging, jumping, building all call it. This is what "vanilla
    // skills stay technically present but get neutralized" (vision.md)
    // actually means in code: register a vanilla SkillType here and its
    // underlying vanilla Skill object never gains XP or levels again --
    // every skill built on Core routes through this same redirect, not a
    // one-off patch per skill.
    //
    // Note: only the XP-neutralizing half is handled here. Hiding the
    // vanilla skill from the in-game skill list UI is a separate, real UI
    // patch that hasn't been researched yet -- Pillar 2 territory.
    public static class SkillXpRedirect
    {
        public class Entry
        {
            public global::Skills.SkillType CustomType;
            public Func<Player, float, float> AdjustXp;
        }

        static readonly Dictionary<global::Skills.SkillType, Entry> Map =
            new Dictionary<global::Skills.SkillType, Entry>();

        // adjustXp lets a skill apply its own milestone/scaling logic
        // (e.g. Woodcutting's level-15 XP double) without this generic
        // redirect needing to know about any specific skill.
        public static void Register(global::Skills.SkillType vanillaType, global::Skills.SkillType customType, Func<Player, float, float> adjustXp = null)
        {
            Map[vanillaType] = new Entry { CustomType = customType, AdjustXp = adjustXp };
        }

        public static bool TryGet(global::Skills.SkillType vanillaType, out Entry entry)
        {
            return Map.TryGetValue(vanillaType, out entry);
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.RaiseSkill))]
    public static class RaiseSkillRedirectPatch
    {
        static bool Prefix(Player __instance, global::Skills.SkillType skill, float value)
        {
            if (!SkillXpRedirect.TryGet(skill, out SkillXpRedirect.Entry entry)) return true;

            float adjusted = entry.AdjustXp != null ? entry.AdjustXp(__instance, value) : value;
            __instance.GetSkills().RaiseSkill(entry.CustomType, adjusted);
            return false;
        }
    }
}
