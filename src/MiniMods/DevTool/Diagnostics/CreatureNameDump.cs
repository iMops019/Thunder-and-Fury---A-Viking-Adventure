using HarmonyLib;

namespace ThunderFury.DevTool.Diagnostics
{
    // ---- TEMPORARY one-shot diagnostic ----
    //
    // 2026-09-11: 8 of the Legendary batches' creature-name guesses
    // failed to resolve a CharacterDrop (Golem, Drake, Fuling,
    // Fuling_Shaman, Dverger_Mage, Twitcher, Charred, Urchin) while
    // several others with the same naming style worked fine (Troll,
    // Greydwarf_Elite, Greydwarf_Shaman, Draugr_Elite). Rather than
    // guess a second time, this dumps every real ZNetScene prefab with a
    // CharacterDrop component -- the exact same component
    // RegisterDropOn needs -- straight from the running game. Delete
    // once the real names are confirmed and the affected batches are
    // fixed.
    public static class CreatureNameDump
    {
        static bool _hasRun;

        public static void RunOnce()
        {
            if (_hasRun) return;
            _hasRun = true;

            if (ZNetScene.instance == null) return;

            Jotunn.Logger.LogInfo("[CreatureNameDump] All prefabs with a CharacterDrop component:");
            foreach (UnityEngine.GameObject prefab in ZNetScene.instance.m_prefabs)
            {
                if (prefab == null) continue;
                if (prefab.GetComponent<CharacterDrop>() == null) continue;

                Jotunn.Logger.LogInfo($"[CreatureNameDump]   {prefab.name}");
            }
        }
    }

    [HarmonyPatch(typeof(Game), "Start")]
    public static class CreatureNameDumpPatch
    {
        static void Postfix()
        {
            CreatureNameDump.RunOnce();
        }
    }
}
