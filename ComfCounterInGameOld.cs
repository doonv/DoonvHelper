// Celeste.TotalStrawberriesDisplay
using System;
using System.Collections.Generic;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.DoonvHelper.Entities
{
    public class ComfDisplayOld : Entity
    {
        private const float NumberUpdateDelay = 0.4f;

        private const float ComboUpdateDelay = 0.3f;

        private const float AfterUpdateDelay = 2f;

        private const float LerpInSpeed = 1.2f;

        private const float LerpOutSpeed = 2f;

        public static readonly Color FlashColor = Calc.HexToColor("FF5E76");

        private MTexture bg;

        public float DrawLerp;

        private float strawberriesUpdateTimer;

        private float strawberriesWaitTimer;
        private MTexture xIcon;
        private MTexture comfIcon;
        private List<EntityID> comfdata;
        private int comfCountDisplayed = 0;
        private Wiggler comfWiggler;
        private float flashTimer;

        // private SpriteBatch test;

        public ComfDisplayOld()
        {
            // this.test = new SpriteBatch(Monocle.Draw.SpriteBatch.GraphicsDevice);
            base.Y = 96f + 96f;
            base.Depth = -101;
            base.Tag = (int)Tags.HUD | (int)Tags.Global | (int)Tags.PauseUpdate | (int)Tags.TransitionUpdate;
            xIcon = GFX.Gui["x"];
            bg = GFX.Gui["strawberryCountBG"];
            comfIcon = GFX.Gui["DoonvHelper/comf/comfer"];
            comfWiggler = Wiggler.Create(0.5f, 3f);
            comfWiggler.StartZero = true;
            comfWiggler.UseRawDeltaTime = true;
            // Add(strawberries = new StrawberriesCounter(centeredX: false, SaveData.Instance.TotalStrawberries_Safe));
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);

            Logger.Log(LogLevel.Info, "DoonvHelper", "comf counter added");
            this.comfdata = DoonvHelperModule.SaveData.ComfLevelData[new LevelSideID(scene)];
            this.comfCountDisplayed = comfdata.Count;
        }
        public override void Removed(Scene scene)
        {
            base.Removed(scene);
            Logger.Log(LogLevel.Info, "DoonvHelper", "comf counter removed");
        }

        public override void Update()
        {
            base.Update();
            Level level = base.Scene as Level;
            if (comfWiggler.Active) {
                comfWiggler.Update();
            }
            if (flashTimer > 0f)
            {
                flashTimer -= Engine.RawDeltaTime;
            }
            if (comfdata.Count > comfCountDisplayed && strawberriesUpdateTimer <= 0f)
            {
                strawberriesUpdateTimer = 0.4f;
            }
            if (strawberriesUpdateTimer > 0f || strawberriesWaitTimer > 0f || (level.Paused && level.PauseMainMenuOpen))
            {
                DrawLerp = Calc.Approach(DrawLerp, 1f, 1.2f * Engine.RawDeltaTime);
            }
            else
            {
                DrawLerp = Calc.Approach(DrawLerp, 0f, 2f * Engine.RawDeltaTime);
            }
            if (strawberriesWaitTimer > 0f)
            {
                strawberriesWaitTimer -= Engine.RawDeltaTime;
            }
            if (strawberriesUpdateTimer > 0f && DrawLerp == 1f)
            {
                strawberriesUpdateTimer -= Engine.RawDeltaTime;
                if (strawberriesUpdateTimer <= 0f)
                {
                    if (comfCountDisplayed < comfdata.Count)
                    {
                        comfCountDisplayed++;
                        Audio.Play("event:/ui/game/increment_strawberry");
                        comfWiggler.Start();
                        flashTimer = 0.5f;
                    }
                    strawberriesWaitTimer = 2f;
                    if (comfCountDisplayed < comfdata.Count)
                    {
                        strawberriesUpdateTimer = 0.3f;
                    }
                }
            }
            if (Visible)
            {
                float num = 96f + 96f;
                if (!level.TimerHidden)
                {
                    if (Settings.Instance.SpeedrunClock == SpeedrunType.Chapter)
                    {
                        num += 58f;
                    }
                    else if (Settings.Instance.SpeedrunClock == SpeedrunType.File)
                    {
                        num += 78f;
                    }
                }
                base.Y = Calc.Approach(base.Y, num, Engine.DeltaTime * 800f);
            }
            Visible = DrawLerp > 0f;
        }

        // Most of this is copypasted code. I tried my best to give comments.
        public override void Render()
        {
            // Set up positioning
            Vector2 vec = Vector2.Lerp(new Vector2(-bg.Width, base.Y), new Vector2(32f, base.Y), Ease.CubeOut(DrawLerp));
            vec = vec.Round();
            Vector2 renderPosition = (vec + new Vector2(0f, 0f - base.Y)) + this.Position;

            // Draw the background
            bg.DrawJustified(vec + new Vector2(-96f, 12f), new Vector2(0f, 0.5f));

            Vector2 upVector = new Vector2(1f, 0f);
            Vector2 vector2 = new Vector2(- upVector.Y, upVector.X);
            Color textColor = Color.White;
            string comfCountString = comfCountDisplayed.ToString();
            float textlength = ActiveFont.Measure(comfCountString).X;
            float num3 = 62f + (float)xIcon.Width + 2f + textlength;
            if (flashTimer > 0f && base.Scene != null && base.Scene.BetweenRawInterval(0.05f))
            {
                textColor = FlashColor;
            }
            // Draw a comf icon
            //*This will render blurry on resolutions lower than <6x (Lower than 1080p). 
            // There is no fix to this as far as I know.
            comfIcon.DrawCentered(Calc.Floor(renderPosition + new Vector2(1f, 0f) * 60f * 0.5f), Color.White, 1f);
            xIcon.DrawCentered(renderPosition + new Vector2(1f, 0f) * (62f + (float)xIcon.Width * 0.5f) + vector2 * 2f, textColor, 1f);
            ActiveFont.DrawOutline(
                comfCountString, 
                renderPosition + new Vector2(1f, 0f) * (num3 - textlength * 0.5f) + vector2 * (comfWiggler.Value * 18f), 
                new Vector2(0.5f, 0.5f), 
                Vector2.One,
                textColor,
                2f,
                Color.Black
            );
        }
        
    }
}