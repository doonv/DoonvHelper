using System;
using System.Collections.Generic;

namespace Celeste.Mod.DoonvHelper;

public class DoonvHelperMapDataProcessor : EverestMapDataProcessor
{
	private int comfs = 0;

	public override void End()
	{
		Logger.Log(LogLevel.Info, "DoonvHelper", String.Format(
			"Storing Comf Info for {0} Ch. {1} {2}-Side: {3} comfs in level.",
			String.IsNullOrWhiteSpace(this.AreaKey.GetSID()) ? this.AreaKey.GetLevelSet() : this.AreaKey.GetSID(),
			this.AreaKey.ChapterIndex,
			this.AreaKey.Mode == AreaMode.Normal ? "A" : (this.AreaKey.Mode == AreaMode.BSide ? "B" : "C"),
			comfs
		));
		DoonvHelperModule.ComfLevelTotals[new LevelSideID(this.AreaKey)] = comfs;
	}

	public override Dictionary<string, Action<BinaryPacker.Element>> Init()
	{
		return new Dictionary<string, Action<BinaryPacker.Element>> {
				{
                    //! This is dumb but it appears this function doesn't support triggers 
                    "triggers",
					triggerList => {
						foreach (BinaryPacker.Element trigger in triggerList.Children) {
							if (trigger.Name == "DoonvHelper/ComfySpot") {
								comfs++;
							}
						}
					}
				}
			};
	}

	public override void Reset()
	{
		comfs = 0;
	}
}
