using BotEngine.Common;
using System.Collections.Generic;
using System.Linq;
using Sanderling.Motor;
using Sanderling.Parse;
using System;
using Sanderling.Interface.MemoryStruct;
using Sanderling.ABot.Parse;
using Bib3;
using System.Text.RegularExpressions;
using System.Globalization;
using WindowsInput.Native;

namespace Sanderling.ABot.Bot.Task
{
	public class IntelTask : IBotTask
	{
		public Bot bot;

		public IEnumerable<IBotTask> Component
		{
			get
			{
				var memoryMeasurementAtTime = bot?.MemoryMeasurementAtTime;
				var memoryMeasurement = memoryMeasurementAtTime?.Value;
				var intelChannel = bot?.ConfigSerialAndStruct.Value?.IntelChannel ?? @"SOUR-Intel";
				var intelChatWindowCandidate =
					memoryMeasurement?.WindowChatChannel
					?.Where(window => window?.Caption?.RegexMatchSuccessIgnoreCase(intelChannel) ?? false)
					?.ToArray();

				if ((intelChatWindowCandidate?.Length ?? 0) > 1)
				{
					yield return new DiagnosticTask
					{
						MessageText = $"Multiple intel channel windows found for: {intelChannel}",
					};
					yield break;
				}
				
				var intelChatWindow = intelChatWindowCandidate?.FirstOrDefault();

				if (intelChatWindow == null) {
					yield return new DiagnosticTask
					{
						MessageText = $"Unable to find intel chat window for: {intelChannel}",
					};
					yield break;
				}

				var lastMessage = intelChatWindow?.MessageView?.Entry?.LastOrDefault()?.LabelText?.LastOrDefault()?.Text;

				var paused = lastMessage.RegexMatchSuccessIgnoreCase(@"--paused--");
				if (paused)
					yield break;

				Match match = Regex.Match(lastMessage, @"--(.*)--");
				var lastDateStr = match?.Groups?[1]?.Value;

				DateTime parsedDate;
				var success = DateTime.TryParse(lastDateStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out parsedDate);
				if (!success)
				{
					parsedDate = DateTime.MinValue;
				}
				parsedDate = parsedDate.ToUniversalTime();

				// Update saveShipCooldown if necessary
				if (parsedDate > DateTime.UtcNow && bot.saveShipCooldown < parsedDate)
				{
					bot.saveShipCooldown = parsedDate;
					bot.cooldownReason = @"intel channel";
				}

				// Report intel if necessary
				if (bot.saveShipCooldown > parsedDate.AddMinutes(1))
					yield return new BotTask() { Effects = new[] {
						intelChatWindow.MessageInput.MouseClick(BotEngine.Motor.MouseButtonIdEnum.Left),
						("--" + bot.saveShipCooldown.ToString("o") + "-- " + bot.cooldownReason).TextEntry(),
						VirtualKeyCode.RETURN.KeyboardPress(),
						memoryMeasurement?.InfoPanelRoute?.ExpandToggleButton?.MouseClick(BotEngine.Motor.MouseButtonIdEnum.Left) } };
			}
		}

		public IEnumerable<MotionParam> Effects => null;
	}
}
