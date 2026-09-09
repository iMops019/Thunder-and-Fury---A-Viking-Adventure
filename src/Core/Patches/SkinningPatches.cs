using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Jotunn.Managers;
using UnityEngine;
using VikingAdventure.Core.SkillSystem;

namespace VikingAdventure.Core.Patches
{
    // ---- Skinning and Butchering: carcasses instead of auto-drop ----
    //
    // docs/valheim-mod-vision.md's original framing plus this session's
    // clarification: skinning (hide) and butchering (meat) are two
    // separate harvests, not one "harvest everything" action, and the
    // tool requirement is any basic knife/dagger a player can craft --
    // not one specific named item. A dead registered animal leaves TWO
    // carcass pieces at the kill site, spawned together: one yields hide
    // when interacted with, the other yields meat. Either can be
    // harvested independently, in any order. Anything else that
    // wasn't a Skinning/Butchering item to begin with (trophies, etc.)
    // is untouched and still drops normally.
    //
    // Confirmed against the real 1.0 decompile: Pickable already has
    // everything needed to make a harvestable resource node --
    // Hoverable/Interactable, ZNetView-synced picked state,
    // m_pickRaiseSkill (raises any SkillType on pick, including a Jotunn
    // custom one -- no custom XP code needed), and
    // m_maxLevelBonusChance/m_bonusYieldAmount (a free bonus-yield roll
    // scaled by that skill's level). Since Pickable only ever grants ONE
    // item type, two carcass pieces (cloned from the same base resource
    // prefab, each configured with a different item) is what "skin OR
    // butcher independently" actually requires -- cleaner than one
    // Pickable trying to hold two different loot outcomes.
    //
    // Tool-gating: any equipped item whose weapon-skill type is
    // Skills.SkillType.Knives qualifies -- vanilla's starting Knife
    // included, so a player may already own a valid tool without
    // crafting anything new. RarityLoot's Skinning Knife
    // (Items/SkinningKnife.cs) is one example that satisfies this, not
    // the only one; it isn't checked by exact identity anymore.
    //
    // Verification caveat, same shape as StonePickaxe's: "MushroomYellow"
    // as the cloned base prefab and "RawMeat"/"DeerHide" as Deer's drops
    // are standard, well-established Jotunn/Valheim names, not
    // independently confirmed against this install's binary asset data
    // (which lives in Unity asset bundles, not the decompiled C#
    // assembly). Safe failure mode if wrong: Jotunn logs a clear error
    // on load.
    public static class SkinningSystem
    {
        public class AnimalEntry
        {
            public string HideCarcassPrefabName;
            public string MeatCarcassPrefabName;
            public string HideItemName;
            public string MeatItemName;
        }

        static readonly Dictionary<string, AnimalEntry> ByCreatureName = new Dictionary<string, AnimalEntry>();
        static readonly HashSet<Pickable> CarcassInstances = new HashSet<Pickable>();

        static readonly Vector3 HidePieceOffset = new Vector3(0.3f, 0f, 0f);
        static readonly Vector3 MeatPieceOffset = new Vector3(-0.3f, 0f, 0f);

        // basePickablePrefabName: an existing vanilla Pickable-based
        // resource to clone structurally (collider, ZNetView, Pickable
        // component) -- its own visual is a placeholder until this gets
        // real art, flagged rather than pretending it's finished.
        public static void RegisterAnimal(string creaturePrefabName, string basePickablePrefabName,
            string hideItemName, int hideAmount, string meatItemName, int meatAmount)
        {
            string hideCarcassName = "Carcass_" + creaturePrefabName + "_Hide";
            string meatCarcassName = "Carcass_" + creaturePrefabName + "_Meat";

            GameObject hideCarcass = CreateCarcassPiece(hideCarcassName, basePickablePrefabName, hideItemName, hideAmount, creaturePrefabName);
            GameObject meatCarcass = CreateCarcassPiece(meatCarcassName, basePickablePrefabName, meatItemName, meatAmount, creaturePrefabName);
            if (hideCarcass == null || meatCarcass == null) return;

            ByCreatureName[creaturePrefabName] = new AnimalEntry
            {
                HideCarcassPrefabName = hideCarcassName,
                MeatCarcassPrefabName = meatCarcassName,
                HideItemName = hideItemName,
                MeatItemName = meatItemName,
            };
        }

        static GameObject CreateCarcassPiece(string carcassName, string basePickablePrefabName, string itemName, int amount, string creaturePrefabName)
        {
            GameObject carcass = PrefabManager.Instance.CreateClonedPrefab(carcassName, basePickablePrefabName);
            if (carcass == null)
            {
                Jotunn.Logger.LogError($"Skinning: failed to clone carcass prefab '{carcassName}' from {basePickablePrefabName}, skipping {creaturePrefabName}");
                return null;
            }

            Pickable pickable = carcass.GetComponent<Pickable>();
            if (pickable == null)
            {
                Jotunn.Logger.LogError($"Skinning: base prefab {basePickablePrefabName} has no Pickable component, skipping {creaturePrefabName}");
                return null;
            }

            GameObject itemPrefab = PrefabManager.Instance.GetPrefab(itemName);
            if (itemPrefab == null)
            {
                Jotunn.Logger.LogError($"Skinning: could not resolve item prefab '{itemName}', skipping {creaturePrefabName}");
                return null;
            }

            pickable.m_itemPrefab = itemPrefab;
            pickable.m_amount = amount;
            pickable.m_dontScale = true;
            pickable.m_respawnTimeMinutes = 0f;
            pickable.m_pickRaiseSkill = SkinningSkill.Type;

            PrefabManager.Instance.AddPrefab(carcass);
            return carcass;
        }

        public static bool TryGetAnimal(string creaturePrefabName, out AnimalEntry entry)
        {
            return ByCreatureName.TryGetValue(creaturePrefabName, out entry);
        }

        public static void SpawnCarcass(AnimalEntry entry, Vector3 position)
        {
            SpawnPiece(entry.HideCarcassPrefabName, position + HidePieceOffset);
            SpawnPiece(entry.MeatCarcassPrefabName, position + MeatPieceOffset);
        }

        static void SpawnPiece(string carcassPrefabName, Vector3 position)
        {
            GameObject prefab = PrefabManager.Instance.GetPrefab(carcassPrefabName);
            if (prefab == null) return;

            GameObject instance = Object.Instantiate(prefab, position, Quaternion.identity);
            Pickable pickable = instance.GetComponent<Pickable>();
            if (pickable != null) CarcassInstances.Add(pickable);
        }

        public static bool IsCarcass(Pickable pickable)
        {
            return pickable != null && CarcassInstances.Contains(pickable);
        }

        // First-cut content: Deer only, proving the mechanic before
        // spending time on every other animal. Adding Boar/Wolf/Neck
        // later is one RegisterAnimal call each, no new plumbing.
        public static void RegisterDefaults()
        {
            RegisterAnimal("Deer", "MushroomYellow", "DeerHide", 1, "RawMeat", 2);
        }
    }

    // Pulls the hide and meat entries out of a registered animal's drop
    // list before vanilla's own CharacterDrop.OnDeath spawns them, then
    // spawns the two carcass pieces instead. Anything else in m_drops
    // (trophies, etc.) is left untouched and still drops normally.
    [HarmonyPatch(typeof(CharacterDrop), nameof(CharacterDrop.OnDeath))]
    public static class SkinningCarcassSpawnPatch
    {
        static void Prefix(CharacterDrop __instance, out List<CharacterDrop.Drop> __state)
        {
            __state = null;
            if (!__instance.m_dropsEnabled || __instance.m_character == null) return;

            string creatureName = __instance.m_character.m_nview != null
                ? __instance.m_character.m_nview.GetPrefabName()
                : null;
            if (creatureName == null || !SkinningSystem.TryGetAnimal(creatureName, out SkinningSystem.AnimalEntry entry)) return;

            List<CharacterDrop.Drop> removed = __instance.m_drops
                .Where(d => d.m_prefab != null && (d.m_prefab.name == entry.HideItemName || d.m_prefab.name == entry.MeatItemName))
                .ToList();
            if (removed.Count == 0) return;

            foreach (CharacterDrop.Drop drop in removed) __instance.m_drops.Remove(drop);
            __state = removed;

            Vector3 pos = __instance.m_character.GetCenterPoint();
            SkinningSystem.SpawnCarcass(entry, pos);
        }

        static void Postfix(CharacterDrop __instance, List<CharacterDrop.Drop> __state)
        {
            if (__state != null) __instance.m_drops.AddRange(__state);
        }
    }

    // Blocks skinning/butchering without a knife equipped -- any weapon
    // in the vanilla Knives skill category qualifies, not one specific
    // item. Scoped to only the Pickable instances SkinningSystem itself
    // spawned, so this can't affect any unrelated vanilla Pickable
    // (mushrooms, berries, etc.).
    [HarmonyPatch(typeof(Pickable), nameof(Pickable.Interact))]
    public static class PickableToolGatePatch
    {
        static bool Prefix(Pickable __instance, Humanoid character)
        {
            if (!SkinningSystem.IsCarcass(__instance)) return true;

            ItemDrop.ItemData weapon = character.RightItem;
            bool hasKnife = weapon != null && weapon.m_shared.m_skillType == global::Skills.SkillType.Knives;
            if (hasKnife) return true;

            character.Message(MessageHud.MessageType.Center, "You need a knife or dagger to skin/butcher this.");
            return false;
        }
    }
}
