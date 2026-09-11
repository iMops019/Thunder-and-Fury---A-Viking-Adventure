using HarmonyLib;
using UnityEngine;

namespace ThunderFury.Core.Patches
{
    // ---- Skinning channel: hold [E] for a couple seconds before a carcass gives up its hide/meat ----
    //
    // User's own request (2026-09-10): "a bit of a flavor mechanic" --
    // instant pickup felt too abrupt for something as deliberate as
    // skinning an animal. Reuses vanilla's own action-progress bar
    // (Hud.m_actionBarRoot/m_actionProgress/m_actionName -- confirmed
    // public via decompile, the same UI already used for things like
    // eating-with-a-delay) rather than building a new one from scratch,
    // so this looks and feels like an existing vanilla timed action
    // instead of a bolted-on custom overlay.
    //
    // Vanilla's own "minor action queue" that normally drives that bar
    // (Player.m_actionQueue) is private with a closed ActionType enum
    // (Equip/Unequip/Reload only, confirmed via decompile) -- not a
    // generic API another mod can add a custom entry to. Cheaper and
    // more robust to drive the SAME bar directly via a Postfix on Hud's
    // own (private, string-targeted) UpdateActionProgress, which already
    // runs every frame regardless of what's actually in that queue.
    //
    // Player.Interact(GameObject, bool hold, bool alt) (confirmed via
    // decompile) calls Interact(hold: false) the frame [E] is first
    // pressed, then Interact(hold: true) every subsequent frame it's
    // still held, and simply stops calling it at all the instant it's
    // released -- there's no explicit "release" event to hook.
    //
    // First cut used a one-Update-frame tolerance (Time.frameCount) to
    // infer release, on the assumption Interact fires every single
    // render frame while held. Confirmed live in-game 2026-09-10 that
    // assumption was wrong: the channel flickered on/off constantly and
    // never reached completion, meaning Player's own hover-target
    // re-check (what actually gates whether Interact gets called at all)
    // is throttled to something coarser than one frame -- the same
    // "don't redo an expensive check every single frame" SlowUpdate
    // idiom already used elsewhere in this codebase (e.g.
    // ItemDrop.SlowUpdate). A real gap between consecutive Interact
    // calls while still genuinely held is therefore normal, not a
    // release signal. Switched to wall-clock time with a much more
    // generous window -- long enough to absorb that throttling
    // comfortably, short enough that letting go still feels responsive.
    [HarmonyPatch(typeof(Pickable), nameof(Pickable.Interact))]
    public static class SkinningChannelInteractPatch
    {
        const float ReleaseTolerance = 0.3f;

        static Pickable _target;
        static float _progress;
        static float _lastChannelTime = -1f;

        static bool Prefix(Pickable __instance, ref bool __result)
        {
            if (!SkinningSystem.IsCarcass(__instance)) return true;

            bool freshStart = _target != __instance || Time.time - _lastChannelTime > ReleaseTolerance;
            if (freshStart)
            {
                _target = __instance;
                _progress = 0f;
            }
            else
            {
                // Real elapsed wall-clock time since the last call, not
                // Time.deltaTime (one render frame's worth) -- Interact
                // only fires on whatever cadence the throttled hover
                // check allows, so crediting just one frame per call
                // would under-count real hold time whenever calls are
                // more than a frame apart, making the channel take
                // noticeably longer than the configured duration.
                _progress += Time.time - _lastChannelTime;
            }

            _lastChannelTime = Time.time;

            if (_progress < CorePlugin.SkinningChannelDuration.Value)
            {
                // Same value Pickable's own unblocked Interact() would
                // return, so the player's interact-hold animation keeps
                // playing consistently while channeling.
                __result = __instance.m_useInteractAnimation;
                return false;
            }

            _target = null;
            _progress = 0f;
            return true;
        }

        public static bool TryGetProgress(out float progress01)
        {
            progress01 = 0f;
            if (_target == null) return false;

            if (Time.time - _lastChannelTime > ReleaseTolerance)
            {
                _target = null;
                _progress = 0f;
                return false;
            }

            progress01 = Mathf.Clamp01(_progress / CorePlugin.SkinningChannelDuration.Value);
            return true;
        }
    }

    [HarmonyPatch(typeof(Hud), "UpdateActionProgress")]
    public static class SkinningChannelHudPatch
    {
        static void Postfix(Hud __instance)
        {
            if (!SkinningChannelInteractPatch.TryGetProgress(out float progress)) return;

            __instance.m_actionBarRoot.SetActive(true);
            __instance.m_actionProgress.SetValue(progress);
            __instance.m_actionName.text = "Skinning...";
        }
    }
}
