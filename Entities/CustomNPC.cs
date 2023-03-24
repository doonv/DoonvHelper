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
        public enum St {
            Dummy  = 0,
            Idle   = 1,
            Moving = 2
        }
        public enum AIType {
            Swim,
            SmartFly,
            Fly,
            NodeWalk,
            SmartWalk,
            Wander
        }

        protected Sprite Sprite;
        public AIType AI;
        public float Speed;
        public float Acceleration;
        public Vector2 Velocity = new Vector2(0f, 0f);
        public readonly Vector2[] Nodes;
        public float FallSpeed;
        public StateMachine StateMachine;
        /// <summary>Makes the sprite rotate towards where its heading instead of just flipping</summary>
        public bool FaceMovement;
        public bool AIEnabled = true;
        public bool WaitForMovement;

        /// <summary>Used for `Wander` AI</summary>
        private float nextMoveTimer = 0f; 
        private int nodeIndex = 0;
        private List<Vector2> path;
        private Player player;
        private Level level;

        public CustomNPC(
            Vector2[] nodes,
            Hitbox hitbox,
            float speed,
            float acceleration,
            AIType ai,
            float fallSpeed,
            string spriteID,
            bool faceMovement,
            bool waitForMovement = true
        ) : base(nodes[0]) 
        {
            hitbox.Position.X -= hitbox.Width / 2f;
            hitbox.Position.Y -= hitbox.Height;
            this.Nodes = nodes;
            base.Collider = hitbox;
            this.AI = ai;
            this.Speed = speed;
            this.Acceleration = acceleration;
            this.FallSpeed = fallSpeed;
            this.FaceMovement = faceMovement;
            this.path = new List<Vector2>();
            this.WaitForMovement = waitForMovement;

            this.StateMachine = new StateMachine(3);
            this.StateMachine.SetCallbacks((int)St.Dummy, null, null, delegate  { Sprite.PlaySafe("idle"); Velocity.X = 0f;   }, null); // Dummy State
            this.StateMachine.SetCallbacks((int)St.Idle, null, null, delegate   { Sprite.PlaySafe("idle");   }, null); // Idle State
            this.StateMachine.SetCallbacks((int)St.Moving, stMovingUpdate, null, delegate { Sprite.PlaySafe("moving"); }, null); // Moving State
            // this.StateMachine.State = (int)St.Idle;
            Add(this.StateMachine);

            Add(Sprite = GFX.SpriteBank.Create(spriteID));
            
        }

        public CustomNPC(EntityData data, Vector2 offset)
            : this(
                data.NodesWithPosition(offset),
                new Hitbox(
                    width: data.Float("hitboxWidth", 16f),
                    height: data.Float("hitboxHeight", 16f),
                    x: data.Float("hitboxXOffset", 0f),
                    y: data.Float("hitboxYOffset", 0f)
                ),
                data.Float("maxSpeed", 32f),
                data.Float("acceleration", 6f),
                Calc.StringToEnum<AIType>(data.Attr("aiType", "Fly").Replace(" ", "")),
                data.Float("fallSpeed", 10f),
                data.Attr("spriteID", "DoonvHelper_CustomEnemy_zombie"),
                data.Bool("faceMovement", false),
                data.Bool("waitForMovement", true)
            ) 
        {
        }

        private Vector2 GetNodeDirection(Vector2[] path, bool loop = false)
        {
            if (nodeIndex >= path.Length)
            {
                if (loop) { nodeIndex = (-nodeIndex) + 1; }
                else { return Vector2.Zero; }
            }
            // Logger.Log(LogLevel.Info, "DoonvHelper big", Vector2.DistanceSquared(base.Center, path[Math.Abs(nodeIndex)]).ToString());
            if (Vector2.DistanceSquared(base.Position, path[Math.Abs(nodeIndex)]) < 36f)
            {
                // Logger.Log(LogLevel.Info, "DoonvHelper", nodeIndex.ToString());
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

        public override void Awake(Scene scene) {
            base.Awake(scene);
            
            level = (scene as Level);
            player = scene.Tracker.GetEntity<Player>();
            StateMachine.State = (int)St.Idle;
        }

        public override void DebugRender(Camera camera)
        {
            base.DebugRender(camera);

            if (path != null)
            {
                for (int i = 0; i < (path.Count - 1); i++)
                {
                    Draw.Line(path[i], path[i+1], Color.Red);
                    Draw.Rect(path[i] + new Vector2(-2f, -2f), 3f, 3f, Color.Red);
                }
                // Draw.Rect(path[path.Count-1].X - 2f, path[path.Count-1].Y - 2f, 4f, 4f, Color.Red);
            }
            Draw.Point(this.Position, Color.Aqua);
	    }

        /// <summary>Kills (destroys) the NPC</summary>
        public virtual void Kill() {
            Scene.OnEndOfFrame += delegate {
                this.RemoveSelf();
            };
        }

        private int stMovingUpdate()
        {
            if (FaceMovement) {
                Sprite.Rotation = Velocity.Angle() - (float)Math.PI;
                Sprite.FlipY = Math.Cos(Sprite.Rotation) < 0f;
            } else if (Math.Abs(Velocity.X) > 0.1) {
                Sprite.FlipX = Velocity.X > 0f;
            }
            // At the end of the frame our state changes to this function's return value.
            return (int)St.Moving; 
        }

        public override void Update()
        {
            base.Update();
            if (player == null || StateMachine.State == (int)St.Dummy) return;

            bool playerSeen = CanSeePlayer(player);
            if (Math.Abs(Velocity.X) > 0.1f) {
                StateMachine.State = (int)St.Moving;
            } else {
                StateMachine.State = (int)St.Idle;
            }

            if (WaitForMovement && player.JustRespawned) return;

            switch (AI)
            {
                case AIType.Fly:
                    Velocity = Calc.Approach(Velocity, (player.Position - this.Position).SafeNormalize(Speed), Acceleration);
                    break;
                case AIType.SmartFly:
                    if (playerSeen && Scene.OnInterval(0.2f)) {
                        nodeIndex = 0;
                        (Scene as Level).Pathfinder.Find(ref path, base.Center, player.Center, false, logging: true);
                    }
                    // Logger.Log(LogLevel.Info, "DoonvHelper", GetNodeDirection(path.ToArray()).ToString());
                    Velocity = Calc.Approach(Velocity, (GetNodeDirection(path.ToArray()) * Speed), Acceleration);
                    break;
                case AIType.NodeWalk:
                    // Logger.Log(LogLevel.Info, "DoonvHelper", String.Format("{0} owo {1}", base.ExactPosition, lastPos,  base.ExactPosition - lastPos));
                    if ((Math.Abs(Velocity.X) < 0.5f && (Math.Abs(Velocity.Y) < 0.5f || this.OnGround())) && Scene.OnInterval(0.2f)) {
                        Velocity.Y -= 80f;
                    }
                    // Logger.Log(LogLevel.Info, "DoonvHelper", GetNodeDirection(nodes, true).ToString());
                    Velocity.Y = Calc.Approach(Velocity.Y, FallSpeed, 2f);
                    Velocity.X = Calc.Approach(Velocity.X, GetNodeDirection(Nodes, true).Sign().X * Speed, Acceleration);
                    break;
                case AIType.Wander:
                    nextMoveTimer -= Math.Abs(Velocity.X) < 5 ? Engine.DeltaTime : Engine.DeltaTime * 1.5f;
                    if (nextMoveTimer < 0f) {
                        nextMoveTimer = Calc.Random.NextFloat(5f - 2f) + 2f;
                        Velocity.X = Calc.Random.NextFloat() > 0.33333f ? Calc.Random.Facing() : 0f;
                    }
                    Velocity.Y = Calc.Approach(Velocity.Y, FallSpeed, 2f);
                    Velocity.X = Calc.Approach(Velocity.X, Math.Sign(Velocity.X) * Speed, Acceleration);
                    break;
            }
            MoveH(Velocity.X * Engine.DeltaTime);
            MoveV(Velocity.Y * Engine.DeltaTime);
        }
    }
}
