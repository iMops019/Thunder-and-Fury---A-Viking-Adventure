using HarmonyLib;

namespace ValheimQoL.Patches
{
    // ---- Building snap/placement tolerance ----
    //
    // Confirmed against the real 1.0 decompile (Player class):
    //  - Piece placement distance is a plain public field, m_maxPlaceDistance
    //    (vanilla default 5f), set once in Player's constructor. A
    //    Postfix on the constructor overrides it per-player on spawn.
    //  - Snap-point matching distance is a method parameter, not a field:
    //    Player.FindClosestSnapPoints(Transform ghost, float maxSnapDistance, ...)
    //    is called with a hardcoded 0.5f at its one call site
    //    (UpdatePlacementGhost). Harmony can still adjust it with a plain
    //    Prefix by declaring the parameter `ref` — no Transpiler needed,
    //    despite what the pre-decompile plan here used to assume.

    // Widens/narrows how far from the player pieces can be placed.
    [HarmonyPatch(typeof(Player), MethodType.Constructor)]
    public static class PlaceDistancePatch
    {
        static void Postfix(Player __instance)
        {
            __instance.m_maxPlaceDistance *= ValheimQoLPlugin.PlaceDistanceMultiplier.Value;
        }
    }

    // Widens/narrows how close two pieces' snap points need to be before
    // the placement ghost snaps to the existing structure.
    [HarmonyPatch(typeof(Player), nameof(Player.FindClosestSnapPoints))]
    public static class SnapTolerancePatch
    {
        static void Prefix(ref float maxSnapDistance)
        {
            maxSnapDistance *= ValheimQoLPlugin.BuildSnapTolerance.Value;
        }
    }
}
