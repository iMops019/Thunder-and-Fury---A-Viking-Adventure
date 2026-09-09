using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ThunderFury.RarityLoot.Affixes
{
    // Rolls and reads rarity/affix data on a specific item INSTANCE via
    // ItemDrop.ItemData.m_customData -- confirmed against the real 1.0
    // decompile that this Dictionary<string,string> field is genuinely
    // written and read by vanilla's own Inventory save/load code (same
    // mechanism Player.m_customData uses), and that ItemData.Clone()
    // deep-copies it, so a roll survives saving and stays independent of
    // every other instance of the same item -- deliberately never touches
    // ItemData.m_shared, which is the SAME object shared by every
    // instance of that item type; mutating it would rewrite every copy of
    // that item in the world, not just this one roll.
    public static class ItemRoller
    {
        public const string TierKey = "rarityloot_tier";
        const string AffixKeyPrefix = "rarityloot_affix_";

        static readonly Random Rng = new Random();

        public static bool IsRolled(ItemDrop.ItemData item)
        {
            return item.m_customData != null && item.m_customData.ContainsKey(TierKey);
        }

        public static void Roll(ItemDrop.ItemData item, RarityTier tier, int affixCount)
        {
            if (item.m_customData == null)
            {
                item.m_customData = new Dictionary<string, string>();
            }

            item.m_customData[TierKey] = tier.ToString();

            AffixTarget target = item.IsWeapon() ? AffixTarget.Weapon
                : IsArmorSlot(item.m_shared.m_itemType) ? AffixTarget.Armor
                : AffixTarget.Any;

            List<AffixDefinition> pool = AffixPool.For(target);
            affixCount = Math.Min(affixCount, pool.Count);

            IEnumerable<AffixDefinition> chosen = pool.OrderBy(_ => Rng.Next()).Take(affixCount);
            foreach (AffixDefinition affix in chosen)
            {
                float value = affix.Roll(Rng);
                item.m_customData[AffixKeyPrefix + affix.Id] = value.ToString(CultureInfo.InvariantCulture);
            }
        }

        static bool IsArmorSlot(ItemDrop.ItemData.ItemType type)
        {
            return type == ItemDrop.ItemData.ItemType.Helmet
                || type == ItemDrop.ItemData.ItemType.Chest
                || type == ItemDrop.ItemData.ItemType.Legs
                || type == ItemDrop.ItemData.ItemType.Shoulder;
        }

        public static bool TryGetAffixValue(ItemDrop.ItemData item, string affixId, out float value)
        {
            value = 0f;
            if (item?.m_customData == null) return false;
            if (!item.m_customData.TryGetValue(AffixKeyPrefix + affixId, out string raw)) return false;
            return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        public static IEnumerable<KeyValuePair<string, float>> GetAllAffixes(ItemDrop.ItemData item)
        {
            if (item?.m_customData == null) yield break;

            foreach (KeyValuePair<string, string> kv in item.m_customData)
            {
                if (!kv.Key.StartsWith(AffixKeyPrefix, StringComparison.Ordinal)) continue;
                if (float.TryParse(kv.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                {
                    yield return new KeyValuePair<string, float>(kv.Key.Substring(AffixKeyPrefix.Length), value);
                }
            }
        }

        public static RarityTier? GetTier(ItemDrop.ItemData item)
        {
            if (item?.m_customData != null
                && item.m_customData.TryGetValue(TierKey, out string raw)
                && Enum.TryParse(raw, out RarityTier tier))
            {
                return tier;
            }
            return null;
        }
    }
}
