using HarmonyLib;

namespace ValheimQoL.Patches
{
    // Multiplies stamina cost for any action that calls Character.UseStamina —
    // attacks, blocking, sprinting, jumping, and dodging all route through
    // this single method, so this one patch covers all of them at once.
    [HarmonyPatch(typeof(Character), nameof(Character.UseStamina))]
    public static class StaminaDrainPatch
    {
        // Harmony wires up a ref-parameter Prefix by matching the
        // ORIGINAL method's parameter name exactly -- confirmed the hard
        // way in-game (2026-09-10): the real vanilla parameter is named
        // "stamina", not "v", and Harmony fails to patch at all
        // ("Parameter 'v' not found") if the names don't match, silently
        // leaving this whole patch (and everything routed through it:
        // attacks, blocking, sprinting, jumping, dodging) un-applied.
        static void Prefix(ref float stamina)
        {
            stamina *= ValheimQoLPlugin.StaminaDrainMultiplier.Value;
        }
    }

    // Stamina regen's actual formula lives inline inside Player.UpdateStats,
    // mixed in with food/adrenaline updates — not independently patchable.
    // But confirmed against the real 1.0 decompile: the base rate it reads
    // from, Player.m_staminaRegen, is a plain public field set once in
    // Player's constructor (default 5f) — same story as BuildingSnap's
    // m_maxPlaceDistance. A constructor Postfix is enough, no Transpiler
    // needed despite what this file used to assume before the decompile
    // was available.
    [HarmonyPatch(typeof(Player), MethodType.Constructor)]
    public static class StaminaRegenPatch
    {
        static void Postfix(Player __instance)
        {
            __instance.m_staminaRegen *= ValheimQoLPlugin.StaminaRegenMultiplier.Value;
        }
    }
}
