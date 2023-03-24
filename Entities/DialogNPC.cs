using Celeste.Mod.DoonvHelper.Utils;
using Celeste.Mod.Entities;
using Celeste.Mod.LuaCutscenes;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Celeste.Mod.DoonvHelper.Entities {
    [CustomEntity("DoonvHelper/DialogNPC")]
    public class DialogNPC : CustomNPC
    {
        public string LuaCutsceneFilePath;
        public string BasicDialogID;
        private bool cutsceneModeEnabled = false;
        public bool CutsceneModeEnabled {
            get { return cutsceneModeEnabled; }
            set { 
                cutsceneModeEnabled = value;
                AIEnabled = !value;
                StateMachine.State = value ? (int)St.Dummy : (int)St.Idle;
                if (value == true && Scene != null) {
                    Sprite.FlipX = Scene.Tracker.GetEntity<Player>().X > this.X;
                }
            }
        }
        // This cutscene simply says dialog
        protected class BasicTalkCutscene : CutsceneEntity {
            private Player player;
            private DialogNPC npc;
            public BasicTalkCutscene(Player player, DialogNPC npc) {
                this.player = player;
                this.npc = npc;
            }
            public override void OnBegin(Level level) { 
                Add(new Coroutine(Cutscene(level)));
            }
            private IEnumerator Cutscene(Level level) {
                player.StateMachine.State = Player.StDummy;
                player.StateMachine.Locked = true;
                player.ForceCameraUpdate = true;
                npc.CutsceneModeEnabled = true;
                yield return Textbox.Say(npc.BasicDialogID);
                EndCutscene(level);
            }
            public override void OnEnd(Level level) {
                npc.CutsceneModeEnabled = false;
                player.ForceCameraUpdate = false;
                player.StateMachine.Locked = false;
                player.StateMachine.State = Player.StNormal;
            }
        }

        public DialogNPC(EntityData data, Vector2 offset) : this(
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
            data.Bool("waitForMovement", true),
            new Point(
                data.Int("talkBoundsWidth", 80),
                data.Int("talkBoundsHeight", 40)
            ),
            new Vector2(
                data.Float("talkIndicatorX"),
                data.Float("talkIndicatorY")
            ),
            data.Attr("basicDialogID", ""),
            data.Attr("luaCutscene", "")
        ) 
        {
        }

        public DialogNPC(
            Vector2[] nodes,
            Hitbox hitbox,
            float speed,
            float acceleration,
            AIType ai,
            float fallSpeed,
            string spriteID,
            bool faceMovement,
            bool waitForMovement,
            Point talkBoundsSize,
            Vector2 talkIndicatorOffset,
            string basicDialogID = "",
            string luaCutscene = ""
        ) : base(nodes, hitbox, speed, acceleration, ai, fallSpeed, spriteID, faceMovement, waitForMovement)
        {
            this.Depth = -1;
            this.LuaCutsceneFilePath = luaCutscene;
            this.BasicDialogID = basicDialogID;
            Add(new TalkComponent(
                new Rectangle((int)(talkBoundsSize.X * -0.5f), -talkBoundsSize.Y, talkBoundsSize.X, talkBoundsSize.Y),
                new Vector2(0f, -this.Sprite.Texture.Height),
                OnTalk,
                new TalkComponent.HoverDisplay
                {
                    Texture = GFX.Gui["hover/highlight"],
                    InputPosition = new Vector2(0f, -75f) + talkIndicatorOffset
                }
            ));
        }
        
        public void OnTalk(Player player)
        {
            Logger.Log(LogLevel.Info, "DoonvHelper", "walkie talkie!");
            if (!String.IsNullOrWhiteSpace(this.BasicDialogID)) {
                (Scene as Level).Add(new BasicTalkCutscene(player, this));
            }
            if (!String.IsNullOrWhiteSpace(this.LuaCutsceneFilePath)) {
                // DynamicData forbiddenCutscenes = DynamicData.For(LuaCutscenes.LuaTalker);
            }
        }
    }
}