using System.Collections.Generic;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.DoonvHelper.Entities {
    [CustomEntity("DoonvHelper/ComfySpot")]
    public class ComfySpot : Trigger {
        private LevelSideID levelID;
        private EntityID entityID;
        public ComfySpot(EntityData data, Vector2 offset) 
            : base(data, offset) {
            entityID = new EntityID(data.Level.Name, data.ID);
        }
        public override void Added(Scene scene)
        {
            base.Added(scene);
            levelID = new LevelSideID(scene);
        }

        public override void OnEnter(Player player) {
            Logger.Log(LogLevel.Info, "DoonvHelper", "Enter");
            if (!DoonvHelperModule.SaveData.ComfLevelData[levelID].Contains(entityID)) {
                DoonvHelperModule.SaveData.ComfLevelData[levelID].Add(entityID);
                (Scene as Level).Add(new SummitCheckpoint.ConfettiRenderer(player.Position));
            }

            foreach(EntityID item in DoonvHelperModule.SaveData.ComfLevelData[levelID])
            {
                Logger.Log(LogLevel.Info, "DoonvHelper", item.ToString());
            }
            base.OnEnter(player);
        }

        public override void OnStay(Player player) {
            base.OnStay(player);
        }

        public override void OnLeave(Player player) {
            Logger.Log(LogLevel.Info, "DoonvHelper", "Leave");
            base.OnLeave(player);
        }
    }
}