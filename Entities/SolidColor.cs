using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.DoonvHelper.Entities {
    
    [CustomEntity("DoonvHelper/SolidColor")]
    public class SolidColor : Entity {

        private Color color;
        private float width;
        private float height;
        public SolidColor(Vector2 position, float width, float height, Color color, int depth = 5000)
            : base(position) 
        {
            this.width = width;
            this.height = height;
            this.color = color;
            this.Depth = depth;
        }
        public SolidColor(EntityData data, Vector2 offset)
            : this(
                data.Position + offset, 
                data.Width, data.Height, 
                Calc.HexToColor(data.Attr("color", defaultValue: "6969ee")),
                data.Int("depth", 5000)
            ) 
        {
        }

        public override void Render()
        {
            Draw.Rect(base.X, base.Y, width, height, color);
            // base.Render();
        }
    }
}
