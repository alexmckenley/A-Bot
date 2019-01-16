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
	public class OrbitTask : IBotTask
	{
		public Bot bot;

		public IEnumerable<IBotTask> Component
		{
			get
			{
				var orbitKM = bot?.ConfigSerialAndStruct.Value?.OrbitKM ?? @"25";

				var memoryMeasurementAtTime = bot?.MemoryMeasurementAtTime;
				var memoryMeasurementAccu = bot?.MemoryMeasurementAccu;

				var memoryMeasurement = memoryMeasurementAtTime?.Value;

				if (memoryMeasurement?.ShipUi?.Indication?.ManeuverType == ShipManeuverTypeEnum.Warp)
				{
					var subsetModuleAfterburner =
						memoryMeasurementAccu?.ShipUiModule?.Where(module => module?.TooltipLast?.Value?.IsAfterburner ?? false);
					yield return bot.EnsureIsInactive(subsetModuleAfterburner);
					yield break;
				}

				if (!memoryMeasurement.ManeuverStartPossible())
					yield break;

				if (memoryMeasurement?.ShipUi?.Indication?.ManeuverType == ShipManeuverTypeEnum.Orbit)
				{
					var subsetModuleAfterburner =
						memoryMeasurementAccu?.ShipUiModule?.Where(module => module?.TooltipLast?.Value?.IsAfterburner ?? false);
					yield return bot.EnsureIsActive(subsetModuleAfterburner);
					yield break;
				}

				var listOverviewEntriesToAttack =
					bot.SortTargets(memoryMeasurement?.WindowOverview?.FirstOrDefault()?.ListView?.Entry);

				var overviewEntryTarget =
					listOverviewEntriesToAttack?.FirstOrDefault();

				if (overviewEntryTarget != null)
				{
					yield return new MenuPathTask {
						RootUIElement = overviewEntryTarget,
						Bot = bot,
						ListMenuListPriorityEntryRegexPattern = new[] { new[] { @"orbit" }, new[] { $"{orbitKM} [m|km]" } },
					};
				}
			}
		}

		public IEnumerable<MotionParam> Effects => null;
	}
}
