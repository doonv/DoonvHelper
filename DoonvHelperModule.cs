using System;
using System.Collections.Generic;
using Guneline;
using Microsoft.Xna.Framework;
using MonoMod.RuntimeDetour;
using Celeste.Mod.DoonvHelper.Entities;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System.Reflection;
using Monocle;
using System.Collections;
using MonoMod.Utils;
using Celeste.Mod.DoonvHelper.DoonvHelperUtils;

namespace Celeste.Mod.DoonvHelper {
    public class DoonvHelperModule : EverestModule {
        public static DoonvHelperModule Instance { get; private set; }

        public override Type SettingsType => typeof(DoonvHelperModuleSettings);
        public static DoonvHelperModuleSettings Settings => (DoonvHelperModuleSettings) Instance._Settings;

        public override Type SessionType => typeof(DoonvHelperModuleSession);
        public static DoonvHelperModuleSession Session => (DoonvHelperModuleSession) Instance._Session;

        public override Type SaveDataType => typeof(DoonvHelperSaveData);
        public static DoonvHelperSaveData SaveData => (DoonvHelperSaveData) Instance._SaveData;

        private ComfCounterInChapterPanel chapterPanel;

        private Dictionary<LevelSideID, int> comfLevelTotals = new Dictionary<LevelSideID, int>();
        public static Dictionary<LevelSideID, int> ComfLevelTotals => Instance.comfLevelTotals;
        

        public DoonvHelperModule() {
            Instance = this;
            chapterPanel = new ComfCounterInChapterPanel();
                        
            #if DEBUG
                // debug builds use info logging
                Logger.SetLogLevel(nameof(DoonvHelperModule), LogLevel.Info);
            #else
                // release builds use verbose logging to reduce spam in log files
                Logger.SetLogLevel(nameof(DoonvHelperModule), LogLevel.Verbose);
            #endif
        }
        private Hook gunelineHook;
        public override void Load() {
            
            On.Celeste.Level.Begin += ModLevelBegin;
            if (Everest.Loader.DependencyLoaded(new() {
                Name = "Guneline",
                Version = new Version(1, 0, 0)
            })) {
                HookGuneline();
            }
            // IL.Celeste.Mod.Gun
            // On.Celeste.Mod.Bul
            chapterPanel.HookMethods();
        }

        private void HookGuneline()
        {
            
            gunelineHook = new Hook(
                typeof(Guneline.Bullet).GetMethod("CollisionCheck", BindingFlags.NonPublic | BindingFlags.Instance), 
                modGunelineBulletCollisionCheck
            );
        }

        /// <summary>
        /// TODO: Remove this
        /// </summary>
        private void modGunelineBulletCollisionCheck(Action<Bullet> orig, Guneline.Bullet bullet) {
            DynamicData bulletData = DynamicData.For(bullet);
            CustomNPC enemy = bulletData.Get<Actor>("owner").Scene.CollideFirst<CustomNPC>(bullet.Hitbox);
            if (enemy != null && !bulletData.Get<bool>("dead"))
            {
                enemy.Kill();
                bulletData.Invoke("Kill");
                return;
            }
            orig(bullet);
        }

        public override void Unload() {
            On.Celeste.Level.Begin -= ModLevelBegin;
            gunelineHook?.Dispose();
            chapterPanel.UnhookMethods();
        }
        public override void PrepareMapDataProcessors(MapDataFixup context) {
            context.Add<DoonvHelperMapDataProcessor>();
        }


        /// <summary>
        /// This is a hook that overrides the `Level.Begin` method.
        /// </summary>
        private void ModLevelBegin(On.Celeste.Level.orig_Begin orig, Level level)
        {
            orig(level); // Call original method that we have overriden so we maintain the original functionality.

            if (ComfLevelTotals.SafeGet(new LevelSideID(level)) > 0) {
                // Add the In-game comf display so we can see our comf counter in game.
                level.Add(new Entities.ComfInGameDisplay());
            }
            
        }
        


    }
}