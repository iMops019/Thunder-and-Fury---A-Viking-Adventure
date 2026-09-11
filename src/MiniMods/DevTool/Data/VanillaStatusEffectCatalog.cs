using System;
using System.Collections.Generic;

namespace ThunderFury.DevTool.Data
{
    // ---- Live catalog of every status effect in the game ----
    //
    // Closes the "cooking special buff" gap flagged earlier this session
    // (docs/PROGRESS.md's Cooking entry) -- Item Creator could only ever
    // set stat multipliers, nothing let a food/potion/weapon grant an
    // actual buff. Rather than build a whole custom-status-effect author-
    // ing tool (a much bigger undertaking -- StatusEffect subclasses carry
    // real per-type behavior, not just data), this reuses one of the
    // game's OWN existing effects (confirmed via decompile:
    // ObjectDB.instance.m_StatusEffects is a real public list of every
    // registered one) -- same "assembled from existing assets, not
    // modeled/authored from scratch" philosophy this whole mod already
    // runs on for items/pieces.
    public static class VanillaStatusEffectCatalog
    {
        public class Entry
        {
            public string InternalName;
        }

        public static List<Entry> GetAll()
        {
            var result = new List<Entry>();
            if (ObjectDB.instance == null) return result;

            foreach (StatusEffect effect in ObjectDB.instance.m_StatusEffects)
            {
                if (effect == null) continue;
                result.Add(new Entry { InternalName = effect.name });
            }

            result.Sort((a, b) => string.Compare(a.InternalName, b.InternalName, StringComparison.OrdinalIgnoreCase));
            return result;
        }
    }
}
