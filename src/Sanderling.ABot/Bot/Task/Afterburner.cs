using BotEngine.Common;
using System.Collections.Generic;
using System.Linq;
using Sanderling.Motor;
using Sanderling.Parse;
using System;
using Sanderling.Interface.MemoryStruct;
using Sanderling.ABot.Parse;
using Bib3;

namespace Sanderling.ABot.Bot.Task
{
	public class AfterburnerTask : IBotTask
	{
		public Bot bot;

		public IEnumerable<IBotTask> Component
		{
			get
			{
				var memoryMeasurementAtTime = bot?.MemoryMeasurementAtTime;
				var memoryMeasurementAccu = bot?.MemoryMeasurementAccu;

				var memoryMeasurement = memoryMeasurementAtTime?.Value;

				var afterburnerModules =
					memoryMeasurementAccu?.ShipUiModule?.Where(module => module?.TooltipLast?.Value?.IsAfterburner ?? false);

				if (memoryMeasurement?.ShipUi?.Indication?.ManeuverType == ShipManeuverTypeEnum.Warp || (memoryMeasurement?.ShipUi?.Indication?.LabelText?.Any(label => label?.Text == @"Aligning") ?? false))
				{
					yield return bot.EnsureIsInactive(afterburnerModules);
					yield break;
				}

				if (memoryMeasurement?.ShipUi?.Indication?.ManeuverType == ShipManeuverTypeEnum.Orbit)
				{
					yield return bot.EnsureIsActive(afterburnerModules);
					yield break;
				}
			}
		}

		public IEnumerable<MotionParam> Effects => null;
	}
}
