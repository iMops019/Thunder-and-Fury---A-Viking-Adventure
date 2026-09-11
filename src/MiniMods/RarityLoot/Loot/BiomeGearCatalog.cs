using System;
using System.Collections.Generic;
using ThunderFury.RarityLoot.Affixes;

namespace ThunderFury.RarityLoot.Loot
{
    // ---- Live, tier-classified pool of every craftable weapon/armor slot ----
    //
    // Built once from ObjectDB.instance.m_recipes (public, confirmed via
    // decompile) the first time an ambient drop tries to roll -- not at
    // plugin Awake, since ObjectDB isn't guaranteed populated that early.
    // Same lazy-build-on-first-use shape VanillaItemCatalog already uses.
    // Five buckets (Weapon + the 4 armor slots) so the ambient roll can
    // pick "which slot drops" uniformly before picking "which item," per
    // the user's own two-stage description.
    public static class BiomeGearCatalog
    {
        class Entry
        {
            public UnityEngine.GameObject Prefab;
            public int Tier;
        }

        static readonly string[] Slots = { "Weapon", "Helmet", "Chest", "Legs", "Shoulder" };

        static Dictionary<string, List<Entry>> _byBucket;

        static void EnsureBuilt()
        {
            if (_byBucket != null) return;
            if (ObjectDB.instance == null) return;

            _byBucket = new Dictionary<string, List<Entry>>();
            foreach (string slot in Slots) _byBucket[slot] = new List<Entry>();

            int excludedCount = 0;

            foreach (Recipe recipe in ObjectDB.instance.m_recipes)
            {
                ItemDrop item = recipe?.m_item;
                if (item?.m_itemData?.m_shared == null) continue;

                ItemDrop.ItemData.SharedData shared = item.m_itemData.m_shared;
                string bucket = item.m_itemData.IsWeapon() ? "Weapon"
                    : ItemRoller.IsArmorSlot(shared.m_itemType) ? shared.m_itemType.ToString()
                    : null;
                if (bucket == null) continue;

                if (!ItemTierClassifier.TryGetItemTier(recipe, out int tier))
                {
                    excludedCount++;
                    continue;
                }

                _byBucket[bucket].Add(new Entry { Prefab = item.gameObject, Tier = tier });
            }

            Jotunn.Logger.LogInfo(
                $"RarityLoot: biome gear catalog built ({excludedCount} weapon/armor recipe(s) excluded -- unrecognized material, tier unknown).");
        }

        public static bool TryPickRandomItem(int maxTier, Random rng, out UnityEngine.GameObject prefab)
        {
            prefab = null;
            EnsureBuilt();
            if (_byBucket == null) return false;

            string slot = Slots[rng.Next(Slots.Length)];
            List<Entry> candidates = _byBucket[slot];

            List<Entry> eligible = new List<Entry>();
            foreach (Entry entry in candidates)
            {
                if (entry.Tier <= maxTier) eligible.Add(entry);
            }

            if (eligible.Count == 0) return false;

            prefab = eligible[rng.Next(eligible.Count)].Prefab;
            return true;
        }
    }
}
