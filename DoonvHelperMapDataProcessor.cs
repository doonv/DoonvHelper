using System;
using System.Collections.Generic;

namespace Celeste.Mod.DoonvHelper
{
    // private void ModMapDataLoad(On.Celeste.MapData.orig_Load orig, MapData map)
    // {
    //     orig(map);
    //     if (map.Area.GetLevelSet() == "Celeste") {
    //         return;
    //     }
    //     int comfs = 0;
    //     foreach (LevelData level in map.Levels)
    //     {
    //         foreach (EntityData trigger in level.Triggers)
    //         {
    //             if (trigger.Name == "DoonvHelper/ComfySpot") {
    //                 comfs++;
    //             }
    //         }
    //     }
    //     ComfLevelTotals[new LevelSideID(map.Area)] = comfs;
    //     Logger.Log(LogLevel.Info, "DoonvHelper", String.Format(
    //         "Storing Comf Info for ({0}, Ch. {1}, {2} Side): {3} comfs in level.", 
    //         String.IsNullOrWhiteSpace(map.Area.GetSID()) ? map.Area.GetLevelSet() : map.Area.GetSID(),
    //         map.Area.ChapterIndex,
    //         map.Area.Mode == AreaMode.Normal ? "A" : (map.Area.Mode == AreaMode.BSide ? "B" : "C"),
    //         ComfLevelTotals[new LevelSideID(map.Area)]
    //     ));
    // }
    public class DoonvHelperMapDataProcessor : EverestMapDataProcessor {
        private int comfs = 0;

        public override void End() {
            Logger.Log(LogLevel.Info, "DoonvHelper", String.Format(
                "Storing Comf Info for {0} Ch. {1} {2}-Side: {3} comfs in level.", 
                String.IsNullOrWhiteSpace(this.AreaKey.GetSID()) ? this.AreaKey.GetLevelSet() : this.AreaKey.GetSID(),
                this.AreaKey.ChapterIndex,
                this.AreaKey.Mode == AreaMode.Normal ? "A" : (this.AreaKey.Mode == AreaMode.BSide ? "B" : "C"),
                comfs
            ));
            DoonvHelperModule.ComfLevelTotals[new LevelSideID(this.AreaKey)] = comfs;
        }

        public override Dictionary<string, Action<BinaryPacker.Element>> Init() {
            return new Dictionary<string, Action<BinaryPacker.Element>> {
                {
                    //! This is dumb but it appears this function doesn't support triggers 
                    "triggers",
                    triggerList => {
                        foreach (BinaryPacker.Element trigger in triggerList.Children) {
                            if (trigger.Name == "DoonvHelper/ComfySpot") {
                                comfs++;
                            }
                        }
                    }
                }
            };
        }

        public override void Reset() {
            comfs = 0;
        }
    }
}