using HarmonyLib;

namespace ThunderFury.Core.Combat
{
    // ---- Generic "this weapon has a special on-hit effect" dispatch ----
    //
    // Reads the SAME per-instance m_customData mechanism Voltun's
    // Hatchet's log-yield bonus already relies on (confirmed via
    // decompile: ItemData.Clone() deep-copies m_customData, so setting a
    // key on an item's template ItemData -- not m_shared, which is
    // shared by every instance of that item type -- propagates it to
    // every real clone made from that prefab). Any current or future
    // weapon can opt into a special effect this way, not just one
    // hand-registered item.
    //
    // Character.RPC_Damage is the same real hook Defense's damage-
    // reduction patch already uses -- a Postfix here runs after damage
    // actually lands, which is the right time for an on-hit proc (a
    // Prefix would fire even if Defense's own reduction zeroed the hit
    // out, or before defense-scaling context prevents mis-triggering).
    [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
    public static class WeaponSpecialEffectPatch
    {
        public const string EffectKey = "ThunderFury_SpecialEffect";
        public const string ChainLightning = "ChainLightning";

        static void Postfix(Character __instance, HitData hit)
        {
            if (hit == null) return;
            if (!(hit.GetAttacker() is Humanoid attacker)) return;

            ItemDrop.ItemData weapon = attacker.RightItem;
            if (weapon?.m_customData == null) return;
            if (!weapon.m_customData.TryGetValue(EffectKey, out string effect)) return;

            switch (effect)
            {
                case ChainLightning:
                    ChainLightningEffect.TryTrigger(__instance, attacker);
                    break;
            }
        }
    }
}
