// Celeste.SwitchGate
using System;
using System.Collections;
using System.Collections.Generic;
using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.DoonvHelper.Entities
{
    [CustomEntity("DoonvHelper/DashCodeGate")]
    public class DashCodeGate : Solid
    {
		public enum IconOrientation {
			Auto,
			Horizontal,
			Vertical
		}

    	public static ParticleType P_Behind = SwitchGate.P_Behind;
    	public static ParticleType P_Dust = SwitchGate.P_Dust;
		public static ParticleType Purple_Fire;
		private ParticleSystem above_particles;
    	private MTexture[,] nineSlice;
		const float iconDistance = 2f;
    	private MTexture[] inactiveIcons;
    	private MTexture[] activeIcons;
    	private Vector2 iconOffset;
    	private Wiggler wiggler;
    	private Vector2 node;
    	private SoundSource openSfx;
    	public string persistenceFlag;
    	private Color inactiveColor = Calc.HexToColor("5fcde4");
    	private Color activeColor = Color.White;
    	private Color finishColor = Calc.HexToColor("f141df");
        private DashListener dashListener;
        public string[] code;
        private List<string> currentInputs = new List<string>();
		private IconOrientation iconOrientation;
		public bool moved = false;
		public int columns;

		/// <summary>
		/// Converts a dash vector into a dash code.
		/// </summary>
		/// <param name="dir">A dash vector.</param>
		/// <returns>A dash code. (i.e. UL, R, D, DL, R, U)</returns>
        private string DashVectorToCode(Vector2 dir) {
            string text = "";

            if (dir.Y < 0f)
                text = "U";
            else if (dir.Y > 0f)
                text = "D";
            if (dir.X < 0f)
                text += "L";
            else if (dir.X > 0f)
                text += "R";
			
            return text;
        }
		/// <summary>
		/// Convert a dash code to a Vector2
		/// </summary>
		/// <param name="code">A dash code. (i.e. UL, R, D, DL, R, U)</param>
		/// <returns>A normalized vector2</returns>
		private Vector2 CodeToDashVector(string code) {
			Vector2 res = new Vector2(0f, 0f);
			foreach (char c in code) {
				switch (c)
				{
					case 'U':
						res.Y = -1f;
						break;
					case 'D':
						res.Y = 1f;
						break;
					case 'L':
						res.X = -1f;
						break;
					case 'R':
						res.X = 1f;
						break;
				}
			}
			res.Normalize();
			return res;
		}
		/// <summary>
		/// 	500 IQ algorithm right here.
		/// 	Computes how much of the code is activated
		/// </summary>
		/// <returns>
		/// 	It outputs a number depending on how much of the code is activated
		/// 	0 is for not activated at all, the code's length when fully activated.
		/// </returns>
		private int ComputeCodeCompletion() {
			int code_counter = 0;
			for (int i = 0; i < currentInputs.Count; i++)
			{
				if (currentInputs[i] == code[code_counter]) {
					code_counter += 1;
				} else {
					// If the code counter is greater than 0 and the current input does not match the corresponding code item, 
					// the loop variable i is decremented by 1. This is done to ensure that the loop re-evaluates the previous input, 
					// as it may have been the start of a correct code sequence.
					if (code_counter > 0) {
						i--;
					}
					code_counter = 0;
				}
			}
			return code_counter;
		}

		/// <summary>
		/// Gets the (centered) draw position of an arrow.
		/// </summary>
		/// <param name="arrowId">The id of the arrow</param>
		/// <returns>The draw position</returns>
		private Vector2 getArrowDrawPos(int arrowId) {
			//! WARNING: Bad code ahead!
			// Viewer discretion is advised.

			IconOrientation orientation = this.iconOrientation;
			if (orientation == IconOrientation.Auto) {
				// In the `Auto` case, we check which orientation is better, 
				// and convert the orientation into that one.
				orientation = 
					this.Width > this.Height ? 
					IconOrientation.Horizontal :
					IconOrientation.Vertical;
			}

			//*I apologize in advance for anyone trying to read this code.
			// I'm sorry. I did my best to tell you what it does but...
			// I just used trial and error for this one.
			if (orientation ==  IconOrientation.Horizontal) {
				int columnSize = (int)Math.Ceiling((double)code.Length / (double)columns);
				int currentColumn = (arrowId) / columnSize; // Stores the column we should be in.
				return
					this.Position + // Get our positon
					new Vector2(this.Width / 2f, this.Height / 2f) + // Center it to our block
					new Vector2(
						(arrowId - (currentColumn * columnSize)) * ((activeIcons[0].Width + iconDistance)),
						currentColumn * (activeIcons[0].Height + iconDistance)
					) + // Go right depending on arrow_id
					new Vector2(
						((columnSize - 1) / 2f) * -(float)(activeIcons[0].Width + iconDistance),
						(columns - 1) * -((activeIcons[0].Height + iconDistance) / 2f)
					); // Shift all arrows left depending on how many arrows and columns we can have.
			} else {
				float currentColumn = arrowId % columns; // Stores the column we should be in.
				return
					this.Position + // Get our positon
					new Vector2(this.Width / 2f, this.Height / 2f) + // Center it to our block
					new Vector2(
						currentColumn * (activeIcons[0].Width + iconDistance),
						(arrowId - currentColumn) * ((activeIcons[0].Height + iconDistance) / columns)
					) + // Go down depending on arrow_id
					new Vector2(
						(columns - 1) * -((activeIcons[0].Width + iconDistance) / 2f),
						(code.Length / columns - 1) * -((activeIcons[0].Height + iconDistance) / 2f)
					); // Shift all arrows up depending on how many arrows and columns we can have.
			}

		}

    	public DashCodeGate(Vector2 position, 
            float width, 
            float height, 
            Vector2 node, 
            string persistenceFlag, 
            string spriteName, 
            string code,
			IconOrientation iconOrientation = IconOrientation.Auto,
			int columns = 1
        ) : base(position, width, height, safe: false)
    	{
			this.iconOrientation = iconOrientation;
			this.columns = columns;
			Purple_Fire = new ParticleType
			{
				Source = GFX.Game["particles/fire"],
				Color = Calc.HexToColor("f141df"),
				Color2 = Color.White,
				ColorMode = ParticleType.ColorModes.Fade,
				FadeMode = ParticleType.FadeModes.Late,
				Acceleration = new Vector2(0f, -10f),
				LifeMin = 0.8f,
				LifeMax = 1.0f,
				Size = 0.3f,
				SizeRange = 0.2f,
				Direction = -(float)Math.PI / 2f,
				DirectionRange = (float)Math.PI / 6f,
				SpeedMin = 4f,
				SpeedMax = 6f,
				SpeedMultiplier = 0.2f,
				ScaleOut = true
			};
            this.code = code.ToUpper().Split(',');
			this.inactiveIcons = new MTexture[this.code.Length];
			this.activeIcons = new MTexture[this.code.Length];
			for (int i = 0; i < this.code.Length; i++)
			{
				Vector2 v = CodeToDashVector(this.code[i]);
				string text = "";
				if (v.Y < 0f)
				{
					text = "up-";
				}
				else if (v.Y > 0f)
				{
					text = "down-";
				}
				if (v.X < 0f)
				{
					text += "left";
				}
				else if (v.X > 0f)
				{
					text += "right";
				}
				this.inactiveIcons[i] = GFX.Game[String.Format(
					"objects/DoonvHelper/dashcodegate/arrows/inactive-{0}", 
				text.Trim('-'))];
				this.activeIcons[i] = GFX.Game[String.Format(
					"objects/DoonvHelper/dashcodegate/arrows/active-{0}",
				text.Trim('-'))];
			}
            Logger.Log(LogLevel.Info, "DoonvHelper", String.Join(", ", this.code));
    		this.node = node;
    		this.persistenceFlag = persistenceFlag;
    		// Add(icon = new Sprite(GFX.Game, "objects/switchgate/icon"));
    		// icon.Add("spin", "", 0.1f, "spin");`
    		// icon.Play("spin");
    		// icon.Rate = 0f;
    		// icon.Color = inactiveColor;
    		// icon.Position = (iconOffset = new Vector2(width / 2f, height / 2f));
    		// icon.CenterOrigin();
    		// Add(wiggler = Wiggler.Create(0.5f, 4f, delegate(float f)
    		// {
    		// 	icon.Scale = Vector2.One * (1f + f);
    		// }));
    		MTexture mTexture = GFX.Game["objects/switchgate/" + spriteName];
    		nineSlice = new MTexture[3, 3];
    		for (int i = 0; i < 3; i++)
    		{
    			for (int j = 0; j < 3; j++)
    			{
    				nineSlice[i, j] = mTexture.GetSubtexture(new Rectangle(i * 8, j * 8, 8, 8));
    			}
    		}
    		Add(openSfx = new SoundSource());
    		Add(new LightOcclude(0.5f));
            Add(dashListener = new DashListener());
            dashListener.OnDash = delegate (Vector2 dir) {
                // Logger.Log(LogLevel.Info, "DoonvHelper", dir.ToString());
				int oldCompletion = ComputeCodeCompletion();
                string code = DashVectorToCode(dir);
                currentInputs.Add(code);
                if (currentInputs.Count > this.code.Length)
                {
                    currentInputs.RemoveAt(0);
                }

				int completion = ComputeCodeCompletion();
				if (completion != 0) {
					SceneAs<Level>().Displacement.AddBurst(
						getArrowDrawPos(completion-1), 
						0.3f, 
						8f, 
						24f, 
						0.4f
					);
				} else if (completion != oldCompletion) {
					for (int i = 0; i < oldCompletion; i++)
					{
						SceneAs<Level>().Displacement.AddBurst(
							getArrowDrawPos(i), 
							1f, 
							8f, 
							24f, 
							0.2f
						);
					}
				}
                if (completion == this.code.Length) {
                    Add(new Coroutine(Sequence(node)));
                }
            };
    	}

    	public DashCodeGate(EntityData data, Vector2 offset)
    		: this(
                data.Position + offset,
                data.Width, data.Height,
                data.Nodes[0] + offset,
                data.Attr("persistenceFlag", defaultValue: ""),
                data.Attr("sprite", defaultValue:"block"),
                data.Attr("code", defaultValue: "U,D,L,R"),
				(IconOrientation)Enum.Parse(
					typeof(IconOrientation),
					data.Attr("iconOrientation", defaultValue: "Auto"),
					true
				),
				data.Int("columns", defaultValue: 1)
            )
    	{
    	}

		// We have to put this method into the `Added()` method
		// because `Entity.Scene` is only set in `Added()`
		// For more info this celestecord thread is pretty useful:
		// https://discord.com/channels/403698615446536203/908809001834274887/1078730402111434764
		public override void Added(Scene scene) {
			base.Added(scene);
			this.above_particles = new ParticleSystem(this.Depth - 1, 200);
			(scene as Level).Add(this.above_particles);
		}
		// Then we remove the particle system when the gate is removed to not leave useless feces around.
		public override void Removed(Scene scene) {
			base.Removed(scene);
			(scene as Level).Remove(this.above_particles);
			this.above_particles = null;
		}
 
    	public override void Awake(Scene scene)
    	{
    		base.Awake(scene);

			// for (int i = 0; i < code.Length; i++)
			// {
			// 	getArrowDrawPos(i, true);
			// }

			// If we have already activated the gate and the gate has a persistence flag
			// added to it, and the player goes back into the room with the gate.
			// We want to reactivate the gate and put it where it used to be.
    		if (
				!String.IsNullOrWhiteSpace(persistenceFlag) && 
				SceneAs<Level>().Session.GetFlag(persistenceFlag)
			)
    		{
    			MoveTo(node);
				moved = true;
    		}
    	}

    	public override void Render()
    	{
    		float num = base.Collider.Width / 8f - 1f;
    		float num2 = base.Collider.Height / 8f - 1f;
    		for (int i = 0; (float)i <= num; i++)
    		{
    			for (int j = 0; (float)j <= num2; j++)
    			{
    				int num3 = (((float)i < num) ? Math.Min(i, 1) : 2);
    				int num4 = (((float)j < num2) ? Math.Min(j, 1) : 2);
    				nineSlice[num3, num4].Draw(Position + base.Shake + new Vector2(i * 8, j * 8));
    			}
    		}
			
			for (int i = 0; i < code.Length; i++)
			{
				Vector2 draw_pos = getArrowDrawPos(i);

				if (
					i < ComputeCodeCompletion() || (
						!String.IsNullOrWhiteSpace(persistenceFlag) && 
						SceneAs<Level>().Session.GetFlag(persistenceFlag)
					) || moved
				) {
					if (Scene.OnInterval(0.1f)) {
						above_particles.Emit(
							Purple_Fire, 
							draw_pos + Calc.AngleToVector(Calc.Random.NextAngle(), 3f)
						);
					}
					activeIcons[i].DrawCentered(draw_pos);
				} else {
					inactiveIcons[i].DrawCentered(draw_pos);
				}
				
			}
			
    		// icon.Position = iconOffset + base.Shake;
    		// icon.DrawOutline();
    		base.Render();
    	}

		/// <summary>
		/// Moves and activates the dash gate.
		/// </summary>
    	private IEnumerator Sequence(Vector2 node)
    	{
    		Vector2 start = Position;
    		if (moved) {
				yield break;
			}
			if (!String.IsNullOrWhiteSpace(persistenceFlag)) {
				if (SceneAs<Level>().Session.GetFlag(persistenceFlag))
				{
					yield break;
				}
				SceneAs<Level>().Session.SetFlag(persistenceFlag);
			}
			moved = true;
			dashListener.Active = false;
    		openSfx.Play("event:/game/general/seed_complete_berry");
    		yield return 1f;
    		openSfx.Play("event:/game/general/touchswitch_gate_open");
    		StartShaking(0.5f);
    		// while (icon.Rate < 1f)
    		// {
    		// 	icon.Color = Color.Lerp(inactiveColor, activeColor, icon.Rate);
    		// 	icon.Rate += Engine.DeltaTime * 2f;
    		// 	yield return null;
    		// }
    		yield return 0.1f;
    		int particleAt = 0;
    		Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeOut, 2f, start: true);
    		tween.OnUpdate = delegate(Tween t)
    		{
    			MoveTo(Vector2.Lerp(start, node, t.Eased));
    			if (Scene.OnInterval(0.1f))
    			{
    				particleAt++;
    				particleAt %= 2;
    				for (int n = 0; (float)n < Width / 8f; n++)
    				{
    					for (int num2 = 0; (float)num2 < Height / 8f; num2++)
    					{
    						if ((n + num2) % 2 == particleAt)
    						{
    							SceneAs<Level>().ParticlesBG.Emit(P_Behind, Position + new Vector2(n * 8, num2 * 8) + Calc.Random.Range(Vector2.One * 2f, Vector2.One * 6f));
    						}
    					}
    				}
    			}
    		};
    		Add(tween);
    		yield return 1.8f;
    		bool collidable = Collidable;
    		Collidable = false;
    		if (node.X <= start.X)
    		{
    			Vector2 vector = new Vector2(0f, 2f);
    			for (int i = 0; (float)i < Height / 8f; i++)
    			{
    				Vector2 vector2 = new Vector2(Left - 1f, Top + 4f + (float)(i * 8));
    				Vector2 point = vector2 + Vector2.UnitX;
    				if (Scene.CollideCheck<Solid>(vector2) && !Scene.CollideCheck<Solid>(point))
    				{
    					SceneAs<Level>().ParticlesFG.Emit(P_Dust, vector2 + vector, (float)Math.PI);
    					SceneAs<Level>().ParticlesFG.Emit(P_Dust, vector2 - vector, (float)Math.PI);
    				}
    			}
    		}
    		if (node.X >= start.X)
    		{
    			Vector2 vector3 = new Vector2(0f, 2f);
    			for (int j = 0; (float)j < Height / 8f; j++)
    			{
    				Vector2 vector4 = new Vector2(Right + 1f, Top + 4f + (float)(j * 8));
    				Vector2 point2 = vector4 - Vector2.UnitX * 2f;
    				if (Scene.CollideCheck<Solid>(vector4) && !Scene.CollideCheck<Solid>(point2))
    				{
    					SceneAs<Level>().ParticlesFG.Emit(P_Dust, vector4 + vector3, 0f);
    					SceneAs<Level>().ParticlesFG.Emit(P_Dust, vector4 - vector3, 0f);
    				}
    			}
    		}
    		if (node.Y <= start.Y)
    		{
    			Vector2 vector5 = new Vector2(2f, 0f);
    			for (int k = 0; (float)k < Width / 8f; k++)
    			{
    				Vector2 vector6 = new Vector2(Left + 4f + (float)(k * 8), Top - 1f);
    				Vector2 point3 = vector6 + Vector2.UnitY;
    				if (Scene.CollideCheck<Solid>(vector6) && !Scene.CollideCheck<Solid>(point3))
    				{
    					SceneAs<Level>().ParticlesFG.Emit(P_Dust, vector6 + vector5, -(float)Math.PI / 2f);
    					SceneAs<Level>().ParticlesFG.Emit(P_Dust, vector6 - vector5, -(float)Math.PI / 2f);
    				}
    			}
    		}
    		if (node.Y >= start.Y)
    		{
    			Vector2 vector7 = new Vector2(2f, 0f);
    			for (int l = 0; (float)l < Width / 8f; l++)
    			{
    				Vector2 vector8 = new Vector2(Left + 4f + (float)(l * 8), Bottom + 1f);
    				Vector2 point4 = vector8 - Vector2.UnitY * 2f;
    				if (Scene.CollideCheck<Solid>(vector8) && !Scene.CollideCheck<Solid>(point4))
    				{
    					SceneAs<Level>().ParticlesFG.Emit(P_Dust, vector8 + vector7, (float)Math.PI / 2f);
    					SceneAs<Level>().ParticlesFG.Emit(P_Dust, vector8 - vector7, (float)Math.PI / 2f);
    				}
    			}
    		}
    		Collidable = collidable;
    		Audio.Play("event:/game/general/touchswitch_gate_finish", Position);
    		StartShaking(0.2f);
    		// while (icon.Rate > 0f)
    		// {
    		// 	icon.Color = Color.Lerp(activeColor, finishColor, 1f - icon.Rate);
    		// 	icon.Rate -= Engine.DeltaTime * 4f;
    		// 	yield return null;
    		// }
    		// icon.Rate = 0f;
    		// icon.SetAnimationFrame(0);
    		// wiggler.Start();
    		bool collidable2 = Collidable;
    		Collidable = false;
    		// if (!Scene.CollideCheck<Solid>(Center))
    		// {
    		// 	for (int m = 0; m < 32; m++)
    		// 	{
    		// 		float num = Calc.Random.NextFloat((float)Math.PI * 2f);
    		// 		// SceneAs<Level>().ParticlesFG.Emit(TouchSwitch.P_Fire, Position + iconOffset + Calc.AngleToVector(num, 4f), num);
    		// 	}
    		// }
    		Collidable = collidable2;
    	}
    }
}
