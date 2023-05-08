using Celeste.Mod.DoonvHelper.Utils;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;

namespace Celeste.Mod.DoonvHelper.Entities {
    /// <summary>
    /// It's like an NPC, but custom!
    /// </summary>
    [CustomEntity("DoonvHelper/CustomNPC")]
    [Tracked(inherited: true)]
    public class CustomNPC : Actor {
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
                if (Settings.Instance.DisableFlashes || Lifetime < 0f) {
                    base.Scene.OnEndOfFrame += () => this.RemoveSelf();
                    return;
                }
                Lifetime -= Engine.DeltaTime;
                if (this.OnGround()) {
                    Velocity.X = Calc.Approach(Velocity.X, 0f, 240f * Engine.DeltaTime);
                } else {
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
        public enum St {
            Dummy   = 0,
            Idle    = 1,
            Walking = 2,
            Flying  = 3
        }
        /// <summary>The type of AI being used by the NPC</summary>
        public enum AIType {
            /// <summary>
            ///     Similar to <see cref="AIType.Fly"/>, but only chases the player while the player is in <see cref="Water"/>.
            ///     The NPC must start in <see cref="Water"/> or this won't work.
            /// </summary>
            Swim,
            /// <summary>The NPC flies directly toward the player.</summary>
            Fly,
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
        /// <summary>Makes the <see cref="Sprite"/> rotate towards where its heading instead of just flipping</summary>
        public bool FaceMovement;
        public bool WaitForMovement;
        public bool OutlineEnabled;

        public Hitbox JumpCheckCollider => new Hitbox(
            this.Collider.Width + 20f,
            this.Collider.Height,
            this.Collider.Position.X + (this.Velocity.X > 0f ? 0f : -20f),
            this.Collider.Position.Y
        );

        /// <summary>Used for <see cref="AIType.Swim" />.</summary>
        private Water swimAIwater;
        /// <summary>Used for <see cref="AIType.Wander" />.</summary>
        private float wanderAINextMoveTimer = 0f; 
        private int nodeIndex = 0;
        private List<Vector2> path = new List<Vector2>();
        private Player player;
        private Level level;

        /// <summary>
        /// Constructor used to create NPCs using code
        /// </summary>
        public CustomNPC(
            Vector2[] nodes, Hitbox hitbox,
            Vector2 speed, float acceleration,
            AIType ai, string spriteID,
            float jumpHeight,
            bool faceMovement = false, bool waitForMovement = true,
            bool outlineEnabled = true
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
            this.FaceMovement = faceMovement;
            this.WaitForMovement = waitForMovement;
            this.JumpHeight = jumpHeight;
            this.OutlineEnabled = outlineEnabled;

            Add(Sprite = GFX.SpriteBank.Create(spriteID));

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
        public CustomNPC(EntityData data, Vector2 offset)
            : this(
                data.NodesWithPosition(offset),
                new Hitbox(
                    width: data.Float("hitboxWidth", 16f),
                    height: data.Float("hitboxHeight", 16f),
                    x: data.Float("hitboxXOffset", 0f),
                    y: data.Float("hitboxYOffset", 0f)
                ),
                new Vector2(data.Float("XSpeed", 48f), data.Float("YSpeed", 240f)),
                data.Float("acceleration", 6f),
                Calc.StringToEnum<AIType>(data.Attr("aiType", "Wander").Replace(" ", "").Replace('&', 'N')),
                data.Attr("spriteID", "DoonvHelper_CustomEnemy_zombie"),
                data.Float("jumpHeight", 50f),
                data.Bool("faceMovement", false),
                data.Bool("waitForMovement", true),
                data.Bool("outlineEnabled", true)
            ) 
        {
        }

        private Vector2 GetNodeDirection(Vector2[] path, bool loop = false)
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
                return GetNodeDirection(path);
            }
            return (path[Math.Abs(nodeIndex)] - base.Position).SafeNormalize();
        }

        /// <summary>Returns true if the NPC can "see" the player.</summary>
        /// <param name="player">The player</param>
        public bool CanSeePlayer(Player player)
        {
            if (player == null) return false;
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

        public override void Awake(Scene scene) {
            base.Awake(scene);
            player = scene.Tracker.GetEntity<Player>();
        }

        public override void DebugRender(Camera camera)
        {
            if (player == null) {
                base.DebugRender(camera);
                return;
            }
            if (AI == AIType.NodeWalk) Draw.HollowRect(JumpCheckCollider.AddPosition(this.Position), Color.Yellow);
            base.DebugRender(camera);
            if (AI == AIType.NodeWalk) Draw.HollowRect(JumpCheckCollider.AddPosition(this.Position), Color.Yellow * 0.5f);
            if (path != null && path.Count > 1)
            {
                for (int i = 0; i < (path.Count - 1); i++)
                {
                    Draw.Line(path[i], path[i+1], Color.Red);
                    Draw.Rect(path[i] + new Vector2(-2f, -2f), 3f, 3f, Color.Red);
                }
            } else if (AI == AIType.Fly) {
                Draw.Line(this.Center, player.Center, Color.Red);
            }
	    }

        /// <summary>Kills (destroys) the NPC.</summary>
        public virtual void Kill() {
            if (Sprite.Has("gore") && Settings.Instance.DisableFlashes == false) {
                Sprite.Animation gores = Sprite.Animations["gore"];
                for (int i = 0; i < gores.Frames.Length; i++)
                {
                    MTexture gore = gores.Frames[i];
                    level.Add(new GoreParticle(
                        gore,
                        this.Center,
                        Calc.AngleToVector(Calc.Random.Range((float)Math.PI * 1.75f, (float)Math.PI * 1.25f), 200f),
                        8f
                    ));
                }
            } else {
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

        private int stMovingUpdate()
        {
            if (FaceMovement) {
                Sprite.Rotation = Velocity.Angle() - (float)Math.PI;
                Sprite.FlipY = Math.Cos(Sprite.Rotation) < 0f;
            } else if (Velocity.Length() > 0.1f) {
                Sprite.FlipX = Velocity.X > 0f;
            }
            // At the end of the frame our state changes to this function's return value.
            return StateMachine.State; 
        }

        public override void Update()
        {
            base.Update();
            if (player == null || StateMachine.State == (int)St.Dummy) return;
            if (WaitForMovement && player.JustRespawned) return;
            
            int newState = AIUpdate();
            if (Velocity.Length() > 0.1f) {
                StateMachine.State = newState;
            } else {
                StateMachine.State = (int)St.Idle;
            }
                
            MoveH(Velocity.X * Engine.DeltaTime);
            MoveV(Velocity.Y * Engine.DeltaTime);
        }
        /// <summary>
        /// Also known as: Shall the character ascend into the heavens beyond our mortal plane?
        /// </summary>
        public bool WalkerJumpCheck() {
            Collider oldCollider = this.Collider;
            this.Collider = this.JumpCheckCollider;
            bool colliding = this.CollideCheck<Solid>();
            this.Collider = oldCollider;
            return colliding && this.OnGround();
        }
        
        public void WalkerFall() {
            if (!this.OnGround()) Velocity.Y = Calc.Approach(Velocity.Y, Speed.Y, 2f * Speed.Y * Engine.DeltaTime);
            else if (Velocity.Y > 0f) Velocity.Y = 0f;
        }

        public override void Render()
        {
            if (OutlineEnabled) Sprite.DrawOutline();
            base.Render();
        }

        /// <summary>
        /// An overridable method used for the NPC's AI.
        /// Use the <see cref="Velocity"/> field to move the NPC. (The base method provides great examples of what you can do)
        /// </summary>
        public virtual int AIUpdate()
        {            
            switch (AI)
            {
                case AIType.Swim:
                    if (swimAIwater == null) swimAIwater = CollideFirst<Water>();
                    else {
                        if (this.CollideRect(new Rectangle(
                            (int)swimAIwater.Collider.AbsoluteX,
                            (int)swimAIwater.Collider.AbsoluteY + 16,
                            (int)swimAIwater.Collider.Width,
                            (int)swimAIwater.Collider.Height - 16
                        ))) {
                            StateMachine.State = (int)St.Flying;
                            if (player.CollideCheck(swimAIwater)) {
                                Velocity = Calc.Approach(Velocity, (player.Position - this.Position).SafeNormalize() * Speed, Acceleration * Engine.DeltaTime);
                            } else {
                                Velocity = Calc.Approach(Velocity, Vector2.Zero, Acceleration * 0.25f * Engine.DeltaTime); 
                            }
                        } else {
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

                    if (bgTileAtPos == '0') {
                        goto case AIType.ChaseWalk;
                    } else {
                        Velocity = Calc.Approach(Velocity, (player.Position - this.Position).SafeNormalize() * Speed.X, Acceleration * Engine.DeltaTime);
                        return (int)St.Flying;
                    }
                case AIType.Fly:
                    Velocity = Calc.Approach(Velocity, (player.Position - this.Position).SafeNormalize() * Speed, Acceleration * Engine.DeltaTime);
                    return (int)St.Walking;
                case AIType.SmartFly:
                    if (Scene.OnInterval(0.2f) && CanSeePlayer(player)) {
                        nodeIndex = 0;
                        (Scene as Level).Pathfinder.Find(ref path, base.Center, player.Center, false, logging: true);
                    }
                    Velocity = Calc.Approach(Velocity, (GetNodeDirection(path.ToArray()) * Speed), Acceleration * Engine.DeltaTime);
                    return (int)St.Flying;
                case AIType.NodeWalk:
                    if (WalkerJumpCheck()) {
                        Velocity.Y = -JumpHeight;
                    }
                    
                    WalkerFall();
                    Velocity.X = Calc.Approach(Velocity.X, GetNodeDirection(Nodes, true).Sign().X * Speed.X, Acceleration * Engine.DeltaTime);
                    return (int)St.Walking;
                case AIType.ChaseWalk:
                    if (WalkerJumpCheck()) {
                        Velocity.Y = -JumpHeight;
                    }
                    
                    WalkerFall();
                    Velocity.X = Calc.Approach(Velocity.X, (player.Position - this.Position).Sign().X * Speed.X, Acceleration * Engine.DeltaTime);
                    return (int)St.Walking;
                case AIType.ChaseJump:
                    wanderAINextMoveTimer -= Engine.DeltaTime;
                    if (wanderAINextMoveTimer < 0f) {
                        if (this.OnGround()) Velocity.Y = -JumpHeight;
                        wanderAINextMoveTimer = Calc.Random.NextFloat(2f - 0.5f) + 0.5f;
                    }
                    
                    WalkerFall();
                    if (this.OnGround()) Velocity.X = Calc.Approach(Velocity.X, 0, Acceleration * Engine.DeltaTime);
                    else Velocity.X = Calc.Approach(Velocity.X, (player.Position - this.Position).Sign().X * Speed.X, Acceleration * Engine.DeltaTime);
                    return (int)St.Walking;
                case AIType.Wander:
                    // The move timer gets reduced by 1.5 each second when moving.
                    // The move timer gets reduced by 1.0 each second when not moving.
                    wanderAINextMoveTimer -= Math.Abs(Velocity.X) < 0.1f ? Engine.DeltaTime : Engine.DeltaTime * 1.5f;
                    if (wanderAINextMoveTimer < 0f) {
                        wanderAINextMoveTimer = Calc.Random.NextFloat(5f - 2f) + 2f;
                        Velocity.X = Calc.Random.Choose<float>(0f, 1f, -1f);
                    }
                    WalkerFall();
                    if (WalkerJumpCheck()) {
                        Velocity.Y = -JumpHeight;
                    }
                    Velocity.X = Calc.Approach(Velocity.X, Math.Sign(Velocity.X) * Speed.X, Acceleration * Engine.DeltaTime);
                    return (int)St.Walking;
            }
            return StateMachine.State;
        }
    }
}
