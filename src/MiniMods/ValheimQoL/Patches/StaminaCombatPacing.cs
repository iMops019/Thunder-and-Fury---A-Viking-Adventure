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

    // Stamina REGEN isn't a single clean entry point the way drain is — it's
    // calculated inside Character's per-frame update, mixed in with health
    // regen and status-effect checks, and the method name has moved between
    // Valheim versions. Rather than guess a name that might not exist in the
    // 1.0 build and hand you code that silently fails to compile, this is
    // left as a flagged TODO — first thing to nail down once we can see the
    // actual 1.0 decompile.
    //
    // [HarmonyPatch(typeof(Character), "MethodNameTBD")]
    // public static class StaminaRegenPatch { ... }
}
