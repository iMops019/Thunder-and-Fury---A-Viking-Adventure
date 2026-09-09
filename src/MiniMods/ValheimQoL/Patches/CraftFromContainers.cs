namespace ValheimQoL.Patches
{
    // ---- Craft from nearby containers ----
    //
    // Goal: crafting checks nearby chests (within a config-driven radius)
    // for missing materials, not just the player's own inventory.
    //
    // Plan:
    //   1. Find all Container components within range of the player
    //      (Physics.OverlapSphere against the right layer)
    //   2. Patch the "do I have the materials for this recipe" check
    //      (likely on the crafting UI or Player's crafting logic) to also
    //      search those containers' inventories
    //   3. Patch the actual material-consumption step the same way, so it
    //      pulls from containers, not just the player's inventory
    //
    // Touches both the UI layer (showing what you CAN craft) and the
    // consumption logic (actually taking the materials) — the most
    // involved of the six QoL features. Last in the build order.
    public static class CraftFromContainersPlan { }
}
