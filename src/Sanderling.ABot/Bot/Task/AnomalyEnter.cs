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

		static public bool AnomalySuitableGeneral(Interface.MemoryStruct.IListEntry scanResult) =>
			scanResult?.CellValueFromColumnHeader("Name")?.RegexMatchSuccessIgnoreCase("forsaken rally") ?? false;

		public IEnumerable<IBotTask> Component
		{
			get
			{
				var memoryMeasurementAtTime = bot?.MemoryMeasurementAtTime;
				var memoryMeasurementAccu = bot?.MemoryMeasurementAccu;

				var memoryMeasurement = memoryMeasurementAtTime?.Value;

				if (!memoryMeasurement.ManeuverStartPossible())
					yield break;

				var probeScannerWindow = memoryMeasurement?.WindowProbeScanner?.FirstOrDefault();

				var seed = Int32.Parse(DateTime.Now.ToString("mm"));
				var r = new Random(seed);

				// Randomize which anom will be chosen to confuse bad guys
				var scanResultCombatSite =
					probeScannerWindow?.ScanResultView?.Entry
					?.Where(AnomalySuitableGeneral)
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
					yield return new MenuPathTask
					{
						RootUIElement = scanResultCombatSite,
						Bot = bot,
						ListMenuListPriorityEntryRegexPattern = new[] { new[] { @"warp to within$" }, new[] { @"within 50 km" } },
					};
			}
		}

		public IEnumerable<MotionParam> Effects => null;
	}
}
