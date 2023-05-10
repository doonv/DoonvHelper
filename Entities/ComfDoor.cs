using System;
using System.Collections;
using System.Collections.Generic;
using Celeste.Mod.DoonvHelper.DoonvHelperUtils;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.DoonvHelper.Entities;

/// <summary>
/// The Almighty Mighty Comf Door Of Death And Suffering
/// </summary>
[CustomEntity("DoonvHelper/ComfDoor")]
public class ComfDoor : Entity
{
	private struct Particle
	{
		public Vector2 Position;

		public float Speed;

		public Color Color;
	}

	private class WhiteLine : Entity
	{
		private float fade = 1f;

		private int blockSize;

		public WhiteLine(Vector2 origin, int blockSize)
			: base(origin)
		{
			base.Depth = -1000000;
			this.blockSize = blockSize;
		}

		public override void Update()
		{
			base.Update();
			fade = Calc.Approach(fade, 0f, Engine.DeltaTime);
			if (!(fade <= 0f)) return;
			RemoveSelf();
			Level level = SceneAs<Level>();
			for (float num = (int)level.Camera.Left; num < level.Camera.Right; num += 1f)
			{
				if (num < base.X || num >= base.X + (float)blockSize)
				{
					level.Particles.Emit(P_Slice, new Vector2(num, base.Y));
				}
			}
		}

		public override void Render()
		{
			Vector2 position = (base.Scene as Level).Camera.Position;
			float num = Math.Max(1f, 4f * fade);
			Draw.Rect(position.X - 10f, base.Y - num / 2f, 340f, num, Color.White);
		}
	}

	private const string OpenedFlag = "opened_heartgem_door_";

	public static ParticleType P_Shimmer = HeartGemDoor.P_Shimmer;

	public static ParticleType P_Slice = HeartGemDoor.P_Slice;

	public readonly int Requires;

	public int Size;

	private readonly float openDistance;

	private float openPercent;

	private Solid TopSolid;

	private Solid BotSolid;

	private float offset;

	private Vector2 mist;

	private MTexture temp = new MTexture();

	private List<MTexture> icon;

	private Particle[] particles = new Particle[50];

	private bool startHidden;

	private float heartAlpha = 1f;
	private MTexture temp2 = new MTexture();

	public int HeartGems
	{
		get
		{
			if (SaveData.Instance.CheatMode)
			{
				return Requires;
			}
			if (Scene is not null)
			{
				return DoonvHelperModule.SaveData.ComfLevelData.SafeGet(new LevelSideID(Scene)).Count;
			}
			return 0;
		}
	}

	public float Counter { get; private set; }

	public bool Opened { get; private set; }

	private float openAmount => openPercent * openDistance;
	private Vector2 comfers;

	public ComfDoor(EntityData data, Vector2 offset)
		: base(data.Position + offset)
	{
		Requires = data.Int("requires");
		Add(new CustomBloom(RenderBloom));
		Size = data.Width;
		openDistance = 32f;
		Vector2? vector = data.FirstNodeNullable(offset);
		if (vector.HasValue)
		{
			openDistance = Math.Abs(vector.Value.Y - base.Y);
		}
		icon = GFX.Game.GetAtlasSubtextures("objects/DoonvHelper/comfDoor/icon");
		startHidden = data.Bool("startHidden");
		comfers = new Vector2(0f, 0f);
	}

	public override void Added(Scene scene)
	{
		base.Added(scene);
		Level level = scene as Level;
		for (int i = 0; i < particles.Length; i++)
		{
			particles[i].Position = new Vector2(Calc.Random.NextFloat(Size), Calc.Random.NextFloat(level.Bounds.Height));
			particles[i].Speed = Calc.Random.Range(4, 12);
			particles[i].Color = Color.White * Calc.Random.Range(0.2f, 0.6f);
		}
		level.Add(TopSolid = new Solid(new Vector2(base.X, level.Bounds.Top - 32), Size, base.Y - (float)level.Bounds.Top + 32f, safe: true));
		TopSolid.SurfaceSoundIndex = 32;
		TopSolid.SquishEvenInAssistMode = true;
		TopSolid.EnableAssistModeChecks = false;
		level.Add(BotSolid = new Solid(new Vector2(base.X, base.Y), Size, (float)level.Bounds.Bottom - base.Y + 32f, safe: true));
		BotSolid.SurfaceSoundIndex = 32;
		BotSolid.SquishEvenInAssistMode = true;
		BotSolid.EnableAssistModeChecks = false;
		if ((base.Scene as Level).Session.GetFlag("opened_heartgem_door_" + Requires))
		{
			Opened = true;
			Visible = true;
			openPercent = 1f;
			Counter = Requires;
			TopSolid.Y -= openDistance;
			BotSolid.Y += openDistance;
		}
		else
		{
			Add(new Coroutine(Routine()));
		}
	}

	public override void Awake(Scene scene)
	{
		base.Awake(scene);
		if (Opened)
		{
			base.Scene.CollideFirst<DashBlock>(BotSolid.Collider.Bounds)?.RemoveSelf();
		}
		else if (startHidden)
		{
			Player entity = base.Scene.Tracker.GetEntity<Player>();
			if (entity is not null && entity.X > base.X)
			{
				startHidden = false;
				base.Scene.CollideFirst<DashBlock>(BotSolid.Collider.Bounds)?.RemoveSelf();
			}
			else
			{
				Visible = false;
			}
		}
	}

	private IEnumerator Routine()
	{
		Level level = Scene as Level;
		float botFrom2;
		float topFrom2;
		float botTo2;
		float topTo2;
		if (startHidden)
		{
			Player entity;
			do
			{
				yield return null;
				entity = Scene.Tracker.GetEntity<Player>();
			}
			while (entity is null || !(Math.Abs(entity.X - Center.X) < 100f));
			Audio.Play("event:/new_content/game/10_farewell/heart_door", Position);
			Visible = true;
			heartAlpha = 0f;
			topTo2 = TopSolid.Y;
			botTo2 = BotSolid.Y;
			topFrom2 = (TopSolid.Y -= 240f);
			botFrom2 = (BotSolid.Y -= 240f);
			for (float p2 = 0f; p2 < 1f; p2 += Engine.DeltaTime * 1.2f)
			{
				float num = Ease.CubeIn(p2);
				TopSolid.MoveToY(topFrom2 + (topTo2 - topFrom2) * num);
				BotSolid.MoveToY(botFrom2 + (botTo2 - botFrom2) * num);
				DashBlock dashBlock = Scene.CollideFirst<DashBlock>(BotSolid.Collider.Bounds);
				if (dashBlock is not null)
				{
					level.Shake(0.5f);
					global::Celeste.Celeste.Freeze(0.1f);
					Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
					dashBlock.Break(BotSolid.BottomCenter, new Vector2(0f, 1f), playSound: true, playDebrisSound: false);
					Player entity2 = Scene.Tracker.GetEntity<Player>();
					if (entity2 is not null && Math.Abs(entity2.X - Center.X) < 40f)
					{
						entity2.PointBounce(entity2.Position + Vector2.UnitX * 8f);
					}
				}
				yield return null;
			}
			level.Shake(0.5f);
			global::Celeste.Celeste.Freeze(0.1f);
			TopSolid.Y = topTo2;
			BotSolid.Y = botTo2;
			while (heartAlpha < 1f)
			{
				heartAlpha = Calc.Approach(heartAlpha, 1f, Engine.DeltaTime * 2f);
				yield return null;
			}
			yield return 0.6f;
		}
		while (!Opened && Counter < (float)Requires)
		{
			Player entity3 = Scene.Tracker.GetEntity<Player>();
			if (entity3 is not null && Math.Abs(entity3.X - Center.X) < 80f && entity3.X < X)
			{
				if (Counter == 0f && HeartGems > 0)
				{
					Audio.Play("event:/game/09_core/frontdoor_heartfill", Position);
				}
				if (HeartGems < Requires)
				{
					level.Session.SetFlag("granny_door");
				}
				int num2 = (int)Counter;
				int target = Math.Min(HeartGems, Requires);
				Counter = Calc.Approach(Counter, target, Engine.DeltaTime * (float)Requires * 0.2f);
				if (num2 != (int)Counter)
				{
					yield return 0.1f;
					if (Counter < (float)target)
					{
						Audio.Play("event:/game/09_core/frontdoor_heartfill", Position);
					}
				}
			}
			else
			{
				Counter = Calc.Approach(Counter, 0f, Engine.DeltaTime * (float)Requires * 4f);
			}
			yield return null;
		}
		yield return 0.5f;
		Scene.Add(new WhiteLine(Position, Size));
		level.Shake();
		Input.Rumble(RumbleStrength.Strong, RumbleLength.Long);
		level.Flash(Color.White);
		Audio.Play("event:/game/09_core/frontdoor_unlock", Position);
		Opened = true;
		level.Session.SetFlag("opened_heartgem_door_" + Requires);
		offset = 0f;
		yield return 0.6f;
		botFrom2 = TopSolid.Y;
		topFrom2 = TopSolid.Y - openDistance;
		botTo2 = BotSolid.Y;
		topTo2 = BotSolid.Y + openDistance;
		for (float p2 = 0f; p2 < 1f; p2 += Engine.DeltaTime)
		{
			level.Shake();
			openPercent = Ease.CubeIn(p2);
			TopSolid.MoveToY(MathHelper.Lerp(botFrom2, topFrom2, openPercent));
			BotSolid.MoveToY(MathHelper.Lerp(botTo2, topTo2, openPercent));
			if (p2 >= 0.4f && level.OnInterval(0.1f))
			{
				for (int i = 4; i < Size; i += 4)
				{
					level.ParticlesBG.Emit(P_Shimmer, 1, new Vector2(TopSolid.Left + (float)i + 1f, TopSolid.Bottom - 2f), new Vector2(2f, 2f), -(float)Math.PI / 2f);
					level.ParticlesBG.Emit(P_Shimmer, 1, new Vector2(BotSolid.Left + (float)i + 1f, BotSolid.Top + 2f), new Vector2(2f, 2f), (float)Math.PI / 2f);
				}
			}
			yield return null;
		}
		TopSolid.MoveToY(topFrom2);
		BotSolid.MoveToY(topTo2);
		openPercent = 1f;
	}

	public override void Update()
	{
		base.Update();
		if (!Opened)
		{
			offset += 12f * Engine.DeltaTime;
			mist.X -= 4f * Engine.DeltaTime;
			mist.Y -= 24f * Engine.DeltaTime;
			comfers.X += 8f * Engine.DeltaTime;
			comfers.Y += 8f * Engine.DeltaTime;
			for (int i = 0; i < particles.Length; i++)
			{
				particles[i].Position.Y += particles[i].Speed * Engine.DeltaTime;
			}
		}
	}

	public void RenderBloom()
	{
		if (!Opened && Visible)
		{
			DrawBloom(new Rectangle((int)TopSolid.X, (int)TopSolid.Y, Size, (int)(TopSolid.Height + BotSolid.Height)));
		}
	}

	private void DrawBloom(Rectangle bounds)
	{
		Draw.Rect(bounds.Left - 4, bounds.Top, 2f, bounds.Height, Color.White * 0.25f);
		Draw.Rect(bounds.Left - 2, bounds.Top, 2f, bounds.Height, Color.White * 0.5f);
		Draw.Rect(bounds, Color.White * 0.75f);
		Draw.Rect(bounds.Right, bounds.Top, 2f, bounds.Height, Color.White * 0.5f);
		Draw.Rect(bounds.Right + 2, bounds.Top, 2f, bounds.Height, Color.White * 0.25f);
	}

	private void DrawMist(Rectangle bounds, Vector2 mist)
	{
		Color color = Color.White * 0.6f;
		MTexture mTexture = GFX.Game["objects/DoonvHelper/comfDoor/mist"];
		int num = mTexture.Width / 2;
		int num2 = mTexture.Height / 2;
		for (int i = 0; i < bounds.Width; i += num)
		{
			for (int j = 0; j < bounds.Height; j += num2)
			{
				mTexture.GetSubtexture((int)Mod(mist.X, num), (int)Mod(mist.Y, num2), Math.Min(num, bounds.Width - i), Math.Min(num2, bounds.Height - j), temp);
				temp.Draw(new Vector2(bounds.X + i, bounds.Y + j), Vector2.Zero, color);
			}
		}
	}

	private void DrawInterior(Rectangle bounds)
	{
		Draw.Rect(bounds, Calc.HexToColor("4b3060"));
		DrawTheComfobomination(bounds);
		DrawMist(bounds, mist);
		DrawMist(bounds, new Vector2(mist.Y, mist.X) * 1.5f);
		Vector2 vector = (base.Scene as Level).Camera.Position;
		if (Opened)
		{
			vector = Vector2.Zero;
		}
		for (int i = 0; i < particles.Length; i++)
		{
			Vector2 vector2 = particles[i].Position + vector * 0.2f;
			vector2.X = Mod(vector2.X, bounds.Width);
			vector2.Y = Mod(vector2.Y, bounds.Height);
			Draw.Pixel.Draw(new Vector2(bounds.X, bounds.Y) + vector2, Vector2.Zero, particles[i].Color);
		}
	}

	private void DrawEdges(Rectangle bounds, Color color)
	{
		MTexture mTexture = GFX.Game["objects/heartdoor/edge"];
		MTexture mTexture2 = GFX.Game["objects/heartdoor/top"];
		int num = (int)(offset % 8f);
		if (num > 0)
		{
			mTexture.GetSubtexture(0, 8 - num, 7, num, temp);
			temp.DrawJustified(new Vector2(bounds.Left + 4, bounds.Top), new Vector2(0.5f, 0f), color, new Vector2(-1f, 1f));
			temp.DrawJustified(new Vector2(bounds.Right - 4, bounds.Top), new Vector2(0.5f, 0f), color, new Vector2(1f, 1f));
		}
		for (int i = num; i < bounds.Height; i += 8)
		{
			mTexture.GetSubtexture(0, 0, 8, Math.Min(8, bounds.Height - i), temp);
			temp.DrawJustified(new Vector2(bounds.Left + 4, bounds.Top + i), new Vector2(0.5f, 0f), color, new Vector2(-1f, 1f));
			temp.DrawJustified(new Vector2(bounds.Right - 4, bounds.Top + i), new Vector2(0.5f, 0f), color, new Vector2(1f, 1f));
		}
		for (int j = 0; j < bounds.Width; j += 8)
		{
			mTexture2.DrawCentered(new Vector2(bounds.Left + 4 + j, bounds.Top + 4), color);
			mTexture2.DrawCentered(new Vector2(bounds.Left + 4 + j, bounds.Bottom - 4), color, new Vector2(1f, -1f));
		}
	}
	private void DrawTheComfobomination(Rectangle bounds)
	{
		MTexture mTexture = GFX.Game["objects/DoonvHelper/comfDoor/omegacomf"];
		int num = mTexture.Width / 2;
		int num2 = mTexture.Height / 2;
		for (int i = 0; i < bounds.Width; i += num)
		{
			for (int j = 0; j < bounds.Height; j += num2)
			{
				mTexture.GetSubtexture((int)Mod(comfers.X, num), (int)Mod(comfers.Y, num2), Math.Min(num, bounds.Width - i), Math.Min(num2, bounds.Height - j), temp2);

				temp2.Draw(new Vector2(bounds.X + i, bounds.Y + j), Vector2.Zero, Color.White);
			}
		}
	}

	public override void Render()
	{
		Color color = (Opened ? (Color.White * 0.25f) : Color.White);
		if (!Opened && TopSolid.Visible && BotSolid.Visible)
		{
			Rectangle bounds = new Rectangle((int)TopSolid.X, (int)TopSolid.Y, Size, (int)(TopSolid.Height + BotSolid.Height));
			DrawInterior(bounds);
			DrawEdges(bounds, color);
		}
		else
		{
			if (TopSolid.Visible)
			{
				Rectangle bounds2 = new Rectangle((int)TopSolid.X, (int)TopSolid.Y, Size, (int)TopSolid.Height);
				DrawInterior(bounds2);
				DrawEdges(bounds2, color);
			}
			if (BotSolid.Visible)
			{
				Rectangle bounds3 = new Rectangle((int)BotSolid.X, (int)BotSolid.Y, Size, (int)BotSolid.Height);
				DrawInterior(bounds3);
				DrawEdges(bounds3, color);
			}
		}
		if (!(heartAlpha > 0f))
		{
			return;
		}
		float num = 12f;
		int num2 = (int)((float)(Size - 8) / num);
		int num3 = (int)Math.Ceiling((float)Requires / (float)num2);
		Color color2 = color * heartAlpha;
		for (int i = 0; i < num3; i++)
		{
			int num4 = (((i + 1) * num2 < Requires) ? num2 : (Requires - i * num2));
			Vector2 vector = new Vector2(
				base.X + (float)Size * 0.5f, base.Y
			) + new Vector2(
				(float)(-num4) / 2f + 0.5f, (float)(-num3) / 2f + (float)i + 0.5f
			) * num;
			if (Opened)
			{
				if (i < num3 / 2)
				{
					vector.Y -= openAmount + 8f;
				}
				else
				{
					vector.Y += openAmount + 8f;
				}
			}
			for (int j = 0; j < num4; j++)
			{
				int num5 = i * num2 + j;
				float num6 = Ease.CubeIn(Calc.ClampedMap(Counter, num5, (float)num5 + 1f));
				icon[(int)(num6 * (float)(icon.Count - 1))].DrawCentered(vector + new Vector2((float)j * num, (i - (num3 / 2f)) * 2f), color2);
			}
		}
	}

	private float Mod(float x, float m)
	{
		return (x % m + m) % m;
	}
}
