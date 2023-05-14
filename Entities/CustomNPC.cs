using Celeste.Mod.DoonvHelper.Utils;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System;
using System.Collections.Generic;
using Celeste.Mod.XaphanHelper;

namespace Celeste.Mod.DoonvHelper.Entities;

/// <summary>
/// It's like <see cref="Mod.Entities.CustomNPC"/> but customer! (Wait that doesn't sound right)
/// </summary>
[CustomEntity("DoonvHelper/CustomNPC")]
[Tracked(inherited: true)]
public class CustomNPC : Actor
{
	public class GoreParticle : Actor
	{
		public Vector2 Velocity;
		public MTexture Texture;
		public float Rotation;
		public float Lifetime;
		public float Lifespan;
		public GoreParticle(MTexture texture, Vector2 position, Vector2 velocity, float lifespan) : base(position)
		{
			this.Depth = 2;
			this.Collider = new Hitbox(8f, 8f, -4f, -4f);
			this.Texture = texture;
			this.Velocity = velocity;
			this.Rotation = Calc.Random.NextAngle();
			this.Lifetime = lifespan;
			this.Lifespan = lifespan;
		}

		public override void Update()
		{
			base.Update();
			if (Settings.Instance.DisableFlashes || Lifetime < 0f)
			{
				base.Scene.OnEndOfFrame += () => this.RemoveSelf();
				return;
			}
			Lifetime -= Engine.DeltaTime;
			if (this.OnGround())
			{
				Velocity.X = Calc.Approach(Velocity.X, 0f, 240f * Engine.DeltaTime);
			}
			else
			{
				Velocity.Y = Calc.Approach(Velocity.Y, 240f, 480f * Engine.DeltaTime);
				Velocity.X = Calc.Approach(Velocity.X, 0f, 50f * Engine.DeltaTime);
			}
			MoveH(Velocity.X * Engine.DeltaTime);
			MoveV(Velocity.Y * Engine.DeltaTime);
		}

		public override void Render()
		{
			base.Render();
			Texture.DrawCentered(Position, Color.White * Calc.Clamp(Lifetime / (Lifespan * 0.1f), 0f, 1f), 1f, Rotation + (float)Math.Atan2(Velocity.Y, Velocity.X));
		}
	}
	/// <summary>Enum that stores the possible states for the <see cref="StateMachine"/>.</summary>
	public enum St
	{
		Dummy = 0,
		Idle = 1,
		Walking = 2,
		Flying = 3
	}
	/// <summary>The type of AI being used by the NPC</summary>
	public enum AIType
	{
		/// <summary>
		///     Similar to <see cref="AIType.Fly"/>, but only chases the player while the player is in <see cref="Water"/>.
		///     The NPC must start in <see cref="Water"/> or this won't work.
		/// </summary>
		Swim,
		/// <summary>The NPC flies directly toward the player.</summary>
		Fly,
		/// <summary>
		///     The NPC flies directly toward the player,
		///     but cant go further than the distance the starting position has from their node.
		/// </summary>
		FlyTied,
		/// <summary>The NPC flies toward the player using pathfinding.</summary>
		SmartFly,
		/// <summary>The NPC walks between it's nodes.</summary>
		NodeWalk,
		/// <summary>The NPC walks toward the player.</summary>
		ChaseWalk,
		/// <summary>The NPC randomly walks and stands around.</summary>
		Wander,
		/// <summary>
		///     The NPC has <see cref="AIType.Fly"/> AI when behind a bg tile, 
		///     and has <see cref="AIType.ChaseWalk"/> AI when not behind a bg tile.
		/// </summary>
		WalkNClimb,
		/// <summary>
		///     Similar to <see cref="AIType.ChaseWalk"/>, but can only move in the air and not on the ground. 
		///     (Similar to Terraria's slime AI)
		/// </summary>
		ChaseJump,
	}
	/// <summary>An enum facing behaviors.</summary>
	public enum FacingAt
	{
		None,
		MovementFlip,
		MovementRotate,
		MovementRotateFlip,
	}
	/// <summary>The positions of each of the nodes.</summary>
	public readonly Vector2[] Nodes;
	/// <summary>The pixels/sec speed of the NPC.</summary>
	public Vector2 Velocity = Vector2.Zero;
	protected Sprite Sprite;

	public AIType AI;
	/// <summary>The maximum speed of the NPC in pixels/sec</summary>
	public Vector2 Speed;
	/// <summary>The pixels/second increase of <see cref="Velocity"/>. </summary>
	public float Acceleration;
	public float JumpHeight;
	public StateMachine StateMachine;
	public FacingAt Facing;
	public bool WaitForMovement;
	public bool OutlineEnabled;
	public bool EnforceLevelBounds;

	public Hitbox JumpCheckCollider => new Hitbox(
		this.Collider.Width + 20f,
		this.Collider.Height,
		this.Collider.Position.X + (this.Velocity.X > 0f ? 0f : -20f),
		this.Collider.Position.Y
	);

	/// <summary>Used for <see cref="AIType.Swim" />.</summary>
	private Entity swimAILiquid;
	/// <summary>Used for <see cref="AIType.FlyTied" />.</summary>
	private readonly float flyTiedAICircleRadius;
	/// <summary>Used for <see cref="AIType.Wander" /> and <see cref="AIType.ChaseJump" />.</summary>
	private float nextMoveTimer = 0f;
	/// <summary>Used for <see cref="AIType.Wander" />.</summary>
	private float wanderAIdirection = 0f;
	private int nodeIndex = 0;
	private List<Vector2> path = new List<Vector2>();
	private Player player;
	private Level level;
	private Texture2D flyTiedAIconnectorTexture;

	/// <summary>
	/// Constructor used to create NPCs using code
	/// </summary>
	public CustomNPC(
		Vector2[] nodes, Hitbox hitbox,
		Vector2 speed, float acceleration,
		AIType ai, string spriteID,
		float jumpHeight,
		FacingAt facing, bool waitForMovement = true,
		bool outlineEnabled = true, bool enforceLevelBounds = true
	) : base(nodes[0])
	{
		this.Depth = 1;

		hitbox.Position.X -= hitbox.Width / 2f;
		hitbox.Position.Y -= hitbox.Height;

		this.Nodes = nodes;
		base.Collider = hitbox;
		this.AI = ai;
		this.Speed = speed;
		this.Acceleration = acceleration;
		this.Facing = facing;
		this.WaitForMovement = waitForMovement;
		this.JumpHeight = jumpHeight;
		this.OutlineEnabled = outlineEnabled;
		this.EnforceLevelBounds = enforceLevelBounds;

		if (this.Nodes.Length > 1)
			this.flyTiedAICircleRadius = Vector2.Distance(this.Nodes[0], this.Nodes[1]);

		Add(Sprite = GFX.SpriteBank.Create(spriteID));
		if (Sprite.Has("connector"))
			this.flyTiedAIconnectorTexture = Sprite.Animations["connector"].Frames[0].Texture.Texture_Safe;

		this.StateMachine = new StateMachine(10);
		this.StateMachine.SetCallbacks((int)St.Dummy, null, null, () => Sprite.PlaySafe("idle"), null);
		this.StateMachine.SetCallbacks((int)St.Idle, null, null, () => Sprite.PlaySafe("idle"), null);
		this.StateMachine.SetCallbacks((int)St.Walking, stMovingUpdate, null, () => Sprite.PlaySafe("walking", "moving"), null);
		this.StateMachine.SetCallbacks((int)St.Flying, stMovingUpdate, null, () => Sprite.PlaySafe("flying", "moving"), null);

		Add(this.StateMachine);
	}

	/// <summary>
	/// Constructor used by the level to create NPCs
	/// </summary>
	public CustomNPC(EntityData data, Vector2 offset) : this(
		data.NodesWithPosition(offset),
		new Hitbox(
			width: data.Float("hitboxWidth", 16f),
			height: data.Float("hitboxHeight", 16f),
			x: data.Float("hitboxXOffset", 0f),
			y: data.Float("hitboxYOffset", 0f)
		),
		new Vector2(data.Float("XSpeed", 48f), data.Float("YSpeed", 240f)),
		data.Float("acceleration", 6f),
		data.Enum<AIType>("aiType", AIType.Wander),
		data.Attr("spriteID", "DoonvHelper_CustomEnemy_zombie"),
		data.Float("jumpHeight", 50f),
		data.Enum<FacingAt>("facing", FacingAt.MovementFlip),
		data.Bool("waitForMovement", true),
		data.Bool("outlineEnabled", true),
		data.Bool("enforceLevelBounds", true)
	)
	{
	}

	/// <summary>
	///     Used for path finding, after getting a path with <see cref="Level.Pathfinder.Find"/> and
	///     passing it to this function, this function will return the direction
	///     to the next node (determined by <see cref="nodeIndex"/>).
	/// </summary>
	/// <param name="path">An array of <see cref="Vector2"/> nodes.</param>
	/// <param name="loop">Whever to start traversing the path in reverse after reaching the path's end.</param>
	/// <returns>The direction to the next node.</returns>
	protected Vector2 GetPathfindingNodeDirection(Vector2[] path, bool loop = false)
	{
		if (nodeIndex >= path.Length)
		{
			if (loop)
				nodeIndex = (-nodeIndex) + 1;
			else
				return Vector2.Zero;
		}
		if (Vector2.DistanceSquared(base.Position, path[Math.Abs(nodeIndex)]) < 36f)
		{
			nodeIndex++;
			return GetPathfindingNodeDirection(path);
		}
		return (path[Math.Abs(nodeIndex)] - base.Position).SafeNormalize();
	}

	/// <summary>Returns <c>true</c> if the NPC can "see" the player.</summary>
	/// <param name="player">The player</param>
	public bool CanSeePlayer(Player player)
	{
		if (player is null) return false;
		if (!SceneAs<Level>().InsideCamera(base.Center) && Vector2.DistanceSquared(base.Center, player.Center) > 25600f)
		{
			return false;
		}
		Vector2 vector = (player.Center - base.Center).Perpendicular().SafeNormalize(2f);
		if (!base.Scene.CollideCheck<Solid>(base.Center + vector, player.Center + vector))
		{
			return !base.Scene.CollideCheck<Solid>(base.Center - vector, player.Center - vector);
		}
		return false;
	}

	public override void Added(Scene scene)
	{
		base.Added(scene);
		StateMachine.State = (int)St.Idle;
		level = (scene as Level);
	}
	public override void Awake(Scene scene)
	{
		base.Awake(scene);
		player = scene.Tracker.GetEntity<Player>();
	}

	public override void DebugRender(Camera camera)
	{
		if (player is null)
		{
			base.DebugRender(camera);
			return;
		}
		if (AI == AIType.NodeWalk) Draw.HollowRect(JumpCheckCollider.AddPosition(this.Position), Color.Yellow);
		base.DebugRender(camera);
		if (AI == AIType.NodeWalk) Draw.HollowRect(JumpCheckCollider.AddPosition(this.Position), Color.Yellow * 0.5f);
		if (path is not null && path.Count > 1)
		{
			for (int i = 0; i < (path.Count - 1); i++)
			{
				Draw.Line(path[i], path[i + 1], Color.Red);
				Draw.Rect(path[i] + new Vector2(-2f, -2f), 3f, 3f, Color.Red);
			}
		}
		else if (AI == AIType.Fly)
		{
			Draw.Line(this.Center, player.Center, Color.Red);
		}
		else if (AI == AIType.FlyTied)
		{
			Draw.Line(this.Center, player.Center, Color.Red);
			if (this.Nodes.Length > 1) Draw.Circle(this.Nodes[0], flyTiedAICircleRadius, Color.Green, 8);
		}
	}

	/// <summary>Kills (destroys) the NPC.</summary>
	public virtual void Kill()
	{
		if (Sprite.Has("gore") && Settings.Instance.DisableFlashes == false)
		{
			Sprite.Animation gores = Sprite.Animations["gore"];
			for (int i = 0; i < gores.Frames.Length; i++)
			{
				level.Add(new GoreParticle(
					gores.Frames[i],
					this.Center,
					Calc.AngleToVector(Calc.Random.Range((float)Math.PI * 1.75f, (float)Math.PI * 1.25f), 200f),
					8f
				));
			}
		}
		else
		{
			MTexture frame = Sprite.GetFrame(Sprite.CurrentAnimationID, Sprite.CurrentAnimationFrame);
			level.Add(new DisperseImage(
				this.Position,
				new Vector2(0f, 1f),
				Sprite.Origin,
				new Vector2(Sprite.Scale.X * (Sprite.FlipX ? -1f : 1f), Sprite.Scale.Y),
				frame
			));
		}

		Scene.OnEndOfFrame += () => this.RemoveSelf();
	}

	public static void RotateSpriteToFacing(Sprite sprite, Vector2 velocity, FacingAt facingAt)
	{
		if (velocity.LengthSquared() < 0.1f) return;

		switch (facingAt)
		{
			// This line isn't needed because the None option doesn't do anything
			// case FacingAt.None: break;
			case FacingAt.MovementRotateFlip:
				sprite.FlipY = Math.Cos(sprite.Rotation) < 0f;
				goto case FacingAt.MovementRotate;
			case FacingAt.MovementRotate:
				sprite.Rotation = velocity.Angle() - (float)Math.PI;
				break;
			case FacingAt.MovementFlip:
				sprite.FlipX = velocity.X > 0f;
				break;
		}
	}

	private int stMovingUpdate()
	{
		RotateSpriteToFacing(Sprite, Velocity, Facing);
		// At the end of the frame our state changes to this function's return value.
		return StateMachine.State;
	}

	public override void Update()
	{
		base.Update();

		if (player is null || StateMachine.State == (int)St.Dummy || (WaitForMovement && player.JustRespawned)) return;

		int newState = AIUpdate(); // Update the enemy based on its AI and then store the new state.
		if (Velocity.LengthSquared() > 0.1f)
			StateMachine.State = newState;
		else
			StateMachine.State = (int)St.Idle;


		// Open doors
		foreach (Door door in Scene.Tracker.GetEntities<Door>())
			if (this.CollideCheck(door)) door.Open(this.X);

		// Move the NPC and then enforce the level bounds.
		MoveH(Velocity.X * Engine.DeltaTime);
		MoveV(Velocity.Y * Engine.DeltaTime);

		if (EnforceLevelBounds)
		{
			if (this.Left < (float)level.Bounds.Left)
			{
				this.Left = level.Bounds.Left;
				this.Velocity.X = 0f;
			}
			if (this.Right > (float)level.Bounds.Right)
			{
				this.Right = level.Bounds.Right;
				this.Velocity.X = 0f;
			}
			if (this.Top < (float)(level.Bounds.Top - 24))
			{
				this.Top = level.Bounds.Top - 24;
				this.Velocity.Y = 0f;
			}
			if (this.Top > (float)(level.Bounds.Bottom + 4))
			{
				this.Kill();
			}
		}
	}


	public override void Render()
	{
		if (OutlineEnabled) Sprite.DrawOutline();
		if (AI == AIType.FlyTied && flyTiedAIconnectorTexture is not null)
		{
			Draw.SpriteBatch.Draw(
				flyTiedAIconnectorTexture,
				this.Nodes[0],
				new Rectangle(0, 0, (int)Vector2.Distance(this.Nodes[0], this.Position), flyTiedAIconnectorTexture.Height),
				Color.White,
				Calc.Angle(this.Nodes[0], this.Position),
				new Vector2(0f, flyTiedAIconnectorTexture.Height / 2f),
				1f,
				SpriteEffects.None,
				0f
			);
		}
		base.Render();
	}

	/// <summary>
	/// An overridable method used for the NPC's AI.
	/// Use the <see cref="Velocity"/> field to move the NPC. (The base method provides great examples of what you can do)
	/// </summary>
	public virtual int AIUpdate()
	{
		void WalkerFall()
		{
			if (!this.OnGround()) Velocity.Y = Calc.Approach(Velocity.Y, Speed.Y, 2f * Speed.Y * Engine.DeltaTime);
			else if (Velocity.Y > 0f) Velocity.Y = 0f;
		}
		/// <summary>Also known as: Shall the character ascend into the heavens beyond our mortal plane?</summary>
		bool WalkerJumpCheck()
		{
			Collider oldCollider = this.Collider;
			this.Collider = this.JumpCheckCollider;
			bool colliding = this.CollideCheck<Solid>();
			this.Collider = oldCollider;
			return colliding && this.OnGround();
		}

		switch (AI)
		{
			case AIType.Swim:
				if (swimAILiquid is null)
				{
					swimAILiquid = CollideFirst<Water>();
					if (swimAILiquid is null && DoonvHelperModule.XaphanLiquid is not null)
					{
						swimAILiquid = Collide.First(this, Scene.Tracker.Entities[DoonvHelperModule.XaphanLiquid]);
					}
				}
				else
				{
					if (this.CollideRect(new Rectangle(
						(int)swimAILiquid.Collider.AbsoluteX,
						(int)swimAILiquid.Collider.AbsoluteY + ((int)this.Collider.Height / 2),
						(int)swimAILiquid.Collider.Width,
						(int)swimAILiquid.Collider.Height - ((int)this.Collider.Height / 2)
					)))
					{
						if (player.CollideCheck(swimAILiquid))
						{
							StateMachine.State = (int)St.Flying;
							Velocity = Calc.Approach(Velocity, (player.Position - this.Position).SafeNormalize() * Speed, Acceleration * Engine.DeltaTime);
						}
						else
						{
							StateMachine.State = (int)St.Idle;
							Velocity = Calc.Approach(Velocity, Vector2.Zero, Acceleration * 0.25f * Engine.DeltaTime);
						}
					}
					else
					{
						StateMachine.State = (int)St.Walking;
						Velocity.X = Calc.Approach(Velocity.X, 0f, Acceleration * Engine.DeltaTime);
						WalkerFall();
					}
				}
				return StateMachine.State;
			case AIType.WalkNClimb:
				char bgTileAtPos = level.BgData[
					(int)(this.Center.X / 8) - level.Session.MapData.TileBounds.Left,
					(int)(this.Center.Y / 8) - level.Session.MapData.TileBounds.Top
				];

				if (bgTileAtPos == '0')
				{
					goto case AIType.ChaseWalk;
				}
				else
				{
					Velocity = Calc.Approach(Velocity, (player.Position - this.Position).SafeNormalize() * Speed.X, Acceleration * Engine.DeltaTime);
					return (int)St.Flying;
				}
			case AIType.Fly:
				Velocity = Calc.Approach(Velocity, (player.Position - this.Position).SafeNormalize() * Speed, Acceleration * Engine.DeltaTime);
				return (int)St.Flying;
			case AIType.FlyTied:
				if (this.Nodes.Length <= 1) goto case AIType.Fly;
				Vector2 targetpos = (player.Position - this.Nodes[0]);
				if (targetpos.Length() > flyTiedAICircleRadius)
					targetpos = targetpos.SafeNormalize(flyTiedAICircleRadius);
				Velocity = Calc.Approach(Velocity, ((targetpos + this.Nodes[0]) - this.Position).SafeNormalize() * Speed, Acceleration * Engine.DeltaTime);

				// if (
				// 	(
				// 		Math.Pow((this.X + Velocity.X) - this.Nodes[0].X, 2) +
				// 		Math.Pow((this.Y + Velocity.Y) - this.Nodes[0].Y, 2)
				// 	) > Vector2.DistanceSquared(this.Nodes[0], this.Nodes[1])
				// )
				// {
				// 	Velocity = Calc.Approach(Velocity, Vector2.Zero, Acceleration * 4 * Engine.DeltaTime);
				// }
				return (int)St.Flying;
			case AIType.SmartFly:
				if (Scene.OnInterval(0.2f) && CanSeePlayer(player))
				{
					nodeIndex = 0;
					(Scene as Level).Pathfinder.Find(ref path, base.Center, player.Center, false, logging: true);
				}
				Velocity = Calc.Approach(Velocity, (GetPathfindingNodeDirection(path.ToArray()) * Speed), Acceleration * Engine.DeltaTime);
				return (int)St.Flying;
			case AIType.NodeWalk:
				if (WalkerJumpCheck()) Velocity.Y = -JumpHeight;

				WalkerFall();
				Velocity.X = Calc.Approach(Velocity.X, GetPathfindingNodeDirection(Nodes, true).Sign().X * Speed.X, Acceleration * Engine.DeltaTime);
				return (int)St.Walking;
			case AIType.ChaseWalk:
				if (WalkerJumpCheck()) Velocity.Y = -JumpHeight;

				WalkerFall();
				Velocity.X = Calc.Approach(Velocity.X, (player.Position - this.Position).Sign().X * Speed.X, Acceleration * Engine.DeltaTime);
				return (int)St.Walking;
			case AIType.ChaseJump:
				nextMoveTimer -= Engine.DeltaTime;
				if (nextMoveTimer < 0f)
				{
					if (this.OnGround()) Velocity.Y = -JumpHeight;
					nextMoveTimer = Calc.Random.NextFloat(2f - 0.5f) + 0.5f;
				}

				WalkerFall();
				if (this.OnGround()) Velocity.X = Calc.Approach(Velocity.X, 0, Acceleration * Engine.DeltaTime);
				else Velocity.X = Calc.Approach(Velocity.X, (player.Position - this.Position).Sign().X * Speed.X, Acceleration * Engine.DeltaTime);
				return (int)St.Walking;
			case AIType.Wander:
				// The move timer gets reduced by 1.5 each second when moving.
				// The move timer gets reduced by 1.0 each second when not moving.
				nextMoveTimer -= Math.Abs(Velocity.X) < 0.1f ? Engine.DeltaTime : Engine.DeltaTime * 1.5f;
				if (nextMoveTimer < 0f)
				{
					nextMoveTimer = Calc.Random.NextFloat(5f - 2f) + 2f;
					wanderAIdirection = Calc.Random.Choose<float>(0f, 1f, -1f);
				}
				WalkerFall();
				if (WalkerJumpCheck()) Velocity.Y = -JumpHeight;
				Velocity.X = Calc.Approach(Velocity.X, wanderAIdirection * Speed.X, Acceleration * Engine.DeltaTime);
				return (int)St.Walking;
		}
		return StateMachine.State;
	}
}
