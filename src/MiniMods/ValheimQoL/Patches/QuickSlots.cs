namespace ValheimQoL.Patches
{
    // ---- Quick Slots for gear/potions ----
    //
    // Goal: dedicated hotkeys (e.g. number keys 6-9, or a modifier+number)
    // that instantly equip/use a chosen item, separate from the normal
    // inventory hotbar.
    //
    // Plan:
    //   1. Config entries per slot using BepInEx's built-in
    //      ConfigEntry<KeyboardShortcut> type — remappable for free
    //   2. A small mapping of slot -> assigned item, saved per-character
    //   3. On key press: find the assigned item in the player's inventory,
    //      call the same equip/consume logic the game uses when you click
    //      an inventory slot (exact method names to confirm against 1.0)
    //   4. Small on-screen UI showing what's assigned to each slot, built
    //      with Jotunn's UI helpers
    //
    // UI + input + inventory logic together — the most involved of the
    // "simple" QoL features. Good candidate for after items 1-3 in the
    // build order are working and tested.
    public static class QuickSlotsPlan { }
}
