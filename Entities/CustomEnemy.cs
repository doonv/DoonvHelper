using Celeste.Mod.DoonvHelper.Utils;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;

namespace Celeste.Mod.DoonvHelper.Entities {
    [CustomEntity("DoonvHelper/CustomEnemy")]
    public class CustomEnemy : CustomNPC
    {
        public CustomEnemy(EntityData data, Vector2 offset) : this(
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
            data.Bool("outlineEnabled", true),
            data.Attr("bulletSpriteID", "badeline_projectile"),
            data.Float("bulletRecharge", 0f),
            data.Float("bulletSpeed", 300f),
            data.Float("bulletSafeTime", 0.25f)
        ) 
        {
        }
        public string BulletSpriteID;
        public float BulletRecharge;
        public float BulletSpeed;
        public float BulletSafeTime;

        public float BulletShootTimer = 0f;
        private Player player;

        public CustomEnemy(
            Vector2[] nodes,
            Hitbox hitbox,
            Vector2 speed,
            float acceleration,
            AIType ai,
            string spriteID,
            float jumpHeight,
            bool faceMovement,
            bool waitForMovement,
            bool outlineEnabled,
            string bulletSpriteID = "badeline_projectile",
            float bulletRecharge = 0.0f,
            float bulletSpeed = 300f,
            float bulletSafeTime = 0.25f
        ) : base(nodes, hitbox, speed, acceleration, ai, spriteID, jumpHeight, faceMovement, waitForMovement, outlineEnabled)
        {
            this.BulletSpriteID = bulletSpriteID;
            this.BulletRecharge = bulletRecharge;
            this.BulletSpeed = bulletSpeed;
            this.BulletSafeTime = bulletSafeTime;
            Add(new PlayerCollider(OnPlayerCollide, this.Collider.Inflated(-4f)));
        }

        public override void Awake(Scene scene)
        {
            base.Awake(scene);
            player = scene.Tracker.GetEntity<Player>();
        }

        [Pooled]
        public class Bullet : Actor {
            public Sprite Sprite;
            public Vector2 Velocity;
            public float SafeTime;

            private Player player;

            public Bullet() : base(Vector2.Zero) {}
            public Bullet Init(string spriteID, Vector2 position, Vector2 velocity, float safeTime, Player player) {
                Add(Sprite = GFX.SpriteBank.Create(spriteID));
                Collider = new Hitbox(6f, 6f, -3f, -3f);
                Position = position;
                Velocity = velocity;
                SafeTime = safeTime;
                this.player = player;
                return this;
            }

            public override void Update()
            {
                base.Update();
                SafeTime -= Engine.DeltaTime;
                if (this.CollideCheck(player)) {
                    if (SafeTime < 0f) {
                        player.Die((player.Center - this.Center).SafeNormalize());
                    }
                    destroy();
                }
                if ((Scene as Level).IsInCamera(Position, 32f) == false) destroy();
                MoveH(Velocity.X * Engine.DeltaTime, destroy);
                MoveV(Velocity.Y * Engine.DeltaTime, destroy);
            }

            private void destroy() => Scene.OnEndOfFrame += () => this.RemoveSelf();
            private void destroy(CollisionData _) => Scene.OnEndOfFrame += () => this.RemoveSelf();
        }

        public override void Update()
        {
            base.Update();
            if (BulletRecharge > 0f) {
                BulletShootTimer -= Engine.DeltaTime;
                if (BulletShootTimer < 0f) {
                    Shoot();
                    BulletShootTimer = BulletRecharge;
                }
            }
        }

        public void Shoot()
        {
            // I don't know how the pooler works really I'm just copying from Spekio's toolbox
            // I hope this increases performance or something? idk
            Scene.Add(Engine.Pooler.Create<Bullet>().Init(
                BulletSpriteID,
                this.Center,
                (player.Center - this.Center).SafeNormalize(BulletSpeed),
                BulletSafeTime,
                player
            ));
        }

        private void OnPlayerCollide(Player player)
        {
            player.Die((player.Center - this.Center).SafeNormalize());
        }
    }
}