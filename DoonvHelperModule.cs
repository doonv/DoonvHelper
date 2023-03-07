using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoMod.RuntimeDetour;
using Celeste.Mod.DoonvHelper.Entities;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System.Reflection;

namespace Celeste.Mod.DoonvHelper {
    public class DoonvHelperModule : EverestModule {
        public static DoonvHelperModule Instance { get; private set; }

        public override Type SettingsType => typeof(DoonvHelperModuleSettings);
        public static DoonvHelperModuleSettings Settings => (DoonvHelperModuleSettings) Instance._Settings;

        public override Type SessionType => typeof(DoonvHelperModuleSession);
        public static DoonvHelperModuleSession Session => (DoonvHelperModuleSession) Instance._Session;

        public override Type SaveDataType => typeof(DoonvHelperSaveData);
        public static DoonvHelperSaveData SaveData => (DoonvHelperSaveData) Instance._SaveData;

        public DoonvHelperModule() {
            Instance = this;
            
            #if DEBUG
                // debug builds use verbose logging
                Logger.SetLogLevel(nameof(DoonvHelperModule), LogLevel.Verbose);
            #else
                // release builds use info logging to reduce spam in log files
                Logger.SetLogLevel(nameof(DoonvHelperModule), LogLevel.Info);
            #endif
        }

        // public void SetDashCounterInChapterPanel(bool unhook) {

        //     // (un)hook methods
        //     if (unhook) {
        //         Logger.Log("DashCountMod", "Hooking chapter panel rendering methods");

        //         using (new DetourContext() { After = { "*" } }) { // be sure to apply _after_ the collab utils.
        //             IL.Celeste.OuiChapterPanel.Render += ModOuiChapterPanelRender;
        //             // On.Celeste.OuiChapterPanel.UpdateStats += ModOuiChapterPanelUpdateStats;
        //             // IL.Celeste.OuiChapterPanel.SetStatsPosition += ModOuiChapterPanelSetStatsPosition;
        //             // On.Celeste.OuiChapterPanel.IncrementStatsDisplay += ModOuiChapterPanelIncrementStatsDisplay;
        //             // On.Celeste.OuiChapterPanel.GetModeHeight += ModOuiChapterPanelGetModeHeight;
        //         }
        //     } else if (hook) {
        //         Logger.Log("DashCountMod", "Unhooking chapter panel rendering methods");

        //         IL.Celeste.OuiChapterPanel.Render -= ModOuiChapterPanelRender;
        //         On.Celeste.OuiChapterPanel.UpdateStats -= ModOuiChapterPanelUpdateStats;
        //         IL.Celeste.OuiChapterPanel.SetStatsPosition -= ModOuiChapterPanelSetStatsPosition;
        //         On.Celeste.OuiChapterPanel.IncrementStatsDisplay -= ModOuiChapterPanelIncrementStatsDisplay;
        //         On.Celeste.OuiChapterPanel.GetModeHeight -= ModOuiChapterPanelGetModeHeight;

        //         collabUtilsHook?.Dispose();
        //         collabUtilsHook = null;

        //         // hide the dash counter if currently shown: as we unhooked everything updating it, it will stay invisible.
        //         dashesCounter.Visible = false;
        //     }

        //     dashCounterInChapterPanel = newValue;
        // }

        public override void Load() {
            On.Celeste.Level.Begin += ModLevelBegin;
            On.Celeste.OuiChapterPanel.ctor += ModOuiChapterPanelConstructor;
            On.Celeste.OuiChapterPanel.UpdateStats += ModOuiChapterPanelUpdateStats;
            IL.Celeste.OuiChapterPanel.Render += ModOuiChapterPanelRender;

        }
        public override void Unload() {
            On.Celeste.Level.Begin -= ModLevelBegin;
            On.Celeste.OuiChapterPanel.ctor -= ModOuiChapterPanelConstructor;
            On.Celeste.OuiChapterPanel.UpdateStats -= ModOuiChapterPanelUpdateStats;
        }

        /// <summary>
        /// This is a hook that overrides the `Level.Begin` method.
        /// </summary>
        private void ModLevelBegin(On.Celeste.Level.orig_Begin orig, Level level)
        {
            orig(level); // Call original method that we have overriden so we don't break the game.

            // If there isn't a list of EntityIDs for a specific level that we have entered into,
            // then we generate a new list.
            // Yes this is our second time doing this, but gotta make sure riiighhhhttt?
            LevelSideID levelID = new LevelSideID(level);
            if (!DoonvHelperModule.SaveData.ComfLevelData.ContainsKey(levelID)) {
                DoonvHelperModule.SaveData.ComfLevelData[levelID] = new List<EntityID>();
            }

            // Add the In-game comf display so we can see our comf counter in game.
            level.Add(new Entities.ComfInGameDisplay());
        }
        
        private ComfCounter comfCounter;
        private void ModOuiChapterPanelConstructor(On.Celeste.OuiChapterPanel.orig_ctor orig, OuiChapterPanel panel) {
            orig(panel);

            // add the dashes counter as well, but have it hidden by default
            panel.Add(comfCounter = new ComfCounter(false, 5, 6));
            comfCounter.CanWiggle = false;
            comfCounter.Visible = true;
            comfCounter.Position = new Vector2(64f, 16f);
        }

        private void ModOuiChapterPanelRender(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            // move after the deaths counter positioning, and place ourselves after that to update dashes counter position as well
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchStfld(typeof(DeathsCounter), "Position"))) {
                Logger.Log("DashCountMod", $"Injecting dashes counter position updating at {cursor.Index} in CIL code for OuiChapterPanel.Render");

                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldfld, typeof(OuiChapterPanel).GetField("contentOffset", BindingFlags.NonPublic | BindingFlags.Instance));
                cursor.EmitDelegate<Action<Vector2>>(UpdateComfCounterRenderedPosition);
            }
        }
        private void UpdateComfCounterRenderedPosition(Vector2 contentOffset) {
            comfCounter.Position = contentOffset + new Vector2(0f, 170f) + new Vector2(0, 0);
        }



        /// <summary>
        /// Update the comf count and visible of the comfCounter.
        /// </summary>
        private void ModOuiChapterPanelUpdateStats(
            On.Celeste.OuiChapterPanel.orig_UpdateStats orig,
            OuiChapterPanel panel,
            bool wiggle,
            bool? overrideStrawberryWiggle,
            bool? overrideDeathWiggle,
            bool? overrideHeartWiggle
        ) {
            orig(panel, wiggle, overrideStrawberryWiggle, overrideDeathWiggle, overrideHeartWiggle);

            // If there isn't a list of EntityIDs for a specific level that we have entered into,
            // then we generate a new list.
            LevelSideID levelID = new LevelSideID(panel.Area);
            if (!DoonvHelperModule.SaveData.ComfLevelData.ContainsKey(levelID)) {
                DoonvHelperModule.SaveData.ComfLevelData[levelID] = new List<EntityID>();
            }

            comfCounter.Visible = panel.DisplayedStats.Modes[(int)panel.Area.Mode].SingleRunCompleted;
            comfCounter.Amount = DoonvHelperModule.SaveData.ComfLevelData[new LevelSideID(panel.Area)].Count;

            if (wiggle && comfCounter.Visible && (overrideDeathWiggle ?? true)) {
                comfCounter.Wiggle();
            }
        }
    }
}