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

				var celestialOrbitIncludes = "broken|pirate gate|wreck";
				var celestialOrbitDistance = "30 km";

				var celestialOrbitEntry =
					memoryMeasurement?.WindowOverview?.FirstOrDefault()?.ListView?.Entry
					?.Where(entry => entry?.Name?.RegexMatchSuccessIgnoreCase(celestialOrbitIncludes) ?? false)
					?.OrderBy(entry => bot.AttackPriorityIndex(entry))
					?.ThenBy(entry => entry?.DistanceMax ?? int.MaxValue)
					?.ToArray()
					?.FirstOrDefault();

				var listOverviewEntryToAttack =
					memoryMeasurement?.WindowOverview?.FirstOrDefault()?.ListView?.Entry?.Where(entry => entry?.MainIcon?.Color?.IsRed() ?? false)
					?.OrderBy(entry => bot.AttackPriorityIndex(entry))
					?.ThenBy(entry => entry?.DistanceMax ?? int.MaxValue)
					?.ToArray();

				var overviewEntryLockTarget =
					listOverviewEntryToAttack?.FirstOrDefault(entry => !((entry?.MeTargeted ?? false) || (entry?.MeTargeting ?? false)));

				if (celestialOrbitEntry != null || overviewEntryLockTarget != null)
				{
					yield return new MenuPathTask {
						RootUIElement = celestialOrbitEntry ?? overviewEntryLockTarget,
						Bot = bot,
						ListMenuListPriorityEntryRegexPattern = new[] { new[] { @"orbit" }, new[] { celestialOrbitDistance } },
					};
				}
			}
		}

		public IEnumerable<MotionParam> Effects => null;
	}
}
