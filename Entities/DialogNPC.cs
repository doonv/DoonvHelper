using Celeste.Mod.DoonvHelper.Utils;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Celeste.Mod.DoonvHelper.Entities;

[CustomEntity("DoonvHelper/DialogNPC")]
[Tracked]
public class DialogNPC : CustomNPC
{
	public string LuaCutsceneFilePath;
	public string BasicDialogID;
	public string CsharpEventID;

	private bool cutsceneModeEnabled = false;

	public bool CutsceneModeEnabled
	{
		get { return cutsceneModeEnabled; }
		set
		{
			cutsceneModeEnabled = value;
			StateMachine.State = value ? (int)St.Dummy : (int)St.Idle;
			if (value == true && Scene is not null)
			{
				Sprite.FlipX = Scene.Tracker.GetEntity<Player>().X > this.X;
			}
		}
	}
	// This cutscene simply says dialog
	protected class BasicTalkCutscene : CutsceneEntity
	{
		private Player player;
		private DialogNPC npc;
		public BasicTalkCutscene(Player player, DialogNPC npc)
		{
			this.player = player;
			this.npc = npc;
		}
		public override void OnBegin(Level level)
		{
			Add(new Coroutine(Cutscene(level)));
		}
		private IEnumerator Cutscene(Level level)
		{
			player.StateMachine.State = Player.StDummy;
			player.StateMachine.Locked = true;
			player.ForceCameraUpdate = true;
			npc.CutsceneModeEnabled = true;
			yield return Textbox.Say(npc.BasicDialogID);
			EndCutscene(level);
		}
		public override void OnEnd(Level level)
		{
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
		new Vector2(data.Float("XSpeed", 48f), data.Float("YSpeed", 240f)),
		data.Float("acceleration", 6f),
		data.Enum<AIType>("aiType", AIType.Wander),
		data.Attr("spriteID", "DoonvHelper_CustomEnemy_zombie"),
		data.Float("jumpHeight", 50f),
		data.Enum<FacingAt>("facing", FacingAt.MovementFlip),
		data.Bool("waitForMovement", true),
		data.Bool("outlineEnabled", true),
		new Point(
			data.Int("talkBoundsWidth", 80),
			data.Int("talkBoundsHeight", 40)
		),
		new Vector2(
			data.Float("talkIndicatorX"),
			data.Float("talkIndicatorY")
		),
		data.Attr("basicDialogID", ""),
		data.Attr("luaCutscene", ""),
		data.Attr("csEventID", "")
	)
	{
	}

	public DialogNPC(
		Vector2[] nodes,
		Hitbox hitbox,
		Vector2 speed,
		float acceleration,
		AIType ai,
		string spriteID,
		float jumpHeight,
		FacingAt facing,
		bool waitForMovement,
		bool outlineEnabled,
		Point talkBoundsSize,
		Vector2 talkIndicatorOffset,
		string basicDialogID = "",
		string luaCutscene = "",
		string csEventID = ""
	) : base(nodes, hitbox, speed, acceleration, ai, spriteID, jumpHeight, facing, waitForMovement, outlineEnabled)
	{
		this.LuaCutsceneFilePath = luaCutscene;
		this.BasicDialogID = basicDialogID;
		this.CsharpEventID = csEventID;
		Add(new TalkComponent(
			new Rectangle((int)(talkBoundsSize.X * -0.5f), -talkBoundsSize.Y, talkBoundsSize.X, talkBoundsSize.Y),
			new Vector2(0f, -this.Sprite.Texture.Height) + talkIndicatorOffset,
			OnTalk
		));
	}

	public void OnTalk(Player player)
	{
		if (!String.IsNullOrWhiteSpace(this.BasicDialogID))
		{
			(Scene as Level).Add(new BasicTalkCutscene(player, this));
		}
		if (!String.IsNullOrWhiteSpace(this.LuaCutsceneFilePath))
		{
			// Thanks cruor, very helpful! /s

			// Get the LuaCutscenes module
			LuaCutscenes.LuaCutscenesMod module = DoonvHelperModule.LuaCutscenesModule;

			if (module is not null)
			{
				// Use reflection to create an instance of the internal
				// class `LuaCutscenes.LuaCutsceneTrigger` using the `EntityData`.
				Trigger cutsceneTrigger = (Trigger)module.GetType().Assembly
					.GetType("Celeste.Mod.LuaCutscenes.LuaCutsceneTrigger") // Get the class
					?.GetConstructor(new Type[] { typeof(EntityData), typeof(Vector2) }) // Get the constructor
					?.Invoke(new object[] {
							new EntityData() { // We make up the entityData of the trigger
                                Values = new Dictionary<string, object>() {
									["onlyOnce"] = false,
									["unskippable"] = false,
									["filename"] = this.LuaCutsceneFilePath,
									["arguments"] = ""
								},
								Position = Vector2.Zero,
								Width = 1,
								Height = 1
							},
							Vector2.Zero
					}); // Call the constructor

				Scene.Add(cutsceneTrigger);

				// We need to wait until the next `Update` before activating the cutscene.
				Action<Entity> activateCutscene = null;
				activateCutscene = (_) =>
				{
					cutsceneTrigger.OnEnter(player);
					// We must set `activateCutscene` to `null` before 
					// setting it to this `Action` or this line won't work.
					this.PreUpdate -= activateCutscene;
				};
				this.PreUpdate += activateCutscene;
			}
		}
		if (!String.IsNullOrWhiteSpace(this.CsharpEventID))
		{
			// Create the trigger
			EventTrigger trigger = new EventTrigger(new EntityData()
			{
				// We make up the entityData of the trigger
				Values = new Dictionary<string, object>()
				{
					["event"] = CsharpEventID,
					["onSpawn"] = false
				},
				Position = Vector2.Zero,
				Width = 1,
				Height = 1
			}, Vector2.Zero);

			Scene.Add(trigger);

			// We need to wait until the next `Update` before activating the cutscene.
			Action<Entity> activateCutscene = null;
			activateCutscene = (_) =>
			{
				EventTrigger.TriggerCustomEvent(trigger, player, CsharpEventID);
				// We must set `activateCutscene` to `null` before 
				// setting it to this `Action` or this line won't work.
				this.PreUpdate -= activateCutscene;
			};
			this.PreUpdate += activateCutscene;
		}
	}
}
