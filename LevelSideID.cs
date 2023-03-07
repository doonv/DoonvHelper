using System;
using System.Collections.Generic;
using Monocle;

namespace Celeste.Mod.DoonvHelper
{
    /// <summary>
    /// A unique level/area/chapter identifier.
    /// </summary>
    public struct LevelSideID {
        /// <summary>
        /// The SID (String Identifier) of the level/area/chapter
        /// </summary>
        public string levelSID;
        /// <summary>
        /// The side type (A/B/C) of the level/area/chapter
        /// </summary>
        public AreaMode side;
        public LevelSideID(Session session) {
            levelSID = session.Area.GetSID();
            side = session.Area.Mode;
        }
        public LevelSideID(Level level) 
            : this(level.Session) {}
        public LevelSideID(Scene scene) 
            : this((scene as Level).Session) {}
        public LevelSideID(AreaKey areakey) {
            levelSID = areakey.GetSID();
            side = areakey.Mode;
        }
        
    }
}
