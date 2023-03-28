using Celeste.Mod.Entities;
using Monocle;
using System;
using System.Collections;

namespace Celeste.Mod.DoonvHelper.Entities
{
    [CustomEvent("DoonvHelper/TestCutscene")]
    public class TestCutscene : CutsceneEntity
    {

        private Player player;

        public TestCutscene(EventTrigger trigger, Player player, string eventID)
        {
            this.player = player;
        }

        public override void OnBegin(Level level)
        {
            Add(new Coroutine(Cutscene(level)));
        }
        private IEnumerator BetterDummyMoveTo(float x, bool walk = false)
        {
            player.StateMachine.State = 11;
            if (Math.Abs(player.X - x) < 4f)
            {
                yield break;
            }

            player.DummyMoving = true;
            player.DummyAutoAnimate = false;
            player.Facing = (Facings)Math.Sign(x - player.X);

            while (Math.Abs(player.X - x) > 4f)
            {
                player.Speed.X = Calc.Approach(player.Speed.X, (float)Math.Sign(x - player.X) * (walk ? 64f : 90f), 1000f * Engine.DeltaTime);
                if (player.OnGround())
                {
                    if (player.Speed.X == 0f)
                    {
                        player.Sprite.Play("idle");
                    }
                    else
                    {
                        player.Sprite.Play(walk ? "walk" : "runFast");
                    }
                }
                else if (player.Speed.Y < 0f)
                {
                    player.Sprite.Play("jumpSlow");
                }
                else
                {
                    player.Sprite.Play("fallSlow");
                }
                yield return null;
            }

            player.Sprite.Play("idle");
            player.DummyAutoAnimate = true;
            player.DummyMoving = false;
        }

        private IEnumerator Cutscene(Level level)
        {
            player.StateMachine.State = 11;
            player.StateMachine.Locked = true;
            player.ForceCameraUpdate = true;
            

            yield return BetterDummyMoveTo(5460f);
            player.Jump(true, true);
            player.AutoJump = true;
            player.AutoJumpTimer = 2f;
            yield return BetterDummyMoveTo(5550f);
            yield return null;

            EndCutscene(level, true);
        }

        public override void OnEnd(Level level)
        {
            player.StateMachine.Locked = false;
            player.StateMachine.State = 0;
            player.ForceCameraUpdate = false;

            if (WasSkipped)
                level.Camera.Position = player.CameraTarget;
        }

    }
}