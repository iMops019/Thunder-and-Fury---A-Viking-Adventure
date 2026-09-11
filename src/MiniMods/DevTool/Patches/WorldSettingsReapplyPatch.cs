using HarmonyLib;
using ThunderFury.DevTool.Data;

namespace ThunderFury.DevTool.Patches
{
    // Reapplies whatever the World/Areas tab last saved, every time a
    // game world actually starts. Game.Start is the same real vanilla
    // method Jotunn's own GUIManager already patches (confirmed via
    // decompile: GUIManager.Patches.CreateCustomGUI is a Postfix on this
    // exact method) -- reusing an already-proven hook point rather than
    // guessing at a different one.
    [HarmonyPatch(typeof(Game), "Start")]
    public static class WorldSettingsReapplyPatch
    {
        static void Postfix()
        {
            DevWorldSettingsRegistry.Apply();
            DevZoneSettingsRegistry.Apply();
        }
    }
}
