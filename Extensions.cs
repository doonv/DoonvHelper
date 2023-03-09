using System.Collections.Generic;

namespace Celeste.Mod.DoonvHelper
{
    public static class Extensions {
        public static List<EntityID> SafeGet(this Dictionary<LevelSideID, List<EntityID>> comfdata, LevelSideID levelID) {
            if (!comfdata.ContainsKey(levelID)) {
                comfdata[levelID] = new List<EntityID>();
            }
            return comfdata[levelID];
        }
        public static int SafeGet(this Dictionary<LevelSideID, int> ComfLevelTotals, LevelSideID levelID) {
            if (ComfLevelTotals.ContainsKey(levelID)) {
                // Logger.Log(LogLevel.Info, "DoonvHelper", "god");
                return ComfLevelTotals[levelID];
            }
            // Logger.Log(LogLevel.Warn, "DoonvHelper", "the big no no!");
            return 0;
        }
    }
}