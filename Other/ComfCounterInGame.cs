using System.Collections.Generic;
using Celeste;
using Celeste.Mod.DoonvHelper.DoonvHelperUtils;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.DoonvHelper.Entities;

public class ComfInGameDisplay : Entity
{
	public const float YOffset = 96f;
	private MTexture bg;
	public float drawLerp;
	private float comfUpdateTimer;
	private float comfWaitTimer;
	private ComfCounter comfCounter;
	private List<EntityID> comfdata;

	public ComfInGameDisplay()
	{
		base.Y = 96f + YOffset;
		base.Depth = -101;
		base.Tag = (int)Tags.HUD | (int)Tags.Global | (int)Tags.PauseUpdate | (int)Tags.TransitionUpdate;
		bg = GFX.Gui["strawberryCountBG"];
	}
	public override void Added(Scene scene)
	{
		base.Added(scene);
		this.comfdata = DoonvHelperModule.SaveData.ComfLevelData.SafeGet(new LevelSideID(scene));
		Add(comfCounter = new ComfCounter(centeredX: false, comfdata.Count));
	}

	public override void Update()
	{
		// Logger.Log(LogLevel.Info, "DoonvHelper", comfCounter.Position.ToString());
		base.Update();
		Level level = base.Scene as Level;
		if (comfdata.Count > comfCounter.Amount && comfUpdateTimer <= 0f)
		{
			comfUpdateTimer = 0.4f;
		}
		if (comfdata.Count > comfCounter.Amount || comfUpdateTimer > 0f || comfWaitTimer > 0f || (level.Paused && level.PauseMainMenuOpen))
		{
			drawLerp = Calc.Approach(drawLerp, 1f, 1.2f * Engine.RawDeltaTime);
		}
		else
		{
			drawLerp = Calc.Approach(drawLerp, 0f, 2f * Engine.RawDeltaTime);
		}
		if (comfWaitTimer > 0f)
		{
			comfWaitTimer -= Engine.RawDeltaTime;
		}
		if (comfUpdateTimer > 0f && drawLerp == 1f)
		{
			comfUpdateTimer -= Engine.RawDeltaTime;
			if (comfUpdateTimer <= 0f)
			{
				if (comfCounter.Amount < comfdata.Count)
				{
					comfCounter.Amount++;
				}
				comfWaitTimer = 2f;
				if (comfCounter.Amount < comfdata.Count)
				{
					comfUpdateTimer = 0.3f;
				}
			}
		}
		if (Visible)
		{
			float num = 96f + YOffset;
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
		Visible = drawLerp > 0f;
	}

	public override void Render()
	{
		Vector2 vec = Vector2.Lerp(new Vector2(-bg.Width, base.Y), new Vector2(32f, base.Y), Ease.CubeOut(drawLerp)).Round();
		bg.DrawJustified(vec + new Vector2(-96f, 12f), new Vector2(0f, 0.5f));
		comfCounter.Position = vec + new Vector2(0f, 0f - base.Y);
		comfCounter.Render();
	}
}
