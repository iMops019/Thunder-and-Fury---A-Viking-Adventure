using HarmonyLib;
using UnityEngine;
using ThunderFury.Core.SkillSystem;

namespace ThunderFury.Core.Patches
{
    // ---- Cooking: reduced burn/fail chance ----
    //
    // Confirmed against the real 1.0 decompile: CookingStation.UpdateCooking
    // marks a slot Burnt purely on a fixed time threshold
    // (`cookedTime > itemConversion.m_cookTime * 2f`), then calls
    // `SetSlot(slot, m_overCookedItem.name, cookedTime, Status.Burnt)` --
    // no skill factor involved anywhere in that decision. Rather than
    // replicate UpdateCooking's own private iteration to intercept the
    // burn decision at its source (fragile -- risks drifting from the
    // real logic over time), this Prefixes SetSlot itself: when a slot
    // is about to be set to Burnt, roll a Cooking-level-scaled chance to
    // redirect it to Done instead, using whatever was already in that
    // slot (GetSlot + GetItemConversion -- the same lookup vanilla
    // itself uses, which matches by either the raw ingredient's name or
    // the finished dish's name) to figure out what the "saved" result
    // should be.
    //
    // "Whose" Cooking level applies is the closest player to the
    // station, same approximation Fishing uses for bite chance --
    // there's no per-slot ownership tracked by vanilla to know who
    // actually put the food on, and UpdateCooking runs on the station's
    // own repeating timer with no player context at all.
    //
    // Known cosmetic gap, not a functional one: UpdateCooking plays its
    // burnt particle/sound effect unconditionally before calling
    // SetSlot, so a "saved" cook still briefly flashes the burnt effect
    // even though the food ends up fine. Fixing that would mean
    // replicating UpdateCooking's own logic instead of this simpler
    // SetSlot-level intercept -- not worth the added fragility for a
    // visual-only quirk.
    [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.SetSlot))]
    public static class CookingBurnPreventionPatch
    {
        static void Prefix(CookingStation __instance, int slot, ref string itemName, ref CookingStation.Status status)
        {
            if (status != CookingStation.Status.Burnt) return;

            Player nearby = Player.GetClosestPlayer(__instance.transform.position, CorePlugin.CookingBurnPreventionRadius.Value);
            if (nearby == null) return;

            float skillFactor = nearby.GetSkillFactor(CookingSkill.Type);
            if (skillFactor <= 0f) return;
            if (Random.value >= skillFactor * CorePlugin.CookingBurnPreventionChanceAtMaxLevel.Value) return;

            __instance.GetSlot(slot, out string currentItemName, out _, out _);
            CookingStation.ItemConversion conversion = __instance.GetItemConversion(currentItemName);
            if (conversion == null || conversion.m_to == null) return;

            itemName = conversion.m_to.name;
            status = CookingStation.Status.Done;
        }
    }
}
