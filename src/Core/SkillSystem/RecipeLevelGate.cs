using System.Collections.Generic;
using HarmonyLib;

namespace ThunderFury.Core.SkillSystem
{
    // ---- Generic recipe skill+level gate ----
    //
    // Same technique RarityLoot's LegendaryCraftGatePatch already proved
    // out (Player.HaveRequirements(Recipe, bool, int, int) is the exact
    // check InventoryGui.DoCrafting gates the real craft attempt on, not
    // just a UI hint), generalized by recipe item name instead of
    // hardcoded to "is this item Legendary" -- built for the Dev Tool's
    // Recipe Creator so ANY recipe (not just RarityLoot's own tier) can
    // optionally require a skill level. RarityLoot's own patch is left
    // as-is rather than rebuilt on top of this -- two independent Prefixes
    // on the same method coexist fine (Harmony chains them), same pattern
    // already relied on elsewhere in this codebase (e.g. Strength's XP
    // share alongside Attack's).
    public static class RecipeLevelGate
    {
        public class Entry
        {
            public string SkillName;
            public int RequiredLevel;
        }

        static readonly Dictionary<string, Entry> ByItemName = new Dictionary<string, Entry>();

        public static void Register(string itemName, string skillName, int requiredLevel)
        {
            ByItemName[itemName] = new Entry { SkillName = skillName, RequiredLevel = requiredLevel };
        }

        public static void Unregister(string itemName)
        {
            ByItemName.Remove(itemName);
        }

        public static bool TryGet(string itemName, out Entry entry)
        {
            return ByItemName.TryGetValue(itemName, out entry);
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.HaveRequirements), typeof(Recipe), typeof(bool), typeof(int), typeof(int))]
    public static class RecipeLevelGatePatch
    {
        static bool Prefix(Player __instance, Recipe recipe, bool discover, ref bool __result)
        {
            if (discover) return true;
            if (recipe == null || recipe.m_item == null) return true;
            if (!RecipeLevelGate.TryGet(recipe.m_item.name, out RecipeLevelGate.Entry entry)) return true;
            if (!SkillRegistry.TryGet(entry.SkillName, out global::Skills.SkillType type)) return true;

            float level = __instance.GetSkills().GetSkillLevel(type);
            if (level >= entry.RequiredLevel) return true;

            __instance.Message(MessageHud.MessageType.Center,
                $"Requires {entry.SkillName} level {entry.RequiredLevel}.");
            __result = false;
            return false;
        }
    }
}
