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
        /// <summary>
        /// Returns a <see cref="Hitbox"/> where every value has been rounded down.
        /// </summary>
        public static Hitbox Floor(this Hitbox hitbox) =>
            new Hitbox(
                x: (int)Math.Floor(hitbox.Position.X),
                y: (int)Math.Floor(hitbox.Position.Y),
                width: (int)Math.Floor(hitbox.Width),
                height: (int)Math.Floor(hitbox.Height)
            );

        /// <summary>
        /// Returns a <see cref="Hitbox"/> where the <paramref name="position"/> vector has been added to its position.
        /// </summary>
        /// <param name="position">The <see cref="Vector2"/> position to add to the hitbox.</param>
        public static Hitbox AddPosition(this Collider hitbox, Vector2 position) => 
            new Hitbox(
                x: hitbox.Position.X + position.X,
                y: hitbox.Position.Y + position.Y,
                width: hitbox.Width,
                height: hitbox.Height
            );
        
        
        /// <summary>Play an animation stored in this sprite. Doesn't crash the game if the animation doesn't exist.</summary>
        /// <param name="id">The animation to play.</param>
        /// <param name="restart">Whether to restart the animation if it is already playing.</param>
        /// <param name="randomizeFrame">Whether to randomize the starting frame and animation timer.</param>
        public static void PlaySafe(this Sprite sprite, string id, bool restart = false, bool randomizeFrame = false) {
            if (sprite.Has(id)) {
                sprite.Play(id, restart, randomizeFrame);
            } else {
                Logger.Log(LogLevel.Error, "DoonvHelper.Utils", $"Sprite {sprite.Path} doesn't contain animation \"{id}\".");
            }
        }

        /// <summary>
        ///     Play animations stored in this sprite. Plays all the animations put in until one is valid.
        ///     Doesn't crash the game if the animations don't exist. 
        /// </summary>
        /// <param name="ids">A list of animations to play.</param>
        public static void PlaySafe(this Sprite sprite, params string[] ids) {
            for (int i = 0; i < ids.Length; i++)
            {
                string id = ids[i];
                if (sprite.Has(id)) {
                    sprite.Play(id, false, false);
                    return;
                }
            }
            Logger.Log(LogLevel.Error, "DoonvHelper.Utils", String.Format(
                "Sprite {0} doesn't contain any of the requested animations: {1}.", 
                sprite.Path,
                String.Join(", ", Array.ConvertAll(ids, id => String.Format("\"{0}\"", id)))
            ));
        }

        /// <summary>
        ///     Play animations stored in this sprite. Plays all the animations put in until one is valid.
        ///     Doesn't crash the game if the animations don't exist. 
        /// </summary>
        /// <param name="restart">Whether to restart the animation if it is already playing.</param>
        /// <param name="randomizeFrame">Whether to randomize the starting frame and animation timer.</param>
        /// <param name="ids">A list of animations to play.</param>
        public static void PlaySafe(this Sprite sprite, bool restart, bool randomizeFrame, params string[] ids) {
            for (int i = 0; i < ids.Length; i++)
            {
                string id = ids[i];
                if (sprite.Has(id)) {
                    sprite.Play(id, restart, randomizeFrame);
                    return;
                }
            }
            Logger.Log(LogLevel.Error, "DoonvHelper.Utils", String.Format(
                "Sprite {0} doesn't contain any of the requested animations: {1}.", 
                sprite.Path,
                String.Join(", ", Array.ConvertAll(ids, id => String.Format("\"{0}\"", id)))
            ));
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

        /// <summary> Returns a hitbox where the size has been changed by <paramref name="sizeAmount">. </summary>
        /// <param name="sizeAmount">
        ///     The size change in pixels. Works best with multiples of 2 as that ensures even expansion on all sides. 
        ///     Positive amounts increase the size of the hitbox, negative amounts decrease the size.
        /// </param>
        /// <returns>A hitbox where the size has been changed by the specified amount</returns>
        public static Hitbox Inflated(this Collider hitbox, float sizeAmount) =>
            new Hitbox(
                width: hitbox.Width + sizeAmount,
                height: hitbox.Height + sizeAmount,
                x: hitbox.Position.X - (sizeAmount / 2f),
                y: hitbox.Position.Y - (sizeAmount / 2f)
            );
        
        /// <summary>
        /// Converts the <paramref name="Vector2"/> into a <see cref="Point"/>.
        /// </summary>
        public static Point ToPoint(this Vector2 vector2) => new Point((int)vector2.X, (int)vector2.Y);

        /// <summary> 
        ///     Returns a rectangle where the size has been changed by <paramref name="changeX"/> and <paramref name="changeY"/>.
        /// </summary>
        /// <param name="changeX">
        ///     The horizontal size change in pixels. Works best with multiples of 2 as that ensures even expansion on both sides. 
        ///     Positive amounts increase the size of the rectangle, negative amounts decrease the size.
        /// </param>
        /// <param name="changeY">
        ///     The vertical size change in pixels. Works best with multiples of 2 as that ensures even expansion on both sides. 
        ///     Positive amounts increase the size of the rectangle, negative amounts decrease the size.
        /// </param>
        public static Rectangle Inflated(this Rectangle rect, int changeX, int changeY) =>
            new Rectangle(
                x: rect.X - (changeX / 2),
                y: rect.Y - (changeY / 2),
                width: rect.Width + changeX,
                height: rect.Height + changeY
            );
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
