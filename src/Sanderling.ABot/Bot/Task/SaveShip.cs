using System.Collections.Generic;
using System.Linq;
using Sanderling.Motor;
using BotEngine.Common;
using Sanderling.ABot.Parse;
using Sanderling.Interface.MemoryStruct;
using System;

namespace Sanderling.ABot.Bot.Task
{
	public class SaveShipTask : IBotTask
	{
		public const string CannotIdentifyLocalChatWindowDiagnosticText = "can not identify local chat window.";
		public const string LocalChatWindowNotFoundDiagnosticText = "local chat window not found.";

		public Bot Bot;

		public bool AllowRoam;

		public bool AllowAnomalyEnter;

		const int AllowRoamSessionDurationMin = 60 * 7;

		const int AllowAnomalyEnterSessionDurationMin = AllowRoamSessionDurationMin + 60 * 7;

		static public bool ChatIsClean(Bot bot, WindowChatChannel chatWindow)
		{
			if (null == chatWindow)
				return false;

			if (chatWindow?.ParticipantView?.Scroll?.IsScrollable() ?? true)
				return false;

			var listParticipantNeutralOrEnemy =
				chatWindow?.ParticipantView?.Entry
				?.Where(participant => !(bot.ConfigSerialAndStruct.Value?.CloakyCampers?.Contains(participant?.NameLabel?.Text) ?? false))
				?.Where(participant => participant.IsNeutralOrEnemy())
				?.ToArray();

			// Report intel if number of enemies in local is 4+
			var numNeutralOrEnemy = listParticipantNeutralOrEnemy?.Length ?? 0;
			if (numNeutralOrEnemy >= 5)
			{
				var gangCooldownMinutes = bot?.ConfigSerialAndStruct.Value?.GangCooldownMinutes ?? 20;
				var cooldownUntil = DateTime.UtcNow.AddMinutes(gangCooldownMinutes);
				if (cooldownUntil > bot.saveShipCooldown.AddMinutes(1))
				{
					bot.saveShipCooldown = cooldownUntil;
					bot.cooldownReason = $"{numNeutralOrEnemy - 1} man gang detected! Dock up.☢";
				}
			}

			//	we expect own char to show up there as well so there has to be one participant with neutral or enemy flag.
			return numNeutralOrEnemy == 1;
		}

		static public bool OverviewIsClean(Bot bot, IEnumerable<Sanderling.Parse.IOverviewEntry> list)
		{
			if (null == list)
				return false;

			var avoidShipType = bot?.ConfigSerialAndStruct.Value?.AvoidShipType;

			if (null == avoidShipType)
				return true;

			var foundShips = list
				?.Where(entry => !(entry?.ListBackgroundColor?.Any(bot.IsFriendBackgroundColor) ?? false))
				?.Where(entry => (entry?.Type?.RegexMatchSuccessIgnoreCase(avoidShipType) ?? false) || (entry?.ListBackgroundColor?.Any(bot.IsEnemyBackgroundColor) ?? false))
				?.ToArray();

			if ((foundShips?.Length ?? 0) > 0)
			{
				var enemySightedCooldownMinutes = bot?.ConfigSerialAndStruct.Value?.EnemySightedCooldownMinutes ?? 20;
				var cooldownUntil = DateTime.UtcNow.AddMinutes(enemySightedCooldownMinutes);
				if (cooldownUntil > bot.saveShipCooldown.AddMinutes(1))
				{
					bot.saveShipCooldown = cooldownUntil;
					bot.cooldownReason = $"Enemy sighted: {foundShips?.FirstOrDefault()?.Name}({foundShips?.FirstOrDefault()?.Type})";
				}
				return false;
			}

			return true;
		}

		public IEnumerable<IBotTask> Component
		{
			get
			{
				var memoryMeasurement = Bot?.MemoryMeasurementAtTime?.Value;

				var shieldRetreatPercent = Bot?.ConfigSerialAndStruct.Value?.ShieldRetreatPercent;
				var armorRetreatPercent = Bot?.ConfigSerialAndStruct.Value?.ArmorRetreatPercent;
				var hullRetreatPercent = Bot?.ConfigSerialAndStruct.Value?.HullRetreatPercent;

				var charIsLocatedInHighsec = 500 < memoryMeasurement?.InfoPanelCurrentSystem?.SecurityLevelMilli;

				var setLocalChatWindowCandidate =
					memoryMeasurement?.WindowChatChannel
					?.Where(window => window?.Caption?.RegexMatchSuccessIgnoreCase(@"local") ?? false)
					?.ToArray();

				if (1 < setLocalChatWindowCandidate?.Length)
					yield return new DiagnosticTask
					{
						MessageText = CannotIdentifyLocalChatWindowDiagnosticText,
					};

				var localChatWindow = setLocalChatWindowCandidate?.FirstOrDefault();

				if (null == localChatWindow)
					yield return new DiagnosticTask
					{
						MessageText = LocalChatWindowNotFoundDiagnosticText,
					};

				var now = DateTime.UtcNow;
				var startWindow = new DateTime(now.Year, now.Month, now.Day, 10, 45, 0, DateTimeKind.Utc);
				var endWindow = new DateTime(now.Year, now.Month, now.Day, 11, 15, 0, DateTimeKind.Utc);
				var impendingDowntime = now >= startWindow && now < endWindow;

				if (impendingDowntime)
					yield return new DiagnosticTask
					{
						MessageText = "Impending Downtime!", 
					};

				var sessionDurationSufficient = AllowRoamSessionDurationMin <= memoryMeasurement?.SessionDurationRemaining;

				var currentShieldPercent = (memoryMeasurement?.ShipUi?.HitpointsAndEnergy?.Shield ?? 0) / 10;
				var currentArmorPercent = (memoryMeasurement?.ShipUi?.HitpointsAndEnergy?.Armor ?? 0) / 10;
				var currentHullPercent = (memoryMeasurement?.ShipUi?.HitpointsAndEnergy?.Struct ?? 0) / 10;
				var safeShield = currentShieldPercent > (shieldRetreatPercent ?? -1);
				var safeArmor = currentArmorPercent > (armorRetreatPercent ?? -1);
				var safeHull = currentHullPercent > (hullRetreatPercent ?? -1);

				var overview =
					memoryMeasurement?.WindowOverview?.FirstOrDefault()?.ListView?.Entry;

				var shipHealthOK = (memoryMeasurement?.IsDocked ?? false) || (safeShield && safeArmor && safeHull);
				var overviewIsClean = (memoryMeasurement?.IsDocked ?? false) || OverviewIsClean(Bot, overview);
				var chatIsClean = charIsLocatedInHighsec || ChatIsClean(Bot, localChatWindow);

				var intelChannel = Bot?.ConfigSerialAndStruct.Value?.IntelChannel ?? @"SOUR-Intel";

				var paused =
					memoryMeasurement?.WindowChatChannel
					?.Where(window => window?.Caption?.RegexMatchSuccessIgnoreCase(intelChannel) ?? false)?.FirstOrDefault()
					?.MessageView?.Entry?.LastOrDefault()?.LabelText?.LastOrDefault()?.Text.RegexMatchSuccessIgnoreCase(@"--paused--") ?? false; 
				if (paused)
					yield return new DiagnosticTask
					{
						MessageText = $"Docking, paused.",
					};

				var coolingDown = Bot.saveShipCooldown > DateTime.UtcNow;
				if (coolingDown)
					yield return new DiagnosticTask
					{
						MessageText = $"Cooling down due to: {Bot.cooldownReason}",
					};

				if (!paused && !coolingDown && !impendingDowntime && sessionDurationSufficient && shipHealthOK && chatIsClean && overviewIsClean)
				{
					AllowRoam = true;
					AllowAnomalyEnter = AllowAnomalyEnterSessionDurationMin <= memoryMeasurement?.SessionDurationRemaining;
					yield break;
				}

				yield return new RetreatTask
				{
					Bot = Bot,
					EmergencyRetreat = !overviewIsClean,
				};
			}
		}

		public IEnumerable<MotionParam> Effects => null;
	}
}
