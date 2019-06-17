using System;
using BotEngine.Common;
using System.Collections.Generic;
using System.Linq;
using Sanderling.Motor;
using Sanderling.Parse;

namespace Sanderling.ABot.Bot.Task
{
	public class UndockTask : IBotTask
	{
		public Bot bot;

		public IEnumerable<IBotTask> Component => null;

		public IEnumerable<MotionParam> Effects
		{
			get
			{
				var memoryMeasurementAtTime = bot?.MemoryMeasurementAtTime;
				var memoryMeasurement = memoryMeasurementAtTime?.Value;

				if (memoryMeasurement?.IsUnDocking ?? false)
					yield break;

				if (!(memoryMeasurement?.IsDocked ?? false))
					yield break;

				if (bot.saveShipCooldown > DateTime.UtcNow)
					yield break;

				yield return memoryMeasurement?.WindowStation?.FirstOrDefault()
					?.ButtonText?.FirstOrDefault(entry => entry?.Text?.RegexMatchSuccessIgnoreCase(@"\Sundock") ?? false)
					?.MouseClick(BotEngine.Motor.MouseButtonIdEnum.Left);
			}
		}
	}
}
