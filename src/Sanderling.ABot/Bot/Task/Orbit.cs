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
				var celestialOrbitIncludes = "wreck";
				var celestialOrbitDistance = "25 km";

				var memoryMeasurementAtTime = bot?.MemoryMeasurementAtTime;
				var memoryMeasurementAccu = bot?.MemoryMeasurementAccu;

				var memoryMeasurement = memoryMeasurementAtTime?.Value;

				if (!memoryMeasurement.ManeuverStartPossible())
					yield break;

				if (memoryMeasurement.ShipUi.Indication.ManeuverType == ShipManeuverTypeEnum.Orbit)
				{
					var subsetModuleAfterburner =
						memoryMeasurementAccu?.ShipUiModule?.Where(module => module?.TooltipLast?.Value?.IsAfterburner ?? false);
					yield return bot.EnsureIsActive(subsetModuleAfterburner);
					yield break;
				}

				var celestialOrbitEntries =
					memoryMeasurement?.WindowOverview?.FirstOrDefault()?.ListView?.Entry
					?.Where(entry => entry?.Name?.RegexMatchSuccessIgnoreCase(celestialOrbitIncludes) ?? false)
					?.Where(entry => (entry?.DistanceMax ?? int.MaxValue) < 100000)
					?.OrderBy(entry => entry?.DistanceMax ?? int.MaxValue)
					?.ToArray();

				// Only orbit if exactly one wreck
				var celestialOrbitEntry = ((celestialOrbitEntries?.Length ?? 0) == 1) ? celestialOrbitEntries?.FirstOrDefault() : null;

				var listOverviewEntriesToAttack =
					bot.SortTargets(memoryMeasurement?.WindowOverview?.FirstOrDefault()?.ListView?.Entry);

				var overviewEntryTarget =
					listOverviewEntriesToAttack?.FirstOrDefault();

				if (celestialOrbitEntry != null || overviewEntryTarget != null)
				{
					yield return new MenuPathTask {
						RootUIElement = celestialOrbitEntry ?? overviewEntryTarget,
						Bot = bot,
						ListMenuListPriorityEntryRegexPattern = new[] { new[] { @"orbit" }, new[] { celestialOrbitDistance } },
					};
				}
			}
		}

		public IEnumerable<MotionParam> Effects => null;
	}
}
