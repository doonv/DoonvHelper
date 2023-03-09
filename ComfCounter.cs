// Celeste.StrawberriesCounter
using System;
using Celeste.Mod;
using Microsoft.Xna.Framework;
using Monocle;
namespace Celeste.Mod.DoonvHelper.Entities
{
    /// <summary>
    /// 
    /// </summary>
    public class ComfCounter : Component
    {
        public static readonly Color FlashColor = Calc.HexToColor("FF5E76");
        private const int IconWidth = 60;
        public Vector2 Position;
        public bool CenteredX;
        public bool CanWiggle = true;
        public float Scale = 1f;
        public float Stroke = 2f;
        public float Rotation;
        public Color color = Color.White;
        public Color OutOfColor = Color.LightGray;
        public bool OverworldSfx;
        private int amount;
        private int outOf = -1;
        private Wiggler wiggler;
        private float flashTimer;
        private string sAmount;
        private string sOutOf;
        private MTexture comfIcon;
        private MTexture xIcon;
        private bool showOutOf;

        public int Amount
        {
            get
            {
                return amount;
            }
            set
            {
                if (amount == value)
                {
                    return;
                }
                amount = value;
                UpdateStrings();
                if (CanWiggle)
                {
                    if (OverworldSfx)
                    {
                        Audio.Play("event:/ui/postgame/strawberry_count");
                    }
                    else
                    {
                        Audio.Play("event:/ui/game/increment_strawberry");
                    }
                    wiggler.Start();
                    flashTimer = 0.5f;
                }
            }
        }
        public int OutOf
        {
            get
            {
                return outOf;
            }
            set
            {
                outOf = value;
                UpdateStrings();
            }
        }
        public bool ShowOutOf
        {
            get
            {
                return showOutOf;
            }
            set
            {
                if (showOutOf != value)
                {
                    showOutOf = value;
                    UpdateStrings();
                }
            }
        }

        public float FullHeight => Math.Max(ActiveFont.LineHeight, comfIcon.Height);
        public Vector2 RenderPosition => (((base.Entity != null) ? base.Entity.Position : Vector2.Zero) + Position).Round();

        public ComfCounter(bool centeredX, int amount, int outOf = 0, bool showOutOf = false, MTexture texture = null)
            : base(active: true, visible: true)
        {
            CenteredX = centeredX;
            this.amount = amount;
            this.outOf = outOf;
            this.showOutOf = showOutOf;
            UpdateStrings();
            wiggler = Wiggler.Create(0.5f, 3f);
            wiggler.StartZero = true;
            wiggler.UseRawDeltaTime = true;
            comfIcon = texture ?? GFX.Gui["DoonvHelper/comf/comfer"];
            xIcon = GFX.Gui["x"];
        }

        private void UpdateStrings()
        {
            sAmount = amount.ToString();
            if (outOf > -1)
            {
                sOutOf = "/" + outOf;
            }
            else
            {
                sOutOf = "";
            }
        }

        public void Wiggle()
        {
            wiggler.Start();
            flashTimer = 0.5f;
        }

        public override void Update()
        {
            base.Update();
            if (wiggler.Active)
            {
                wiggler.Update();
            }
            if (flashTimer > 0f)
            {
                flashTimer -= Engine.RawDeltaTime;
            }
        }

        public override void Render()
        {
            Vector2 upVector = new Vector2(1f, 0f);
            Vector2 vector2 = new Vector2(- upVector.Y, upVector.X);
            Color textColor = Color.White;
            Vector2 renderPosition = RenderPosition;
            string outOfText = (showOutOf ? sOutOf : "");
            string comfCountString = sAmount;
            float textlength = ActiveFont.Measure(comfCountString).X;
            float outOfTextLength = ActiveFont.Measure(outOfText).X;
            float num3 = 62f + (float)xIcon.Width + 2f + outOfTextLength + textlength;
            if (flashTimer > 0f && base.Scene != null && base.Scene.BetweenRawInterval(0.05f))
            {
                textColor = FlashColor;
            }
            if (CenteredX)
            {
                renderPosition -= upVector * (num3 / 2f) * Scale;
            }
            // Draw a comf icon
            //*The comfIcon will render blurry on resolutions lower than 6x pixel scale (Lower than 1080p). 
            // There is no fix to this as far as I know.
            comfIcon.DrawCentered(Calc.Floor(renderPosition + new Vector2(1f, 0f) * 60f * 0.5f), Color.White, 1f);
            xIcon.DrawCentered(renderPosition + new Vector2(1f, 0f) * (62f + (float)xIcon.Width * 0.5f) + vector2 * 2f, textColor, 1f);
            ActiveFont.DrawOutline(
                comfCountString, 
                renderPosition + new Vector2(1f, 0f) * (num3 - outOfTextLength - textlength * 0.5f) + vector2 * (wiggler.Value * 18f), 
                new Vector2(0.5f, 0.5f), 
                Vector2.One,
                textColor,
                2f,
                Color.Black
            );
            if (outOfText != "")
            {
                ActiveFont.DrawOutline(outOfText, renderPosition + upVector * (num3 - outOfTextLength / 2f) * Scale, new Vector2(0.5f, 0.5f), Vector2.One * Scale, OutOfColor, Stroke, Color.Black);
            }
        }
    }
}