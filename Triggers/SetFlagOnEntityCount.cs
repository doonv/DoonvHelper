using System;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using System.Linq;
using Monocle;

namespace Celeste.Mod.DoonvHelper.Entities;

[CustomEntity("DoonvHelper/SetFlagOnEntityCount")]
public class SetFlagOnEntityCount : Trigger
{
	public enum OperatorType
	{
		EqualTo,
		GreaterThan,
		LessThan,
	}
	public enum CheckDuring
	{
		Always,
		OnEnter,
		OnStay,
		OnLeave,
	}
	public OperatorType Operator;
	public CheckDuring CheckOn;
	public string Flag;
	public Type TargetType;
	public int TargetCount;

	public bool Condition
	{
		get
		{
			int count = Scene.Entities.Count((Entity entity) => entity.GetType() == TargetType);
			switch (Operator)
			{
				case OperatorType.EqualTo: return count == TargetCount;
				case OperatorType.GreaterThan: return count > TargetCount;
				case OperatorType.LessThan: return count < TargetCount;
			}
			return false;
		}
	}

	private Session session;

	public SetFlagOnEntityCount(EntityData data, Vector2 offset) : base(data, offset)
	{
		if (DoonvHelperModule.FrostHelperImports.EntityNameToType is null)
			Logger.LogDetailed(LogLevel.Error, "DoonvHelper", "FrostHelper `EntityNameToType` ModInterop function not found!");
		this.Flag = data.Attr("flag", "");
		this.TargetCount = data.Int("count", 0);
		this.Operator = data.Enum<OperatorType>("operator", OperatorType.EqualTo);
		this.TargetType = DoonvHelperModule.FrostHelperImports.EntityNameToType(data.Attr("entityIDs", ""));
		this.CheckOn = data.Enum<CheckDuring>("checkOn", CheckDuring.OnEnter);
	}

	public override void Added(Scene scene)
	{
		session = (scene as Level).Session;
		base.Added(scene);
	}

	private void checkAndSetFlag(CheckDuring check)
	{
		if (check != CheckOn || Condition == false) return;
		session.SetFlag(Flag);
	}

	public override void Update()
	{
		checkAndSetFlag(CheckDuring.Always);
		base.Update();
	}
	public override void OnEnter(Player player)
	{
		checkAndSetFlag(CheckDuring.OnEnter);
		base.OnEnter(player);
	}
	public override void OnStay(Player player)
	{
		checkAndSetFlag(CheckDuring.OnStay);
		base.OnStay(player);
	}
	public override void OnLeave(Player player)
	{
		checkAndSetFlag(CheckDuring.OnLeave);
		base.OnLeave(player);
	}
}
