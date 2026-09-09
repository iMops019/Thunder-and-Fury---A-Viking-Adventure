namespace ValheimQoL.Patches
{
    // ---- Building snap/placement tolerance ----
    //
    // The snap tolerance lives inside Player's placement-ghost logic as a
    // value used mid-method, not a simple field we can multiply from
    // outside — a plain Harmony Prefix/Postfix can't reach it. Once we can
    // see the actual 1.0 code, this needs one of:
    //   1. A Harmony Transpiler that finds the tolerance value in the
    //      method's IL and swaps it for our config value
    //   2. If it turns out to live in an exposed field instead, a much
    //      simpler Prefix/Postfix
    //
    // Leaving this as a stub rather than writing a patch that compiles but
    // silently does nothing — worth confirming which case we're in first.
    public static class BuildingSnapPlan { }
}
