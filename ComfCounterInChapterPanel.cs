using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoMod.RuntimeDetour;
using Celeste.Mod.DoonvHelper.Entities;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System.Reflection;
using Monocle;
using System.Collections;
using Celeste.Mod.DoonvHelper.DoonvHelperUtils;

namespace Celeste.Mod.DoonvHelper
{
    public class ComfCounterInChapterPanel {

        /// <summary>
        /// Hook the methods of `OuiChapterPanel` so it displays a comf counter.
        /// </summary>
        public void HookMethods() {
            On.Celeste.Session.ctor_AreaKey_string_AreaStats += ModSessionCreate; // Store comf amount at beginning of session
            On.Celeste.OuiChapterPanel.ctor += ModOuiChapterPanelConstructor; // Create the counter
            On.Celeste.OuiChapterPanel.UpdateStats += ModOuiChapterPanelUpdateStats; // Set counter stats
            IL.Celeste.OuiChapterPanel.Render += ModOuiChapterPanelRender; // Set counter position
            IL.Celeste.OuiChapterPanel.SetStatsPosition += ModOuiChapterPanelSetStatsPosition; // Center other counters
            On.Celeste.OuiChapterPanel.IncrementStatsDisplay += ModOuiChapterPanelIncrementStatsDisplay; // Tick up animation
            On.Celeste.OuiChapterPanel.GetModeHeight += ModOuiChapterPanelGetModeHeight; // See the function comment
        }

        private void TestReset(On.Celeste.OuiChapterPanel.orig_Reset orig, OuiChapterPanel self)
        {
            orig(self);
            Logger.Log(LogLevel.Info, "DoonvHelper", "Reset.");
        }

        /// <summary>
        /// Unhook the methods of `OuiChapterPanel` so it doesn't display a comf counter.
        /// </summary>
        public void UnhookMethods() {
            On.Celeste.Session.ctor_AreaKey_string_AreaStats -= ModSessionCreate;
            On.Celeste.OuiChapterPanel.ctor -= ModOuiChapterPanelConstructor; 
            On.Celeste.OuiChapterPanel.Reset -= TestReset;
            On.Celeste.OuiChapterPanel.UpdateStats -= ModOuiChapterPanelUpdateStats;
            IL.Celeste.OuiChapterPanel.Render -= ModOuiChapterPanelRender;
            IL.Celeste.OuiChapterPanel.SetStatsPosition -= ModOuiChapterPanelSetStatsPosition; 
            On.Celeste.OuiChapterPanel.IncrementStatsDisplay -= ModOuiChapterPanelIncrementStatsDisplay;
            On.Celeste.OuiChapterPanel.GetModeHeight -= ModOuiChapterPanelGetModeHeight;
        }

        private void ModSessionCreate(
            On.Celeste.Session.orig_ctor_AreaKey_string_AreaStats orig,
            Session self,
            AreaKey area,
            string checkpoint,
            AreaStats oldStats
        )
        {
            orig(self, area, checkpoint, oldStats);

            // Here we store the comf amount at the beginning of the session
            // so we can have an animation of the comf counter ticking up from its value at the beginning of the session
            // to its value at the end of the session.
            DoonvHelperModule.SaveData.OldComfAmount = DoonvHelperModule.SaveData.ComfLevelData.SafeGet(new LevelSideID(area)).Count;
        }
        
        /// <summary>The thing that shows the comf count.</summary>
        private ComfCounter comfCounter;
        /// <summary>Offset of `comfCounter`.</summary>
        private Vector2 comfOffset;

        /// <summary>
        /// Creates the `comfCounter`.
        /// </summary>
        private void ModOuiChapterPanelConstructor(On.Celeste.OuiChapterPanel.orig_ctor orig, OuiChapterPanel panel) {
            Logger.Log(LogLevel.Info, "DoonvHelper", "Constructor.");
            orig(panel);
            // Logger.Log(LogLevel.Info, "DoonvHelper ModOuiChapterPanelConstructor", DoonvHelperModule.ComfLevelTotals.SafeGet(new LevelSideID(panel.Area)).ToString());
            // Logger.Log(LogLevel.Info, "DoonvHelper ModOuiChapterPanelConstructor", panel.Area.GetSID().ToString());
            // Logger.Log(LogLevel.Info, "DoonvHelper ModOuiChapterPanelConstructor", SaveData.Instance.LastArea_Safe.GetSID().ToString());
            // If the current level has no comfs then don't create the comf counter because we don't need it
            // We also add a return statement to nearly every method so our hooks don't do anything when the comf counter is gone
            // if (DoonvHelperModule.ComfLevelTotals.SafeGet(new LevelSideID(panel.Area)) == 0) {
                
            // }

            // Add the comf counter
            panel.Add(comfCounter = new ComfCounter(true, 0, 68, false, GFX.Gui["DoonvHelper/comf/photorealistic_comfer"]));
            comfCounter.CanWiggle = false;
            comfCounter.Visible = false;
            comfCounter.OverworldSfx = true;
        }

        /// <summary>
        /// Shifts the position of the comf counter.
        /// I don't know how IL hooks work and I am not going to explain 
        /// this because I just copied this from max480's dash count mod.
        /// </summary>
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
            if (comfCounter == null) return;
            comfCounter.Position = contentOffset + new Vector2(0f, 170f) + comfOffset;
        }

        /// <summary>
        /// Shifts the position of the other counters in the chapter panel. 
        /// I don't know how IL hooks work and I am not going to explain 
        /// this because I just copied this from max480's dash count mod.
        /// </summary>
        private void ModOuiChapterPanelSetStatsPosition(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            // this is a tricky one... in lines like this:
            // this.strawberriesOffset = this.Approach(this.strawberriesOffset, new Vector2(120f, (float)(this.deaths.Visible ? -40 : 0)), !approach);
            // we want to catch the result of (float)(this.deaths.Visible ? -40 : 0) and transform it to shift the things up if the dashes counter is there.
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchConvR4())) {
                Logger.Log("DashCountMod", $"Modifying strawberry/death counter positioning at {cursor.Index} in CIL code for OuiChapterPanel.SetStatsPosition");
                cursor.EmitDelegate<Func<float, float>>(ShiftCountersPosition);
            }

            cursor.Index = 0;

            // we will cross 2 occurrences when deathsOffset will be set: first time with the heart, second time without.
            // the only difference is the X offset, so putf the code in common.
            bool hasHeart = true;
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchStfld(typeof(OuiChapterPanel), "deathsOffset"))) {
                Logger.Log("DashCountMod", $"Injecting dashes counter position updating at {cursor.Index} in CIL code for OuiChapterPanel.SetStatsPosition (has heart = {hasHeart})");

                // bool approach
                cursor.Emit(OpCodes.Ldarg_1);
                // StrawberriesCounter strawberries
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldfld, typeof(OuiChapterPanel).GetField("strawberries", BindingFlags.NonPublic | BindingFlags.Instance));
                // DeathsCounter deaths
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldfld, typeof(OuiChapterPanel).GetField("deaths", BindingFlags.NonPublic | BindingFlags.Instance));
                // bool hasHeart
                cursor.Emit(hasHeart ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                // function call
                cursor.EmitDelegate<Action<bool, StrawberriesCounter, DeathsCounter, bool>>(UpdateComfCounterOffset);

                hasHeart = false;
            }

            cursor.Index = 0;

            if (cursor.TryGotoNext(MoveType.After,
                instr => instr.MatchLdfld<OuiChapterPanel>("deaths"),
                instr => instr.MatchLdfld<Component>("Visible"))) {

                Logger.Log("DashCountMod", $"Patching chapter panel columns count at {cursor.Index} in CIL code for OuiChapterPanel.SetStatsPosition");
                cursor.EmitDelegate<Func<bool, bool>>(orig => orig || (comfCounter?.Visible ?? false));
            }
        }

        private float ShiftCountersPosition(float position) {
            if (comfCounter == null) return position;
            return comfCounter.Visible && comfOffset.Y != 160f ? position + 40 : position;
        }

        private void UpdateComfCounterOffset(bool approach, StrawberriesCounter strawberries, DeathsCounter deaths, bool hasHeart) {
            if (comfCounter == null) return;
            int shift = 0;
            if (strawberries.Visible) shift += 40;
            if (deaths.Visible) shift += 40;
            if (shift == 120f) shift += 40;
            comfOffset = Approach(comfOffset, new Vector2(hasHeart ? 120f : 0f, -shift), !approach);
        }

        /// <summary>
        /// Copy of `Celeste.OuiChapterPanel.Approach`. This is copied because the original method was private.
        /// Modding is dumb.
        /// </summary>
        private Vector2 Approach(Vector2 from, Vector2 to, bool snap) {
            if (snap) return to;
            return from += (to - from) * (1f - (float) Math.Pow(0.0010000000474974513, Engine.DeltaTime));
        }

        /// <summary>
        /// Update the visibility, "out of", and amount of the comfCounter.
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
            Logger.Log(LogLevel.Info, "DoonvHelper ModOuiChapterPanelUpdateStats", DoonvHelperModule.ComfLevelTotals.SafeGet(new LevelSideID(panel.Area)).ToString());
            if (comfCounter == null) return;

            LevelSideID levelID = new LevelSideID(panel.Area);
            comfCounter.Visible = 
                (panel.DisplayedStats.Modes[(int)panel.Area.Mode].Completed ||
                DoonvHelperModule.SaveData.ComfLevelData.SafeGet(levelID).Count > 0) &&
                !panel.Data.Interlude_Safe && DoonvHelperModule.ComfLevelTotals.SafeGet(levelID) > 0;
            comfCounter.OutOf = DoonvHelperModule.ComfLevelTotals.SafeGet(levelID);
		    comfCounter.ShowOutOf = panel.DisplayedStats.Modes[(int)panel.Area.Mode].Completed;
            
            comfCounter.Amount = DoonvHelperModule.SaveData.ComfLevelData.SafeGet(levelID).Count;

            if (panel.DisplayedStats != panel.RealStats) {
                // this is a sign that we are returning from a level, and we should display the old dash count so that it can animate to the new dash count.
                comfCounter.Amount = DoonvHelperModule.SaveData.OldComfAmount;
            }

            if (wiggle && comfCounter.Visible && (overrideDeathWiggle ?? true)) {
                comfCounter.Wiggle();
            }
        }

        /// <summary>
        /// Basically does the same thing as the vanilla method does to the `StrawberriesCounter`,
        /// but to the `comfCounter`.
        /// </summary>
        private IEnumerator ModOuiChapterPanelIncrementStatsDisplay(
            On.Celeste.OuiChapterPanel.orig_IncrementStatsDisplay orig, 
            OuiChapterPanel panel, AreaModeStats modeStats,
            AreaModeStats newModeStats, bool doHeartGem, 
            bool doStrawberries, bool doDeaths, bool doRemixUnlock
        ) {
            
            bool modeStatsWasCompleted = modeStats.Completed; // We store this because it will get set to true in the original method.
            yield return orig(panel, modeStats, newModeStats, doHeartGem, doStrawberries, doDeaths, doRemixUnlock);
            
            if (comfCounter == null) yield break;

            int oldComfs = DoonvHelperModule.SaveData.OldComfAmount;
            int newComfs = DoonvHelperModule.SaveData.ComfLevelData.SafeGet(new LevelSideID(panel.Area)).Count;
            int totalComfs = DoonvHelperModule.ComfLevelTotals.SafeGet(new LevelSideID(panel.Area));
			
            comfCounter.Visible = !panel.Data.Interlude_Safe && totalComfs > 0;
            if (!comfCounter.Visible) {
                yield break;
            }
            comfCounter.CanWiggle = true;
			while (newComfs > oldComfs)
			{
				int num = newComfs - oldComfs;
				if (num < 3)
				{
					yield return 0.3f;
				}
				else if (num < 8)
				{
					yield return 0.2f;
				}
				else
				{
					yield return 0.1f;
					oldComfs++;
				}
				oldComfs++;
                comfCounter.Amount = oldComfs;
				Input.Rumble(RumbleStrength.Light, RumbleLength.Short);
			}
			comfCounter.CanWiggle = false;
			yield return 0.5f;
			if (newModeStats.Completed && !modeStatsWasCompleted)
			{
				yield return 0.25f;
				Audio.Play(comfCounter.Amount >= totalComfs ? "event:/ui/postgame/strawberry_total_all" : "event:/ui/postgame/strawberry_total");
				comfCounter.OutOf = totalComfs;
				comfCounter.ShowOutOf = true;
				comfCounter.Wiggle();
				modeStats.Completed = true;
				Input.Rumble(RumbleStrength.Light, RumbleLength.Medium);
				yield return 0.6f;
			}
        }

        /// <summary>
        /// This hook fixes a bug where only the `comfCounter` is showing. 
        /// However because the height function (this one) only checks for strawberries, hearts and deaths,
        /// the height of the `OuiChapterPanel` is as if it had nothing in it.
        ///
        /// So basically what we do here is we check if the panel is small and it should be big, then make it big.
        /// Yeah...
        /// </summary>
        private int ModOuiChapterPanelGetModeHeight(On.Celeste.OuiChapterPanel.orig_GetModeHeight orig, OuiChapterPanel panel) {
            int originalValue = orig(panel);
            if (comfCounter == null) return originalValue;
            if (
                originalValue <= 300 && 
                DoonvHelperModule.SaveData.ComfLevelData.SafeGet(new LevelSideID(panel.Area)).Count > 0
            ) {
                return 540;
            }
            return originalValue;
        }


    }
}