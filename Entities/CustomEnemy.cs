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

        public CustomEnemy(
            Vector2[] nodes,
            Hitbox hitbox,
            float speed,
            float acceleration,
            AIType ai,
            float fallSpeed,
            string spriteID,
            bool faceMovement,
            bool waitForMovement
        ) : base(nodes, hitbox, speed, acceleration, ai, fallSpeed, spriteID, faceMovement, waitForMovement)
        {
            Add(new PlayerCollider(OnPlayerCollide, base.Collider.Inflated(-4f)));
        }

        private void OnPlayerCollide(Player player)
        {
            player.Die((player.Position - this.Position).SafeNormalize());
        }
    }
}