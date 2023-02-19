using System;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.DoonvHelper {
    public class DoonvHelperModule : EverestModule {
        public static DoonvHelperModule Instance { get; private set; }

        public override Type SettingsType => typeof(DoonvHelperModuleSettings);
        public static DoonvHelperModuleSettings Settings => (DoonvHelperModuleSettings) Instance._Settings;

        public override Type SessionType => typeof(DoonvHelperModuleSession);
        public static DoonvHelperModuleSession Session => (DoonvHelperModuleSession) Instance._Session;

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

        public override void Load() {
            // TODO: apply any hooks that should always be active
        }

        public override void Unload() {
            // TODO: unapply any hooks applied in Load()
        }
    }
}