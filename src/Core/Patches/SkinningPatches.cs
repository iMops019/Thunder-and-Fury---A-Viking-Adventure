using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Jotunn.Managers;
using UnityEngine;
using ThunderFury.Core.SkillSystem;

namespace ThunderFury.Core.Patches
{
    // ---- Skinning and Butchering: carcasses instead of auto-drop ----
    //
    // docs/valheim-mod-vision.md's original framing plus this session's
    // clarification: skinning (hide) and butchering (meat) are two
    // separate harvests, not one "harvest everything" action, and the
    // tool requirement is any basic knife/dagger a player can craft --
    // not one specific named item. A dead registered animal leaves up to
    // TWO carcass pieces at the kill site, spawned together: one yields
    // hide when interacted with, the other yields meat (an animal with
    // only one of the two, e.g. Boar with no hide item, just gets one
    // piece). Either can be harvested independently, in any order.
    // Anything else that wasn't a Skinning/Butchering item to begin with
    // (trophies, etc.) is untouched and still drops normally.
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
    // butcher independently" actually requires -- no container-based
    // redesign needed, contrary to this system's own first-cut notes.
    //
    // Tool-gating: any equipped item whose weapon-skill type is
    // Skills.SkillType.Knives qualifies -- vanilla's starting Knife
    // included, so a player may already own a valid tool without
    // crafting anything new. RarityLoot's Skinning Knife
    // (Items/SkinningKnife.cs) is one example that satisfies this, not
    // the only one; it isn't checked by exact identity.
    //
    // IMPORTANT hook-point correction (found while researching this
    // session's carcass-visual question, by reading Character.OnDeath in
    // full for the first time): the original version of this patch
    // targeted CharacterDrop.OnDeath, but that's frequently too late to
    // matter. Character.OnDeath creates the death Ragdoll BEFORE calling
    // CharacterDrop.OnDeath, and Ragdoll.Setup immediately calls
    // characterDrop.GenerateDropList() to save a snapshot of the loot for
    // its own delayed drop-on-dissolve -- then, since Ragdoll.m_dropItems
    // defaults to true, Character.OnDeath disables CharacterDrop's drops
    // outright (CharacterDrop.SetDropsEnabled(false)) so it never fires
    // at all. Net effect: for any creature with a normal death ragdoll
    // (which is all of them), CharacterDrop.OnDeath either never runs or
    // runs against a drops-disabled component, so a Prefix there never
    // actually intercepts anything -- the ragdoll's own saved snapshot,
    // taken before the Prefix could run, still contains hide+meat and
    // still drops them later via Ragdoll.SpawnLoot. Patching
    // Character.OnDeath instead -- before the ragdoll is created at
    // all -- means the ragdoll's own snapshot is taken from the
    // already-filtered drop list, so this works whether or not the
    // ragdoll ends up disabling CharacterDrop.
    //
    // Verification caveat, same shape as StonePickaxe's: "MushroomYellow"
    // as the cloned base prefab and the animal drop-item ids below are
    // standard, well-established Jotunn/Valheim names, not independently
    // confirmed against this install's binary asset data (which lives in
    // Unity asset bundles, not the decompiled C# assembly). Confidence is
    // lower for Wolf's meat drop and treating Neck's signature item
    // (NeckTail) as its "hide" slot -- flagged explicitly on those calls
    // below. Safe failure mode if any name is wrong: Jotunn logs a clear
    // error on load for that one animal, nothing else is affected.
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
        //
        // hideItemName/meatItemName may each be null (but not both) for
        // an animal that only yields one of the two -- Boar has no hide
        // item, for example.
        public static void RegisterAnimal(string creaturePrefabName, string basePickablePrefabName,
            string hideItemName, int hideAmount, string meatItemName, int meatAmount)
        {
            if (hideItemName == null && meatItemName == null)
            {
                Jotunn.Logger.LogError($"Skinning: {creaturePrefabName} registered with neither a hide nor a meat item, skipping");
                return;
            }

            string hideCarcassName = null;
            if (hideItemName != null)
            {
                hideCarcassName = "Carcass_" + creaturePrefabName + "_Hide";
                if (CreateCarcassPiece(hideCarcassName, basePickablePrefabName, hideItemName, hideAmount, creaturePrefabName) == null) return;
            }

            string meatCarcassName = null;
            if (meatItemName != null)
            {
                meatCarcassName = "Carcass_" + creaturePrefabName + "_Meat";
                if (CreateCarcassPiece(meatCarcassName, basePickablePrefabName, meatItemName, meatAmount, creaturePrefabName) == null) return;
            }

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

            ApplyCreatureVisual(carcass, creaturePrefabName);

            PrefabManager.Instance.AddPrefab(carcass);
            return carcass;
        }

        // Reuses the actual animal's own mesh instead of the cloned base
        // prefab's placeholder visual (a mushroom, structurally cloned
        // for its Pickable/collider/ZNetView, nothing to do with what it
        // looks like). Confirmed against the real 1.0 decompile:
        // Character.m_visual (the field vanilla itself uses for the
        // living model, e.g. ground-tilt rotation) is only populated at
        // runtime by Awake, so it's not usable on an uninstantiated
        // prefab asset -- but GetComponentInChildren<SkinnedMeshRenderer>
        // walks the prefab's actual authored hierarchy directly and works
        // fine without instantiating anything.
        //
        // SkinnedMeshRenderer.sharedMesh is the base bind-pose geometry
        // (before bone deformation) -- a completely valid mesh to render
        // statically via a plain MeshFilter/MeshRenderer, just frozen in
        // whatever pose the model was authored/rigged in rather than a
        // true "collapsed dead body" ragdoll pose (that pose only exists
        // after per-instance physics settles, not as reusable prefab
        // data). A reasonable, safe first approximation, not a perfect
        // one -- rotation and scale are config values specifically
        // because they'll need live tuning once this is actually seen
        // in-game, not something to get right blind.
        //
        // Non-destructive by design: disables the base prefab's own
        // renderers rather than removing any child GameObjects, so
        // there's no risk of deleting something structurally important
        // (the collider/ZNetView host) that this code doesn't have full
        // visibility into.
        static void ApplyCreatureVisual(GameObject carcassPiece, string creaturePrefabName)
        {
            GameObject creaturePrefab = PrefabManager.Instance.GetPrefab(creaturePrefabName);
            SkinnedMeshRenderer source = creaturePrefab != null ? creaturePrefab.GetComponentInChildren<SkinnedMeshRenderer>(true) : null;
            if (source == null || source.sharedMesh == null)
            {
                Jotunn.Logger.LogWarning($"Skinning: no mesh found on '{creaturePrefabName}' for its carcass visual, keeping the placeholder");
                return;
            }

            foreach (Renderer r in carcassPiece.GetComponentsInChildren<Renderer>(true))
            {
                r.enabled = false;
            }

            GameObject visual = new GameObject("CreatureVisual");
            visual.transform.SetParent(carcassPiece.transform, false);
            visual.transform.localRotation = Quaternion.Euler(CorePlugin.SkinningCarcassVisualRotationX.Value, 0f, 0f);
            visual.transform.localScale = Vector3.one * CorePlugin.SkinningCarcassVisualScale.Value;

            MeshFilter filter = visual.AddComponent<MeshFilter>();
            filter.sharedMesh = source.sharedMesh;
            MeshRenderer meshRenderer = visual.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterials = source.sharedMaterials;
        }

        public static bool TryGetAnimal(string creaturePrefabName, out AnimalEntry entry)
        {
            return ByCreatureName.TryGetValue(creaturePrefabName, out entry);
        }

        public static void SpawnCarcass(AnimalEntry entry, Vector3 position)
        {
            if (entry.HideCarcassPrefabName != null) SpawnPiece(entry.HideCarcassPrefabName, position + HidePieceOffset);
            if (entry.MeatCarcassPrefabName != null) SpawnPiece(entry.MeatCarcassPrefabName, position + MeatPieceOffset);
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

        // Four animals now instead of just Deer -- the registry made
        // this a one-line call each, as promised. Confidence varies:
        // Deer and Boar are high-confidence, well-established names.
        // Wolf's meat drop and Neck's "hide" slot are lower-confidence
        // guesses, called out individually below rather than presented
        // with the same certainty as the other two.
        public static void RegisterDefaults()
        {
            RegisterAnimal("Deer", "MushroomYellow", "DeerHide", 1, "RawMeat", 2);

            // Boar has no distinct hide/pelt item in vanilla as far as
            // this could confirm -- meat only.
            RegisterAnimal("Boar", "MushroomYellow", null, 0, "RawMeat", 2);

            // WolfPelt is a well-known crafting material (wolf armor
            // recipes). Wolf also dropping generic RawMeat is a lower-
            // confidence assumption (most early creatures share that one
            // item, but this wasn't independently confirmed for Wolf
            // specifically) -- if wrong, only Wolf's meat carcass piece
            // fails to register (logged), the hide piece is unaffected.
            RegisterAnimal("Wolf", "MushroomYellow", "WolfPelt", 1, "RawMeat", 2);

            // Neck has no separate meat item as far as this could
            // confirm -- its signature drop, NeckTail, is used here as
            // the "hide" slot even though it isn't literally hide, since
            // it's the creature's one distinctive harvestable material
            // and the skinning/butchering split is really "which of the
            // two slots does this go in" rather than a strict hide-vs-
            // meat rule.
            RegisterAnimal("Neck", "MushroomYellow", "NeckTail", 1, null, 0);
        }
    }

    // Pulls the hide and meat entries out of a registered animal's drop
    // list BEFORE Character.OnDeath creates its death ragdoll (see this
    // file's header comment for why that timing matters -- the ragdoll
    // snapshots the drop list for its own delayed drop immediately, so
    // waiting until CharacterDrop.OnDeath is too late), then spawns the
    // carcass piece(s) instead. Anything else in m_drops (trophies, etc.)
    // is left untouched and still drops normally, whether via
    // CharacterDrop.OnDeath directly or via the ragdoll's delayed drop.
    [HarmonyPatch(typeof(Character), nameof(Character.OnDeath))]
    public static class SkinningCarcassSpawnPatch
    {
        static void Prefix(Character __instance, out List<CharacterDrop.Drop> __state)
        {
            __state = null;

            CharacterDrop drop = __instance.GetComponent<CharacterDrop>();
            if (drop == null || !drop.m_dropsEnabled) return;

            string creatureName = __instance.m_nview != null ? __instance.m_nview.GetPrefabName() : null;
            if (creatureName == null || !SkinningSystem.TryGetAnimal(creatureName, out SkinningSystem.AnimalEntry entry)) return;

            List<CharacterDrop.Drop> removed = drop.m_drops
                .Where(d => d.m_prefab != null
                    && ((entry.HideItemName != null && d.m_prefab.name == entry.HideItemName)
                        || (entry.MeatItemName != null && d.m_prefab.name == entry.MeatItemName)))
                .ToList();
            if (removed.Count == 0) return;

            foreach (CharacterDrop.Drop d in removed) drop.m_drops.Remove(d);
            __state = removed;

            SkinningSystem.SpawnCarcass(entry, __instance.GetCenterPoint());
        }

        static void Postfix(Character __instance, List<CharacterDrop.Drop> __state)
        {
            if (__state == null) return;

            CharacterDrop drop = __instance.GetComponent<CharacterDrop>();
            if (drop != null) drop.m_drops.AddRange(__state);
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
