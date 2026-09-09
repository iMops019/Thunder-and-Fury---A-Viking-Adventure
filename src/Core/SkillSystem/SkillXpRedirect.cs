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
    // Registering a vanilla SkillType here also hides it from the in-game
    // skill list (see HideRedirectedSkillsPatch below) -- one
    // registration call does both halves of "neutralized and hidden"
    // (vision.md), since hiding is really just "don't show a skill we've
    // already made permanently frozen."
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

        public static bool IsRedirected(global::Skills.SkillType vanillaType)
        {
            return Map.ContainsKey(vanillaType);
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

    // Confirmed against the real 1.0 decompile: every consumer of "what
    // skills does this player have" -- SkillsDialog's UI list included --
    // goes through Skills.GetSkillList(), which builds a fresh
    // List<Skills.Skill> from internal storage on every call. Filtering
    // redirected (neutralized) skills out of that list here hides them
    // everywhere at once, rather than needing a patch on the dialog
    // itself or any other future consumer of the list.
    [HarmonyPatch(typeof(global::Skills), nameof(global::Skills.GetSkillList))]
    public static class HideRedirectedSkillsPatch
    {
        static void Postfix(List<global::Skills.Skill> __result)
        {
            __result.RemoveAll(skill => SkillXpRedirect.IsRedirected(skill.m_info.m_skill));
        }
    }
}
