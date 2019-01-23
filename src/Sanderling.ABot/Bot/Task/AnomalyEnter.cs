using System.Collections.Generic;
using System.Linq;
using Sanderling.Motor;
using Sanderling.Parse;
using BotEngine.Common;
using Sanderling.ABot.Parse;
using System;

namespace Sanderling.ABot.Bot.Task
{
	public class AnomalyEnter : IBotTask
	{
		public const string NoSuitableAnomalyFoundDiagnosticMessage = "no suitable anomaly found. waiting for anomaly to appear.";

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

				var tethering = memoryMeasurement?.ShipUi?.EWarElement?.Any(entry => entry?.EWarType?.RegexMatchSuccessIgnoreCase(@"tethering") ?? false) ?? false;
				var currentShieldPercent = (memoryMeasurement?.ShipUi?.HitpointsAndEnergy?.Shield ?? 0) / 10;
				var currentArmorPercent = (memoryMeasurement?.ShipUi?.HitpointsAndEnergy?.Armor ?? 0) / 10;
				var currentHullPercent = (memoryMeasurement?.ShipUi?.HitpointsAndEnergy?.Struct ?? 0) / 10;

				if (tethering && (currentShieldPercent < 100 || currentArmorPercent < 100 || currentHullPercent < 100))
					yield break;

				var probeScannerWindow = memoryMeasurement?.WindowProbeScanner?.FirstOrDefault();

				var configSeed = bot?.ConfigSerialAndStruct.Value?.RandomSeed ?? 0;
				var minuteSeed = Int32.Parse(DateTime.Now.ToString("mm")) + configSeed;
				var r = new Random(minuteSeed + configSeed);

				var anoms = bot?.ConfigSerialAndStruct.Value?.Anoms ?? @"forsaken rally";
				var warpToWithinKM = bot?.ConfigSerialAndStruct.Value?.WarpToWithinKM ?? @"30";

				// Randomize which anom will be chosen to confuse enemies 
				var scanResultCombatSite =
					probeScannerWindow?.ScanResultView?.Entry
					?.Where(scanResult => scanResult?.CellValueFromColumnHeader("Name")?.RegexMatchSuccessIgnoreCase(anoms) ?? false)
					?.Select(x => new { Number = r.Next(), Item = x })
					?.OrderBy(x => x.Number)
					?.Select(x => x.Item)
					?.FirstOrDefault();

				if (null == scanResultCombatSite)
					yield return new DiagnosticTask
					{
						MessageText = NoSuitableAnomalyFoundDiagnosticMessage,
					};

				if (null != scanResultCombatSite)
				{
					yield return new MenuPathTask
					{
						RootUIElement = scanResultCombatSite,
						Bot = bot,
						ListMenuListPriorityEntryRegexPattern = new[] { new[] { @"warp to within$" }, new[] { $"within {warpToWithinKM} [m|km]" } },
					};
				}

			}
		}

		public IEnumerable<MotionParam> Effects => null;
	}
}
