using System;
using System.Collections.Generic;

namespace Celeste.Mod.DoonvHelper {
    public class DoonvHelperSaveData : EverestModuleSaveData {
        /// <summary>
        /// Stores the `EntityID`s and SID of comf spots in order to track them.
        /// </summary>
        /// <value>A dictionary where the key is the level ID and where the value is a list of all comf spots in that level.</value>
        public Dictionary<LevelSideID, List<EntityID>> ComfLevelData { get; set; } 
            = new Dictionary<LevelSideID, List<EntityID>>();
    }
}
