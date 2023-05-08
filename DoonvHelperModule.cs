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
using MonoMod.ModInterop;

namespace Celeste.Mod.DoonvHelper;

public class DoonvHelperModule : EverestModule
{
    public static DoonvHelperModule Instance { get; private set; }

    public override Type SettingsType => typeof(DoonvHelperModuleSettings);
    public static DoonvHelperModuleSettings Settings => (DoonvHelperModuleSettings)Instance._Settings;

    public override Type SessionType => typeof(DoonvHelperModuleSession);
    public static DoonvHelperModuleSession Session => (DoonvHelperModuleSession)Instance._Session;

    public override Type SaveDataType => typeof(DoonvHelperSaveData);
    public static DoonvHelperSaveData SaveData => (DoonvHelperSaveData)Instance._SaveData;

    private ComfCounterInChapterPanel chapterPanel;

    private Dictionary<LevelSideID, int> comfLevelTotals = new Dictionary<LevelSideID, int>();
    public static Dictionary<LevelSideID, int> ComfLevelTotals => Instance.comfLevelTotals;

    private LuaCutscenes.LuaCutscenesMod luaCutscenesModule = null;
    public static LuaCutscenes.LuaCutscenesMod LuaCutscenesModule => Instance.luaCutscenesModule;

    [ModImportName("FrostHelper")]
    public static class FrostHelperImports {
        public static Func<string, Type> EntityNameToType;
    }

    public DoonvHelperModule()
    {
        Instance = this;
        chapterPanel = new ComfCounterInChapterPanel();

        #if DEBUG
            // Debug Builds log any level of logging.
            Logger.SetLogLevel("DoonvHelper", LogLevel.Verbose);
        #else
            // Release builds only log log levels higher or equal to `LogLevel.Info`.
            Logger.SetLogLevel("DoonvHelper", LogLevel.Info);
        #endif
    }
    private Hook gunelineHook;
    public override void Load()
    {
        typeof(FrostHelperImports).ModInterop();

        On.Celeste.Level.Begin += ModLevelBegin;
        if (Everest.Loader.DependencyLoaded(new()
        {
            Name = "Guneline",
            Version = new Version(1, 0, 0)
        }))
        {
            HookGuneline();
        }
        if (Everest.Loader.TryGetDependency(new()
        {
            Name = "LuaCutscenes",
            Version = new Version(0, 2, 7)
        }, out EverestModule module))
        {
            luaCutscenesModule = (LuaCutscenes.LuaCutscenesMod)module;
        }
        chapterPanel.HookMethods();
    }

    /// <summary> TODO: Remove this when Guneline 2 comes out  </summary>
    private void HookGuneline()
    {
        gunelineHook = new Hook(
            typeof(Guneline.Bullet).GetMethod("CollisionCheck", BindingFlags.NonPublic | BindingFlags.Instance),
            modGunelineBulletCollisionCheck
        );
    }

    /// <summary> TODO: Remove this when Guneline 2 comes out  </summary>
    private void modGunelineBulletCollisionCheck(Action<Bullet> orig, Guneline.Bullet bullet)
    {
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

    public override void Unload()
    {
        On.Celeste.Level.Begin -= ModLevelBegin;
        gunelineHook?.Dispose();
        chapterPanel.UnhookMethods();
    }
    public override void PrepareMapDataProcessors(MapDataFixup context)
    {
        context.Add<DoonvHelperMapDataProcessor>();
    }


    /// <summary>
    /// This is a hook that overrides the `Level.Begin` method.
    /// </summary>
    private void ModLevelBegin(On.Celeste.Level.orig_Begin orig, Level level)
    {
        orig(level); // Call original method that we have overriden so we maintain the original functionality.

        if (ComfLevelTotals.SafeGet(new LevelSideID(level)) > 0)
        {
            // Add the In-game comf display so we can see our comf counter in game.
            level.Add(new Entities.ComfInGameDisplay());
        }
    }
}
