// Celeste.IntroCar
using System;
using System.Collections;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.DoonvHelper.Entities;

/// <summary>
/// World's most evil entity.
/// </summary>
[CustomEntity("DoonvHelper/CustomIntroCar")]
public class CustomIntroCar : JumpThru
{
	/// <summary>
	/// Choose your suffering!
	/// </summary>
	public enum DisappearanceType
	{
		None,
		Instant,
		Fade,
		Glitch,
		Disperse,
		Ascend
	}
	private Image bodySprite;

	private Image wheels;

	private float startY;

	private bool didHaveRider;
	private PlayerCollider disappearChecker;
	public bool hasRoad;
	public bool hasBarriers;
	public DisappearanceType disappearanceType;
	public bool keepWheels;
	public Facings facing;
	private Entity wheelsEntity;
	private string persistenceFlag;

	public CustomIntroCar(
		Vector2 position,
		float disappearDistance,
		DisappearanceType disappearanceType,
		Facings facing,
		bool keepWheels = true,
		bool hasRoad = false,
		bool hasBarriers = false,
		string persistenceFlag = ""
	) : base(position, 25, safe: true)
	{
		Add(bodySprite = new Image(GFX.Game["scenery/DoonvHelper/customIntroCar/body"]));
		bodySprite.Origin = new Vector2(bodySprite.Width / 2f, bodySprite.Height);
		if (facing == Facings.Left)
			bodySprite.FlipX = true;

		this.persistenceFlag = persistenceFlag;
		this.facing = facing;
		this.keepWheels = keepWheels;
		this.disappearanceType = disappearanceType;
		this.hasRoad = hasRoad;
		this.hasBarriers = hasBarriers;
		startY = position.Y;
		base.Depth = 1;

		Hitbox hitbox = new Hitbox(25f, 4f, facing == Facings.Right ? -15f : -10f, -17f);
		Hitbox hitbox2 = new Hitbox(19f, 4f, facing == Facings.Right ? 8f : -27f, -11f);
		base.Collider = new ColliderList(hitbox, hitbox2);
		// Add a checker that checks if the player is close
		if (this.disappearanceType != DisappearanceType.None)
		{
			Add(disappearChecker = new PlayerCollider(
				PlayerTrigger,
				new Circle(disappearDistance, 0, -this.Height)
			));
		}
		SurfaceSoundIndex = 2;
	}

	public CustomIntroCar(EntityData data, Vector2 offset)
		: this(
			data.Position + offset,
			data.Float("disappearDistance", 50f),
			(DisappearanceType)Enum.Parse(
				typeof(DisappearanceType),
				data.Attr("disappearanceType", "Instant"),
				true
			),
			(Facings)Enum.Parse(
				typeof(Facings),
				data.Attr("facingDirection", "Right"),
				true
			),
			data.Bool("keepWheels", true),
			data.Bool("road", false),
			data.Bool("barriers", false),
			data.Attr("persistenceFlag", "")
		)
	{
	}

	public override void Added(Scene scene)
	{
		base.Added(scene);
		Level level = scene as Level;

		wheelsEntity = new Entity(this.Position);
		wheelsEntity.Depth = base.Depth + 1;
		wheels = new Image(GFX.Game["scenery/DoonvHelper/customIntroCar/wheels"]);
		wheels.Origin = new Vector2(wheels.Width / 2f, wheels.Height);
		wheels.FlipX = facing == Facings.Left;
		wheelsEntity.Add(wheels);
		level.Add(wheelsEntity);

		if (hasRoad)
		{
			level.Add(new IntroPavement(
				new Vector2(level.Bounds.Left, base.Y),
				(int)(base.X - (float)level.Bounds.Left - 48f)
			)
			{
				Depth = -10001
			});
		}
		if (hasBarriers)
		{
			level.Add(new IntroCarBarrier(Position + new Vector2(32f, 0f), -10, Color.White));
			level.Add(new IntroCarBarrier(Position + new Vector2(41f, 0f), 5, Color.DarkGray));
		}
	}
	public override void Awake(Scene scene)
	{
		Level level = (Scene as Level);
		if (level.Session.GetFlag(persistenceFlag))
		{
			Scene.OnEndOfFrame += () =>
			{
				Remove(disappearChecker);
				base.Collider = null;
				Visible = false;
				if (!keepWheels)
					wheels.Visible = false;
			};
		}
	}


	// This runs when the player gets close enough to the car.
	private void PlayerTrigger(Player player)
	{
		Scene.OnEndOfFrame += () =>
		{
			Remove(disappearChecker);
			Add(new Coroutine(Disappear(player, disappearanceType)));
		};
	}

	private IEnumerator Disappear(Player player, DisappearanceType disappearanceType)
	{
		Logger.Log(LogLevel.Info, "DoonvHelper", "trigger");
		(Scene as Level).Session.SetFlag(persistenceFlag);
		// Image wheelImage = wheels.Components.Get<Image>();

		switch (disappearanceType)
		{
			case DisappearanceType.Instant:
				base.Collider = null;
				Visible = false;
				if (!keepWheels)
					wheels.Visible = false;
				break;
			case DisappearanceType.Glitch:
				base.Collider = null;
				Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
				Audio.Play("event:/new_content/game/10_farewell/glitch_short");
				yield return MoonGlitchBackgroundTrigger.GlitchRoutine(0.2f, false);
				Visible = false;
				if (!keepWheels)
					wheels.Visible = false;
				break;
			case DisappearanceType.Fade:
				base.Collider = null;
				float alpha = 1.0f;
				Color baseColor = bodySprite.Color;
				Color baseWheelColor = wheels.Color;
				for (float i = 1f; i > 0.0f; i -= Engine.DeltaTime)
				{
					alpha -= Engine.DeltaTime / 1f;
					bodySprite.Color = baseColor * alpha;
					if (!keepWheels)
						wheels.Color = baseWheelColor * alpha;
					yield return null;
				}
				Visible = false;
				if (!keepWheels)
					wheels.Visible = false;
				break;
			case DisappearanceType.Disperse:
				base.Collider = null;
				Audio.Play("event:/new_content/char/granny/dissipate", Position);
				SceneAs<Level>().Add(new DisperseImage(
					Position + new Vector2(facing == Facings.Left ? -1f : 0f, 0f),
					new Vector2(this.X > player.X ? 1f : -1f, -0.1f),
					bodySprite.Origin,
					new Vector2(bodySprite.Scale.X * (facing == Facings.Left ? -1f : 1f), bodySprite.Scale.Y),
					bodySprite.Texture
				));
				if (!keepWheels)
				{
					SceneAs<Level>().Add(new DisperseImage(
						Position + new Vector2(facing == Facings.Left ? -1f : 0f, 0f),
						new Vector2(this.X > player.X ? 1f : -1f, -0.1f),
						wheels.Origin,
						new Vector2(wheels.Scale.X * (facing == Facings.Left ? -1f : 1f), wheels.Scale.Y),
						wheels.Texture
					));
				}
				yield return null;
				Visible = false;
				if (!keepWheels)
					wheels.Visible = false;
				break;
			case DisappearanceType.Ascend:
				base.Collider = null;
				while (true)
				{
					bodySprite.Position.Y += Engine.DeltaTime * -50f;
					if (!keepWheels)
					{
						wheels.Position.Y += Engine.DeltaTime * -50f;
					}
					yield return null;
				}
			default:
				Logger.Log(
					LogLevel.Error,
					"DoonvHelper",
					String.Format(
						"DisappearanceType enum `{0}` hasn't been implemented yet!",
						disappearanceType
					)
				);
				break;
		}
		yield return null;
	}


	public override void Update()
	{
		bool hasRider = HasRider();
		if (base.Y > startY && (!hasRider || base.Y > startY + 1f))
		{
			float moveV = -10f * Engine.DeltaTime;
			MoveV(moveV);
		}
		if (base.Y <= startY && !didHaveRider && hasRider)
		{
			MoveV(2f);
		}
		if (didHaveRider && !hasRider)
		{
			Audio.Play("event:/game/00_prologue/car_up", Position);
		}
		didHaveRider = hasRider;
		base.Update();
	}

	public override int GetLandSoundIndex(Entity entity)
	{
		Audio.Play("event:/game/00_prologue/car_down", Position);
		return -1;
	}
}
