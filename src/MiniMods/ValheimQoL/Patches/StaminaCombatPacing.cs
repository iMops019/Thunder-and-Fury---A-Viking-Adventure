using HarmonyLib;

namespace ValheimQoL.Patches
{
    // Multiplies stamina cost for any action that calls Character.UseStamina —
    // attacks, blocking, sprinting, jumping, and dodging all route through
    // this single method, so this one patch covers all of them at once.
    [HarmonyPatch(typeof(Character), nameof(Character.UseStamina))]
    public static class StaminaDrainPatch
    {
        static void Prefix(ref float v)
        {
            v *= ValheimQoLPlugin.StaminaDrainMultiplier.Value;
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
