using System;
using System.Globalization;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;
using ThunderFury.DevTool.Data;
using static ThunderFury.DevTool.UI.DevToolUiHelpers;

namespace ThunderFury.DevTool.UI
{
    // ---- World/Areas tab ----
    //
    // Exposes vanilla's own real World Level system (docs/PROGRESS.md's
    // Classes & Passive Trees research already confirmed these fields
    // against the decompile: Game.m_worldLevel, 0-10, is vanilla 1.0's
    // actual "New Game+/hard mode" dial, scaling enemy/gear/piece
    // stats all at once via the multiplier fields below) as an editable
    // panel instead of console commands. All are real public fields on
    // Game/Game.instance, not new state this mod invents.
    //
    // Deliberately only usable while actually in a world -- Game.instance
    // is null at the main menu, and editing "how hard is the world"
    // multipliers means nothing without one loaded.
    //
    // Persistence: every edit here both applies live to the running Game
    // instance AND writes through to DevWorldSettingsRegistry's own JSON
    // (see that file for why -- Game.m_worldLevel turns out to load from
    // a world-creation-time modifiers store, not a normal runtime save
    // key, so writing back into Valheim's own save format directly was
    // judged too risky; Dev Tool remembers the values itself instead and
    // silently reapplies them on every game start).
    public class WorldAreaTab : DevToolOverlay.ITab
    {
        public string Title => "World";

        public void Build(Transform contentParent)
        {
            GUIManager gui = GUIManager.Instance;

            if (Game.instance == null)
            {
                TextRow(gui, contentParent, "Enter a world to edit these values -- Game.instance isn't available from the main menu.", true);
                return;
            }

            // Only overwrite Current from the live game if nothing's been
            // saved yet -- otherwise a fresh Game instance's own defaults
            // would clobber a previously-saved override the moment this
            // tab is opened, before the player touches anything.
            if (!DevWorldSettingsRegistry.Current.Present)
            {
                DevWorldSettingsRegistry.SyncFromLiveGame();
            }

            TextRow(gui, contentParent, "World / Area Difficulty (vanilla's own World Level system)", true);
            TextRow(gui, contentParent,
                "Changes apply immediately AND are remembered by Dev Tool for next time you play (see this tab's header comment for why that's not the same as a normal save).",
                false, 55f);

            IntFieldRow(gui, contentParent, "World Level (0-10)",
                () => Game.m_worldLevel,
                v => { Game.m_worldLevel = Mathf.Clamp(v, 0, 10); DevWorldSettingsRegistry.Current.WorldLevel = Game.m_worldLevel; });

            IntFieldRow(gui, contentParent, "Enemy Base Armor",
                () => Game.instance.m_worldLevelEnemyBaseAC,
                v => { Game.instance.m_worldLevelEnemyBaseAC = v; DevWorldSettingsRegistry.Current.EnemyBaseAC = v; });

            FloatFieldRow(gui, contentParent, "Enemy HP Multiplier",
                () => Game.instance.m_worldLevelEnemyHPMultiplier,
                v => { Game.instance.m_worldLevelEnemyHPMultiplier = v; DevWorldSettingsRegistry.Current.EnemyHPMultiplier = v; });

            IntFieldRow(gui, contentParent, "Enemy Base Damage",
                () => Game.instance.m_worldLevelEnemyBaseDamage,
                v => { Game.instance.m_worldLevelEnemyBaseDamage = v; DevWorldSettingsRegistry.Current.EnemyBaseDamage = v; });

            FloatFieldRow(gui, contentParent, "Enemy Move Speed Multiplier",
                () => Game.instance.m_worldLevelEnemyMoveSpeedMultiplier,
                v => { Game.instance.m_worldLevelEnemyMoveSpeedMultiplier = v; DevWorldSettingsRegistry.Current.EnemyMoveSpeedMultiplier = v; });

            FloatFieldRow(gui, contentParent, "Enemy Level-Up Exponent",
                () => Game.instance.m_worldLevelEnemyLevelUpExponent,
                v => { Game.instance.m_worldLevelEnemyLevelUpExponent = v; DevWorldSettingsRegistry.Current.EnemyLevelUpExponent = v; });

            IntFieldRow(gui, contentParent, "Gear Base Armor",
                () => Game.instance.m_worldLevelGearBaseAC,
                v => { Game.instance.m_worldLevelGearBaseAC = v; DevWorldSettingsRegistry.Current.GearBaseAC = v; });

            IntFieldRow(gui, contentParent, "Gear Base Damage",
                () => Game.instance.m_worldLevelGearBaseDamage,
                v => { Game.instance.m_worldLevelGearBaseDamage = v; DevWorldSettingsRegistry.Current.GearBaseDamage = v; });

            IntFieldRow(gui, contentParent, "Piece Base Damage",
                () => Game.instance.m_worldLevelPieceBaseDamage,
                v => { Game.instance.m_worldLevelPieceBaseDamage = v; DevWorldSettingsRegistry.Current.PieceBaseDamage = v; });

            FloatFieldRow(gui, contentParent, "Piece HP Multiplier",
                () => Game.instance.m_worldLevelPieceHPMultiplier,
                v => { Game.instance.m_worldLevelPieceHPMultiplier = v; DevWorldSettingsRegistry.Current.PieceHPMultiplier = v; });

            FloatFieldRow(gui, contentParent, "Mine HP Multiplier",
                () => Game.instance.m_worldLevelMineHPMultiplier,
                v => { Game.instance.m_worldLevelMineHPMultiplier = v; DevWorldSettingsRegistry.Current.MineHPMultiplier = v; });

            GameObject saveButton = Button(gui, contentParent, "Save (remember for next time)");
            saveButton.GetComponent<Button>().onClick.AddListener(() =>
            {
                DevWorldSettingsRegistry.Save();
                Player.m_localPlayer?.Message(MessageHud.MessageType.Center, "World/Area settings saved -- will reapply next time you play.");
            });
        }

        static void IntFieldRow(GUIManager gui, Transform parent, string label, Func<int> getter, Action<int> setter)
        {
            InputField field = FieldRow(gui, parent, label, getter().ToString(CultureInfo.InvariantCulture));
            field.onEndEdit.AddListener(text =>
            {
                if (int.TryParse(text, out int v)) setter(v);
            });
        }

        static void FloatFieldRow(GUIManager gui, Transform parent, string label, Func<float> getter, Action<float> setter)
        {
            InputField field = FieldRow(gui, parent, label, getter().ToString(CultureInfo.InvariantCulture));
            field.onEndEdit.AddListener(text =>
            {
                if (float.TryParse(text, out float v)) setter(v);
            });
        }
    }
}
