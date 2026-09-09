namespace ValheimQoL.Patches
{
    // ---- Auto-pickup / Auto-sort ----
    //
    // Auto-pickup: extend the player's existing pickup detection so items
    // on the ground go straight to inventory without a manual click.
    //
    // Auto-sort: a "sort inventory" action (keybind or button) that
    // reorders the grid by item type/category and merges partial stacks.
    // Needs a stable sort key (type, then name) and a way to rewrite the
    // Inventory's item list/positions, then refresh the UI.
    //
    // Both are inventory-list manipulation rather than UI-heavy work —
    // should be more approachable than Quick Slots once the config-driven
    // patches (1-3) are done and tested.
    public static class AutoPickupSortPlan { }
}
