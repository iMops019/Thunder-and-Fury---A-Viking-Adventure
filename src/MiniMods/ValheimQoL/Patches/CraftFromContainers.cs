using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;

namespace ValheimQoL.Patches
{
    // ---- Craft from nearby containers ----
    //
    // Confirmed against the real 1.0 decompile: both the "can I
    // build/craft this" checks and the actual material consumption go
    // through Player.m_inventory (Inventory.CountItems/HaveItem for
    // checks, Inventory.RemoveItem for consumption — both just iterate
    // Inventory.m_inventory, a plain public List<ItemDrop.ItemData>).
    // Since ItemData objects are shared by reference, temporarily
    // appending nearby containers' ItemData objects into the player's own
    // list lets vanilla's own check/consume code "see" and spend them
    // without reimplementing any crafting logic — RemoveItem decrements
    // m_stack in place (visible from the container's list too, same
    // object) and only strips fully-consumed entries from whichever
    // list's RemoveItem was called on, so a Postfix reconciles borrowed
    // items back out afterward: still present in the player's list means
    // untouched/partially spent (leave it, the container already sees the
    // updated stack via the shared reference); missing means fully
    // consumed (remove it from the origin container's list too).
    //
    // Covers: piece placement (Player.HaveRequirements(Piece,...) +
    // ConsumeResources) and standard multi-ingredient recipe crafting
    // (Player.HaveRequirementItems + ConsumeResources, both called from
    // InventoryGui.DoCrafting). NOT covered: recipes flagged "require
    // only one ingredient" — those consume via a direct
    // Inventory.RemoveItem call in InventoryGui.DoCrafting instead of
    // ConsumeResources, and RemoveItem is used far too broadly elsewhere
    // (eating, dropping, repairing, ...) to safely patch on its own
    // without risking container items leaking into unrelated inventory
    // operations. That recipe flag is uncommon — most crafting/building
    // goes through the two covered paths.
    //
    // First cut — rescans nearby containers with a fresh
    // Physics.OverlapSphere on every check/consume call rather than
    // caching per-frame. Revisit for perf once actually felt out
    // in-game; the crafting UI may call the check path once per visible
    // recipe.
    public static class CraftFromContainers
    {
        static readonly Dictionary<ItemDrop.ItemData, Inventory> s_borrowed = new Dictionary<ItemDrop.ItemData, Inventory>();

        public static void Borrow(Player player)
        {
            s_borrowed.Clear();

            float radius = ValheimQoLPlugin.CraftFromContainersRadius.Value;
            if (radius <= 0f) return;

            long playerID = Game.instance.GetPlayerProfile().GetPlayerID();
            Vector3 pos = ((Component)player).transform.position;

            foreach (var collider in Physics.OverlapSphere(pos, radius))
            {
                var container = collider.GetComponentInParent<Container>();
                if (container == null) continue;
                if (container.m_checkGuardStone && !PrivateArea.CheckAccess(((Component)container).transform.position, 0f, false)) continue;
                if (!container.CheckAccess(playerID)) continue;

                var inv = container.GetInventory();
                if (inv == null) continue;

                foreach (var item in inv.m_inventory)
                {
                    if (s_borrowed.ContainsKey(item)) continue;
                    player.m_inventory.m_inventory.Add(item);
                    s_borrowed[item] = inv;
                }
            }
        }

        public static void Return(Player player)
        {
            foreach (var kv in s_borrowed)
            {
                var item = kv.Key;
                var originInventory = kv.Value;
                if (player.m_inventory.m_inventory.Contains(item))
                {
                    player.m_inventory.m_inventory.Remove(item);
                }
                else
                {
                    originInventory.m_inventory.Remove(item);
                    originInventory.Changed();
                }
            }
            s_borrowed.Clear();
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.HaveRequirementItems))]
    public static class CraftFromContainers_HaveRequirementItems
    {
        static void Prefix(Player __instance) => CraftFromContainers.Borrow(__instance);
        static void Postfix(Player __instance) => CraftFromContainers.Return(__instance);
    }

    [HarmonyPatch(typeof(Player), nameof(Player.HaveRequirements), typeof(Piece), typeof(Player.RequirementMode))]
    public static class CraftFromContainers_HaveRequirementsPiece
    {
        static void Prefix(Player __instance) => CraftFromContainers.Borrow(__instance);
        static void Postfix(Player __instance) => CraftFromContainers.Return(__instance);
    }

    [HarmonyPatch(typeof(Player), nameof(Player.ConsumeResources))]
    public static class CraftFromContainers_ConsumeResources
    {
        static void Prefix(Player __instance) => CraftFromContainers.Borrow(__instance);
        static void Postfix(Player __instance) => CraftFromContainers.Return(__instance);
    }
}
