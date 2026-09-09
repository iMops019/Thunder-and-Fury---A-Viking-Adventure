using System.Text;
using HarmonyLib;
using VikingAdventure.RarityLoot.Affixes;

namespace VikingAdventure.RarityLoot.Patches
{
    // Reads a rolled item's own m_customData and adds the bonus on top of
    // vanilla's own calculation -- never touches ItemData.m_shared (see
    // ItemRoller's header comment for why). Confirmed against the real
    // 1.0 decompile: ItemData.GetArmor(int, float) and
    // GetDamage(int, float) are the actual per-instance calculations
    // (their 0-arg overloads just call these with the item's own
    // quality/worldLevel), so patching these two covers every caller.

    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetArmor), typeof(int), typeof(float))]
    public static class ArmorAffixPatch
    {
        static void Postfix(ItemDrop.ItemData __instance, ref float __result)
        {
            if (ItemRoller.TryGetAffixValue(__instance, AffixPool.ArmorAffixId, out float bonus))
            {
                __result += bonus;
            }
        }
    }

    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetDamage), typeof(int), typeof(float))]
    public static class DamageAffixPatch
    {
        static void Postfix(ItemDrop.ItemData __instance, ref HitData.DamageTypes __result)
        {
            if (!ItemRoller.TryGetAffixValue(__instance, AffixPool.DamageAffixId, out float pct)) return;

            float mult = 1f + pct / 100f;
            __result.m_damage *= mult;
            __result.m_blunt *= mult;
            __result.m_slash *= mult;
            __result.m_pierce *= mult;
            __result.m_chop *= mult;
            __result.m_pickaxe *= mult;
            __result.m_fire *= mult;
            __result.m_frost *= mult;
            __result.m_lightning *= mult;
            __result.m_poison *= mult;
        }
    }

    // Max health isn't per-item (there's no "armor" style summation point
    // for it), so this scans the player's own currently-equipped items --
    // confirmed ItemData.m_equipped is the real flag vanilla itself sets
    // on equip/unequip. Character.GetMaxHealth is called very frequently
    // for every character in the game (monsters included), so the
    // `is Player` check has to come first and be cheap for the common
    // case.
    [HarmonyPatch(typeof(Character), nameof(Character.GetMaxHealth))]
    public static class LifeAffixPatch
    {
        static void Postfix(Character __instance, ref float __result)
        {
            if (!(__instance is Player player)) return;

            foreach (ItemDrop.ItemData item in player.m_inventory.m_inventory)
            {
                if (!item.m_equipped) continue;
                if (ItemRoller.TryGetAffixValue(item, AffixPool.LifeAffixId, out float bonus))
                {
                    __result += bonus;
                }
            }
        }
    }

    // Appends rarity + rolled affix lines to the item's own tooltip.
    // First cut -- only patches ItemData's own GetTooltip, not every
    // comparison/hover-panel variant elsewhere in the UI.
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip), typeof(int))]
    public static class TooltipAffixPatch
    {
        static readonly System.Collections.Generic.Dictionary<RarityTier, string> TierColors =
            new System.Collections.Generic.Dictionary<RarityTier, string>
            {
                { RarityTier.Magic, "#8888FF" },
                { RarityTier.Rare, "#FFFF77" },
                { RarityTier.Legendary, "#FF8000" },
            };

        static void Postfix(ItemDrop.ItemData __instance, ref string __result)
        {
            RarityTier? tier = ItemRoller.GetTier(__instance);
            if (tier == null || tier == RarityTier.Normal) return;

            var sb = new StringBuilder();
            string color = TierColors.TryGetValue(tier.Value, out string c) ? c : "#FFFFFF";
            sb.Append("\n<color=").Append(color).Append('>').Append(tier.Value).Append("</color>");

            foreach (var kv in ItemRoller.GetAllAffixes(__instance))
            {
                AffixDefinition def = AffixPool.Get(kv.Key);
                if (def == null) continue;
                sb.Append('\n').Append(string.Format(def.DisplayFormat, kv.Value));
            }

            __result += sb.ToString();
        }
    }
}
