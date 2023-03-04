using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.DoonvHelper.Entities {
    
    [CustomEntity("DoonvHelper/ColorBlock")]
    public class ColorBlock : Entity {

        private Color color;
        private float width;
        private float height;
        private float alpha;
        public ColorBlock(Vector2 position, float width, float height, Color color, float alpha = 1.0f, int depth = 5000)
            : base(position) 
        {
            this.width = width;
            this.height = height;
            this.color = color;
            this.Depth = depth;
            this.alpha = alpha;
        }
        public ColorBlock(EntityData data, Vector2 offset)
            : this(
                data.Position + offset, 
                data.Width, data.Height, 
                Calc.HexToColor(data.Attr("color", defaultValue: "6969ee")),
                data.Float("alpha", defaultValue: 1.0f),
                data.Int("depth", defaultValue: 5000)
            ) 
        {
        }
        
        // World's most complicated function
        public override void Render()
        {
            Draw.Rect(base.X, base.Y, width, height, color * alpha);
            // base.Render();
        }
    }
}
