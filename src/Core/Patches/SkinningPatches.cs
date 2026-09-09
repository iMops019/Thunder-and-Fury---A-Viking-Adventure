using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Jotunn.Managers;
using UnityEngine;
using VikingAdventure.Core.SkillSystem;

namespace VikingAdventure.Core.Patches
{
    // ---- Skinning: carcasses instead of auto-drop ----
    //
    // docs/valheim-mod-vision.md: "animal deaths no longer auto-drop
    // hide/raw meat -- instead they leave a carcass that requires a
    // Skinning Knife to harvest." First-cut scope, registered per animal
    // (Deer only for now, same "build generic, ship minimal content"
    // pattern as RarityLoot's Voltun's Set): only the MEAT drop entry is
    // pulled out of the kill and moved into a carcass -- hide and trophy
    // still drop normally. Multi-item carcasses (meat + hide together)
    // would need a custom container-based carcass instead of reusing
    // vanilla's Pickable, which is single-item; deferred rather than
    // guessed at.
    //
    // Confirmed against the real 1.0 decompile: Pickable already has
    // everything needed to make a harvestable resource node --
    // Hoverable/Interactable, ZNetView-synced picked state,
    // m_pickRaiseSkill (raises any SkillType on pick, including a Jotunn
    // custom one -- no custom XP code needed), and
    // m_maxLevelBonusChance/m_bonusYieldAmount (a free bonus-yield roll
    // scaled by that skill's level). Cloning an existing Pickable-based
    // resource prefab and reconfiguring it is far less work and far more
    // proven than building a new interactable prefab from scratch.
    //
    // Tool-gating (must hold a Skinning Knife) isn't part of vanilla
    // Pickable at all -- that's PickableToolGatePatch below, scoped to
    // only the Pickable instances this system itself spawned (tracked by
    // reference, not name matching, so it can't accidentally gate an
    // unrelated Pickable).
    //
    // Verification caveat, same shape as StonePickaxe's: "MushroomYellow"
    // as the cloned base prefab and "RawMeat" as Deer's meat drop are
    // standard, well-established Jotunn/Valheim names, not independently
    // confirmed against this install's binary asset data (which lives in
    // Unity asset bundles, not the decompiled C# assembly). Safe failure
    // mode if wrong: Jotunn logs a clear error on load.
    public static class SkinningSystem
    {
        public class AnimalEntry
        {
            public string CarcassPrefabName;
        }

        static readonly Dictionary<string, AnimalEntry> ByCreatureName = new Dictionary<string, AnimalEntry>();
        static readonly HashSet<Pickable> CarcassInstances = new HashSet<Pickable>();

        // basePickablePrefabName: an existing vanilla Pickable-based
        // resource to clone structurally (collider, ZNetView, Pickable
        // component) -- its own visual is a placeholder until this gets
        // real art, flagged rather than pretending it's finished.
        public static void RegisterAnimal(string creaturePrefabName, string basePickablePrefabName, string meatPrefabName, int amount)
        {
            string carcassName = "Carcass_" + creaturePrefabName;

            GameObject carcass = PrefabManager.Instance.CreateClonedPrefab(carcassName, basePickablePrefabName);
            if (carcass == null)
            {
                Jotunn.Logger.LogError($"Skinning: failed to clone carcass prefab for {creaturePrefabName} from {basePickablePrefabName}, skipping");
                return;
            }

            Pickable pickable = carcass.GetComponent<Pickable>();
            if (pickable == null)
            {
                Jotunn.Logger.LogError($"Skinning: base prefab {basePickablePrefabName} has no Pickable component, skipping {creaturePrefabName}");
                return;
            }

            GameObject meatPrefab = PrefabManager.Instance.GetPrefab(meatPrefabName);
            if (meatPrefab == null)
            {
                Jotunn.Logger.LogError($"Skinning: could not resolve meat prefab '{meatPrefabName}', skipping {creaturePrefabName}");
                return;
            }

            pickable.m_itemPrefab = meatPrefab;
            pickable.m_amount = amount;
            pickable.m_dontScale = true;
            pickable.m_respawnTimeMinutes = 0f;
            pickable.m_pickRaiseSkill = SkinningSkill.Type;

            PrefabManager.Instance.AddPrefab(carcass);

            ByCreatureName[creaturePrefabName] = new AnimalEntry { CarcassPrefabName = carcassName };
        }

        public static bool TryGetAnimal(string creaturePrefabName, out AnimalEntry entry)
        {
            return ByCreatureName.TryGetValue(creaturePrefabName, out entry);
        }

        public static void SpawnCarcass(AnimalEntry entry, Vector3 position)
        {
            GameObject prefab = PrefabManager.Instance.GetPrefab(entry.CarcassPrefabName);
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
            RegisterAnimal("Deer", "MushroomYellow", "RawMeat", 2);
        }
    }

    // Pulls the meat entry out of a registered animal's drop list before
    // vanilla's own CharacterDrop.OnDeath spawns it, then spawns a
    // carcass with that meat instead. Hide/trophy entries are left in
    // m_drops untouched, so they still drop normally.
    [HarmonyPatch(typeof(CharacterDrop), nameof(CharacterDrop.OnDeath))]
    public static class SkinningCarcassSpawnPatch
    {
        static void Prefix(CharacterDrop __instance, out CharacterDrop.Drop __state)
        {
            __state = null;
            if (!__instance.m_dropsEnabled || __instance.m_character == null) return;

            string creatureName = __instance.m_character.m_nview != null
                ? __instance.m_character.m_nview.GetPrefabName()
                : null;
            if (creatureName == null || !SkinningSystem.TryGetAnimal(creatureName, out SkinningSystem.AnimalEntry entry)) return;

            GameObject meatPrefab = PrefabManager.Instance.GetPrefab(EntryMeatName(entry));
            CharacterDrop.Drop meatDrop = __instance.m_drops.FirstOrDefault(d => d.m_prefab != null && meatPrefab != null && d.m_prefab.name == meatPrefab.name);
            if (meatDrop == null) return;

            __instance.m_drops.Remove(meatDrop);
            __state = meatDrop;

            Vector3 pos = __instance.m_character.GetCenterPoint();
            SkinningSystem.SpawnCarcass(entry, pos);
        }

        static void Postfix(CharacterDrop __instance, CharacterDrop.Drop __state)
        {
            if (__state != null) __instance.m_drops.Add(__state);
        }

        // Small helper since AnimalEntry only stores the carcass prefab
        // name (the meat prefab is baked into the carcass's own Pickable
        // already) -- re-derives which drop entry to pull by reading it
        // back off the spawned carcass prefab rather than storing it
        // twice.
        static string EntryMeatName(SkinningSystem.AnimalEntry entry)
        {
            GameObject carcass = PrefabManager.Instance.GetPrefab(entry.CarcassPrefabName);
            Pickable pickable = carcass != null ? carcass.GetComponent<Pickable>() : null;
            return pickable != null && pickable.m_itemPrefab != null ? pickable.m_itemPrefab.name : null;
        }
    }

    // Blocks harvesting a carcass without a Skinning Knife equipped --
    // scoped to only the Pickable instances SkinningSystem itself
    // spawned, so this can't affect any unrelated vanilla Pickable
    // (mushrooms, berries, etc.).
    [HarmonyPatch(typeof(Pickable), nameof(Pickable.Interact))]
    public static class PickableToolGatePatch
    {
        static bool Prefix(Pickable __instance, Humanoid character)
        {
            if (!SkinningSystem.IsCarcass(__instance)) return true;

            ItemDrop.ItemData weapon = character.RightItem;
            bool hasKnife = weapon?.m_dropPrefab != null && weapon.m_dropPrefab.name == SkinningSkill.SkinningKnifePrefabName;
            if (hasKnife) return true;

            character.Message(MessageHud.MessageType.Center, "You need a Skinning Knife to harvest this.");
            return false;
        }
    }
}
