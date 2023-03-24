using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.DoonvHelper.Utils
{
    public static class Utils {
        public static void DrawGradient(Hitbox rect, Color color1, Color color2) {
            for (float x = 0; x < rect.Width; x++)
            {
                Draw.Rect(rect.AbsoluteX + x, rect.AbsoluteY, 1f, rect.Height, Color.Lerp(color1, color2, x / rect.Width));
            }
        }
    }
    /// <summary> Extensions useful for everyone </summary>
    public static class GenericExtensions {
        public static Hitbox Floor(this Hitbox hitbox) {
            return new Hitbox(
                x: (int)Math.Floor(hitbox.Position.X),
                y: (int)Math.Floor(hitbox.Position.Y),
                width: (int)Math.Floor(hitbox.Width),
                height: (int)Math.Floor(hitbox.Height)
            );
        }
        
        /// <summary>Play an animation stored in this sprite. Doesn't crash the game if there is no animation.s</summary>
        /// <param name="id">The animation to play.</param>
        /// <param name="restart">Whether to restart the animation if it is already playing.</param>
        /// <param name="randomizeFrame">Whether to randomize the starting frame and animation timer.</param>
        public static void PlaySafe(this Sprite sprite, string id, bool restart = false, bool randomizeFrame = false) {
            if (sprite.Animations.ContainsKey(id)) {
                sprite.Play(id, restart, randomizeFrame);
            } else {
                Logger.Log(LogLevel.Error, "DoonvHelper.Utils", String.Format(
                    "Sprite {0} doesn't contain animation \"{1}\"", 
                    sprite.ToString(),
                    id
                ));
            }
        }
        public static void Draw(this MTexture texture, ref SpriteBatch batch, Vector2 position, Vector2 origin, Color color, float scale = 1f, float rotation = 0f)
        {
            batch.Draw(
                texture.Texture.Texture_Safe,
                position,
                texture.ClipRect,
                color,
                rotation,
                (origin - texture.DrawOffset) /  texture.ScaleFix,
                scale * texture.ScaleFix,
                SpriteEffects.None,
                0f
            );
        }

        /// <summary> Returns a hitbox where the size has been changed by that amount. </summary>
        /// <param name="sizeAmount">
        ///     The size change in pixels. Works best with multiples of 2 as that ensures even expansion on all sides. 
        ///     Positive amounts increase the size of the hitbox, negative amounts decrease the size.
        /// </param>
        /// <returns>A hitbox where the size has been changed by the specified amount</returns>
        public static Hitbox Inflated(this Collider hitbox, float sizeAmount) {
            return new Hitbox(
                width: hitbox.Width + sizeAmount,
                height: hitbox.Height + sizeAmount,
                x: hitbox.Position.X - (sizeAmount / 2f),
                y: hitbox.Position.Y - (sizeAmount / 2f)
            );
        }

        public static Point ToPoint(this Vector2 vector2) => new Point((int)vector2.X, (int)vector2.Y);

        /// <summary> Returns a rectangle where the size has been changed by that amount. </summary>
        /// <param name="changeX">
        ///     The horizontal size change in pixels. Works best with multiples of 2 as that ensures even expansion on both sides. 
        ///     Positive amounts increase the size of the rectangle, negative amounts decrease the size.
        /// </param>
        /// <param name="changeY">
        ///     The vertical size change in pixels. Works best with multiples of 2 as that ensures even expansion on both sides. 
        ///     Positive amounts increase the size of the rectangle, negative amounts decrease the size.
        /// </param>
        public static Rectangle Inflated(this Rectangle rect, int changeX, int changeY) {
            return new Rectangle(
                x: rect.X - (changeX / 2),
                y: rect.Y - (changeY / 2),
                width: rect.Width + changeX,
                height: rect.Height + changeY
            );
        }
    }
}
namespace Celeste.Mod.DoonvHelper.DoonvHelperUtils {
    /// <summary>
    /// Extensions only useful for DoonvHelper
    /// </summary>
    public static class DoonvHelperExtensions {
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
