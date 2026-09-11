using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThunderFury.DevTool.Data
{
    // ---- Live catalog of every item in the game ----
    //
    // Deliberately built from ObjectDB.instance.m_items at the moment
    // it's needed, not a hand-typed list -- today's session already
    // proved hardcoded prefab-name guesses go stale the moment the game
    // updates (Hatchet/Knife/MushroomYellow all failed after a same-day
    // Valheim patch). Reading the game's own live item registry is
    // self-updating by construction: whatever the actual running game
    // considers a real item, this sees too, no maintenance needed.
    //
    // Confirmed ObjectDB.m_items is a real public field on the actual
    // (non-publicized) game assembly, not just the local dev reference.
    public static class VanillaItemCatalog
    {
        public class Entry
        {
            public string PrefabName;
            public string DisplayName;
            public ItemDrop.ItemData.ItemType ItemType;
            public bool IsWeapon;
            public bool IsArmor;
        }

        public static List<Entry> GetAll()
        {
            var result = new List<Entry>();
            if (ObjectDB.instance == null) return result;

            foreach (GameObject prefab in ObjectDB.instance.m_items)
            {
                if (prefab == null) continue;

                ItemDrop itemDrop = prefab.GetComponent<ItemDrop>();
                if (itemDrop == null || itemDrop.m_itemData?.m_shared == null) continue;

                ItemDrop.ItemData.SharedData shared = itemDrop.m_itemData.m_shared;
                result.Add(new Entry
                {
                    PrefabName = prefab.name,
                    DisplayName = string.IsNullOrEmpty(shared.m_name) ? prefab.name : shared.m_name,
                    ItemType = shared.m_itemType,
                    IsWeapon = itemDrop.m_itemData.IsWeapon(),
                    IsArmor = IsArmorSlot(shared.m_itemType),
                });
            }

            result.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));
            return result;
        }

        static bool IsArmorSlot(ItemDrop.ItemData.ItemType type)
        {
            return type == ItemDrop.ItemData.ItemType.Helmet
                || type == ItemDrop.ItemData.ItemType.Chest
                || type == ItemDrop.ItemData.ItemType.Legs
                || type == ItemDrop.ItemData.ItemType.Shoulder;
        }
    }
}
