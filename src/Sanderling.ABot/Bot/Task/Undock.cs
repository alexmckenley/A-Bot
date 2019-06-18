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

		public IEnumerable<IBotTask> Component
		{
			get {
				var memoryMeasurementAtTime = bot?.MemoryMeasurementAtTime;
				var memoryMeasurement = memoryMeasurementAtTime?.Value;

				if (memoryMeasurement?.IsUnDocking ?? false)
					yield break;

				if (!(memoryMeasurement?.IsDocked ?? false))
					yield break;

				if (bot.saveShipCooldown > DateTime.UtcNow)
					yield break;

				var undockButton = memoryMeasurement?.WindowStation?.FirstOrDefault()
				?.ButtonText?.FirstOrDefault(entry => entry?.Text?.RegexMatchSuccessIgnoreCase(@"\Sundock") ?? false);

				if (undockButton == null)
				{
					yield return new DiagnosticTask
					{
						MessageText = @"Unable to find Undock button",
					};
					yield break;
				}

				yield return new BotTask
				{
					Effects = new[] {
					undockButton?.MouseClick(BotEngine.Motor.MouseButtonIdEnum.Left) }
				};
			}
		}

		public IEnumerable<MotionParam> Effects => null;
	}
}
