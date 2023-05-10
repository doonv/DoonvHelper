// Celeste.CS10_FinalLaunch
using System;
using System.Collections;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.DoonvHelper.Entities;

internal class CustomBadelineBoostCutscene : CutsceneEntity
{
	private Player player;
	private CustomBadelineBoost boost;
	private BirdNPC bird;
	private float fadeToWhite;
	private Vector2 birdScreenPosition;
	private AscendManager.Streaks streaks;
	private Vector2 cameraWaveOffset;
	private Vector2 cameraOffset;
	private float timer;
	private Coroutine wave;
	private bool hasGolden;
	private string sayDialog;
	private bool haveBird;
	private string teleportTo;
	private string goldenTeleportTo;

	public CustomBadelineBoostCutscene(
		Player player,
		CustomBadelineBoost boost,
		string sayDialog = "CH9_LAST_BOOST",
		string teleportTo = "",
		string goldenTeleportTo = "",
		bool haveBird = true
	)
	{
		this.player = player;
		this.boost = boost;
		this.sayDialog = sayDialog;
		this.teleportTo = teleportTo;
		this.goldenTeleportTo = goldenTeleportTo;
		this.haveBird = haveBird;
		base.Depth = 10010;
	}

	public override void OnBegin(Level level)
	{
		if (String.IsNullOrWhiteSpace(teleportTo))
		{
			Add(new Coroutine(CutsceneDialogOnly()));
			return;
		}
		Audio.SetMusic(null);
		ScreenWipe.WipeColor = Color.White;
		hasGolden = false;
		foreach (Follower follower in player.Leader.Followers)
		{
			if (follower.Entity is Strawberry strawberry && strawberry.Golden)
			{
				hasGolden = true;
				break;
			}
		}
		Add(new Coroutine(Cutscene()));
	}

	private IEnumerator CutsceneDialogOnly()
	{
		if (!String.IsNullOrWhiteSpace(sayDialog))
		{
			yield return Textbox.Say(sayDialog);
		}
		EndCutscene(Level);
	}
	private IEnumerator Cutscene()
	{
		Engine.TimeRate = 1f;
		boost.Active = false;
		yield return null;
		if (!String.IsNullOrWhiteSpace(sayDialog))
		{
			yield return Textbox.Say(sayDialog);
		}
		else
		{
			yield return 0.152f;
		}
		cameraOffset = new Vector2(0f, -20f);
		boost.Active = true;
		player.EnforceLevelBounds = false;
		yield return null;
		BlackholeBG blackholeBG = Level.Background.Get<BlackholeBG>();
		if (blackholeBG is not null)
		{
			blackholeBG.Direction = -2.5f;
			blackholeBG.SnapStrength(Level, BlackholeBG.Strengths.High);
			blackholeBG.CenterOffset.Y = 100f;
			blackholeBG.OffsetOffset.Y = -50f;
		}
		Add(wave = new Coroutine(WaveCamera()));
		if (haveBird)
		{
			Add(new Coroutine(BirdRoutine(0.8f)));
		}
		Level.Add(streaks = new AscendManager.Streaks(null));
		float p2;
		for (p2 = 0f; p2 < 1f; p2 += Engine.DeltaTime / 12f)
		{
			fadeToWhite = p2;
			streaks.Alpha = p2;
			foreach (Parallax item in Level.Foreground.GetEach<Parallax>("blackhole"))
			{
				item.FadeAlphaMultiplier = 1f - p2;
			}
			yield return null;
		}
		while (bird is not null)
		{
			yield return null;
		}
		ScreenWipe.WipeColor = Color.White;
		FadeWipe wipe = new FadeWipe(Level, wipeIn: false)
		{
			Duration = 4f
		};
		if (!hasGolden)
		{
			Audio.SetMusic("event:/new_content/music/lvl10/granny_farewell");
		}
		p2 = cameraOffset.Y;
		int to = 180;
		for (float p = 0f; p < 1f; p += Engine.DeltaTime / 2f)
		{
			cameraOffset.Y = p2 + ((float)to - p2) * Ease.BigBackIn(p);
			yield return null;
		}
		yield return wipe.Wait();
		EndCutscene(Level);
	}

	public override void OnEnd(Level level)
	{
		if (String.IsNullOrWhiteSpace(teleportTo))
		{
			boost.Active = true;
			return;
		}
		if (WasSkipped && boost is not null && boost.Ch9FinalBoostSfx is not null)
		{
			boost.Ch9FinalBoostSfx.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
			boost.Ch9FinalBoostSfx.release();
		}
		string nextLevelName = teleportTo;
		Player.IntroTypes nextLevelIntro = Player.IntroTypes.None;
		// Logger.Log(LogLevel.Info, "DoonvHelper", String.Format("goo goo ga ga | {0} - {1} - {2}", hasGolden, goldenTeleportTo, teleportTo));
		if (hasGolden && !String.IsNullOrEmpty(goldenTeleportTo))
		{
			nextLevelName = goldenTeleportTo;
			nextLevelIntro = Player.IntroTypes.Jump;
		}
		Engine.TimeRate = 1f;
		Level.OnEndOfFrame += () =>
		{
			Level.TeleportTo(player, nextLevelName, nextLevelIntro);

			if (Level.Wipe is not null)
			{
				Level.Wipe.Cancel();
			}
			// if (hasGolden) {
			//     Level.SnapColorGrade("golden");
			// }
			new FadeWipe(level, wipeIn: true)
			{
				Duration = 2f
			};
			ScreenWipe.WipeColor = Color.White;

			player.Active = true;
			player.Speed = Vector2.Zero;
			player.EnforceLevelBounds = true;
			player.DummyFriction = true;
			player.DummyGravity = true;
			player.DummyAutoAnimate = true;
			player.ForceCameraUpdate = false;
			player.StateMachine.State = 0;
			player.StateMachine.Locked = false;
		};
	}

	private IEnumerator WaveCamera()
	{
		float timer = 0f;
		while (true)
		{
			cameraWaveOffset.X = (float)Math.Sin(timer) * 16f;
			cameraWaveOffset.Y = (float)Math.Sin(timer * 0.5f) * 16f + (float)Math.Sin(timer * 0.25f) * 8f;
			timer += Engine.DeltaTime * 2f;
			yield return null;
		}
	}

	private IEnumerator BirdRoutine(float delay)
	{
		yield return delay;
		Level.Add(bird = new BirdNPC(Vector2.Zero, BirdNPC.Modes.None));
		bird.Sprite.Play("flyupIdle");
		Vector2 vector = new Vector2(320f, 180f) / 2f;
		Vector2 topCenter = new Vector2(vector.X, 0f);
		Vector2 vector2 = new Vector2(vector.X, 180f);
		Vector2 from2 = vector2 + new Vector2(40f, 40f);
		Vector2 to2 = vector + new Vector2(-32f, -24f);
		for (float t3 = 0f; t3 < 1f; t3 += Engine.DeltaTime / 4f)
		{
			birdScreenPosition = from2 + (to2 - from2) * Ease.BackOut(t3);
			yield return null;
		}
		bird.Sprite.Play("flyupRoll");
		for (float t3 = 0f; t3 < 1f; t3 += Engine.DeltaTime / 2f)
		{
			birdScreenPosition = to2 + new Vector2(64f, 0f) * Ease.CubeInOut(t3);
			yield return null;
		}
		to2 = birdScreenPosition;
		from2 = topCenter + new Vector2(-40f, -100f);
		bool playedAnim = false;
		for (float t3 = 0f; t3 < 1f; t3 += Engine.DeltaTime / 4f)
		{
			if (t3 >= 0.35f && !playedAnim)
			{
				bird.Sprite.Play("flyupRoll");
				playedAnim = true;
			}
			birdScreenPosition = to2 + (from2 - to2) * Ease.BigBackIn(t3);
			birdScreenPosition.X += t3 * 32f;
			yield return null;
		}
		bird.RemoveSelf();
		bird = null;
	}

	public override void Update()
	{
		base.Update();
		timer += Engine.DeltaTime;
		if (bird is not null)
		{
			bird.Position = Level.Camera.Position + birdScreenPosition;
			bird.Position.X += (float)Math.Sin(timer) * 4f;
			bird.Position.Y += (float)Math.Sin(timer * 0.1f) * 4f + (float)Math.Sin(timer * 0.25f) * 4f;
		}
		Level.CameraOffset = cameraOffset + cameraWaveOffset;
	}

	public override void Render()
	{
		Camera camera = Level.Camera;
		Draw.Rect(camera.X - 1f, camera.Y - 1f, 322f, 322f, Color.White * fadeToWhite);
	}
}
