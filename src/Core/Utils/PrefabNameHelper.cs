using UnityEngine;

namespace ThunderFury.Core.Utils
{
    // ---- Getting a live Character's clean prefab name, the verified-safe way ----
    //
    // Found while decompiling for the biome-drop feature (2026-09-10):
    // Character.m_nview is now `protected` and ZNetView.GetPrefabName()
    // is now `private` on the REAL (non-publicized) game assembly --
    // confirmed via ilspycmd against assembly_valheim.dll, timestamped
    // the same day as the in-session hotfix DiscoverBiomePatch's own
    // comment already documents breaking Player.AddKnownBiome. The local
    // publicized reference assembly still shows both as public (it was
    // regenerated slightly AFTER that real assembly, so it isn't stale in
    // the usual sense -- publicizing just makes everything compile-time
    // accessible regardless of the real access modifier), which is
    // exactly the "compiles fine, throws at runtime" trap this project's
    // own verify-before-build lesson warns about. This was silently
    // broken in two already-shipped patches that used the same
    // `__instance.m_nview.GetPrefabName()` pattern (SkinningPatches.cs's
    // carcass detection, Quests' KillCreature tracking) -- fixed to call
    // this instead.
    //
    // Every step here is confirmed public: Component.GetComponent<T>()
    // (Unity), ZNetView.GetZDO(), ZDO.GetPrefab() (returns the prefab's
    // stable name hash), ZNetScene.GetPrefab(int) (resolves that hash
    // back to the actual prefab GameObject, whose .name is never
    // "(Clone)"-suffixed since it's the original asset, not a live
    // instance).
    public static class PrefabNameHelper
    {
        public static string GetPrefabName(Character character)
        {
            if (character == null) return null;

            ZNetView nview = character.GetComponent<ZNetView>();
            ZDO zdo = nview != null ? nview.GetZDO() : null;
            if (zdo == null) return null;

            GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(zdo.GetPrefab()) : null;
            return prefab != null ? prefab.name : null;
        }
    }
}
