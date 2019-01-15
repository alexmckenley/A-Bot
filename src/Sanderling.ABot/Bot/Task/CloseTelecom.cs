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
	public class CloseTelecomTask : IBotTask
	{
		public Bot bot;

		public IEnumerable<IBotTask> Component
		{
			get
			{
				var memoryMeasurementAtTime = bot?.MemoryMeasurementAtTime;

				var memoryMeasurement = memoryMeasurementAtTime?.Value;

				var windowTelecomCloseButton = memoryMeasurement?.WindowTelecom?.FirstOrDefault()?.ButtonText?.FirstOrDefault(entry => entry?.Text?.RegexMatchSuccessIgnoreCase(@"close") ?? false);

				if (windowTelecomCloseButton != null)
					yield return new BotTask() { Effects = new[] { windowTelecomCloseButton.MouseClick(BotEngine.Motor.MouseButtonIdEnum.Left) } };
			}
		}

		public IEnumerable<MotionParam> Effects => null;
	}
}
