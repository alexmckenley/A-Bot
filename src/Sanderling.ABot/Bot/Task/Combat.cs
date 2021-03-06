﻿using BotEngine.Common;
using System.Collections.Generic;
using System.Linq;
using Sanderling.Motor;
using Sanderling.Parse;
using System;
using Sanderling.Interface.MemoryStruct;
using Sanderling.ABot.Parse;
using Bib3;
using WindowsInput.Native;

namespace Sanderling.ABot.Bot.Task
{
	public class CombatTask : IBotTask
	{
		const int TargetCountMax = 3;

		public Bot bot;

		public bool Completed { private set; get; }


		public IEnumerable<IBotTask> Component
		{
			get
			{
				var memoryMeasurementAtTime = bot?.MemoryMeasurementAtTime;
				var memoryMeasurementAccu = bot?.MemoryMeasurementAccu;

				var memoryMeasurement = memoryMeasurementAtTime?.Value;

				if (!memoryMeasurement.ManeuverStartPossible())
					yield break;

				var targetingRangeDistance = (bot?.ConfigSerialAndStruct.Value?.TargetingRangeKM ?? 30) * 1000;

				var listOverviewEntryToAttack =
					bot.SortTargets(memoryMeasurement?.WindowOverview?.FirstOrDefault()?.ListView?.Entry);

				var targetSelected =
					memoryMeasurement?.Target?.FirstOrDefault(target => target?.IsSelected ?? false);

				var shouldAttackTarget =
					listOverviewEntryToAttack?.Any(entry => entry?.MeActiveTarget ?? false) ?? false;

				var setModuleWeapon =
					memoryMeasurementAccu?.ShipUiModule?.Where(module => module?.TooltipLast?.Value?.IsWeapon ?? false);

				var droneListView = memoryMeasurement?.WindowDroneView?.FirstOrDefault()?.ListView;

				var droneGroupWithNameMatchingPattern = new Func<string, DroneViewEntryGroup>(namePattern =>
						droneListView?.Entry?.OfType<DroneViewEntryGroup>()?.FirstOrDefault(group => group?.LabelTextLargest()?.Text?.RegexMatchSuccessIgnoreCase(namePattern) ?? false));

				var droneGroupInBay = droneGroupWithNameMatchingPattern("bay");
				var droneGroupInLocalSpace = droneGroupWithNameMatchingPattern("local space");

				var droneInBayCount = droneGroupInBay?.Caption?.Text?.CountFromDroneGroupCaption();
				var droneInLocalSpaceCount = droneGroupInLocalSpace?.Caption?.Text?.CountFromDroneGroupCaption();

				//	assuming that local space is bottommost group.
				var setDroneInLocalSpace =
					droneListView?.Entry?.OfType<DroneViewEntryItem>()
					?.Where(drone => droneGroupInLocalSpace?.RegionCenter()?.B < drone?.RegionCenter()?.B)
					?.ToArray();

				var droneInLocalSpaceSetStatus =
					setDroneInLocalSpace?.Select(drone => drone?.LabelText?.Select(label => label?.Text?.StatusStringFromDroneEntryText()))?.ConcatNullable()?.WhereNotDefault()?.Distinct()?.ToArray();

				var droneInLocalSpaceIdle =
					droneInLocalSpaceSetStatus?.Any(droneStatus => droneStatus.RegexMatchSuccessIgnoreCase("idle")) ?? false;

				var droneInLocalSpaceReturning =
					droneInLocalSpaceSetStatus?.Any(droneStatus => droneStatus.RegexMatchSuccessIgnoreCase("returning")) ?? false;

				var overviewEntryTarget =
					listOverviewEntryToAttack?.FirstOrDefault();

				var overviewEntryLockTarget =
					listOverviewEntryToAttack?.FirstOrDefault(entry => !((entry?.MeTargeted ?? false) || (entry?.MeTargeting ?? false)));

				var numCurrentTargets = listOverviewEntryToAttack?.Where(entry => ((entry?.MeTargeted ?? false) || (entry?.MeTargeting ?? false))).Count();

				var intelChannel = bot?.ConfigSerialAndStruct.Value?.IntelChannel ?? @"SOUR-Intel";
				var intelCloseFriends =
					memoryMeasurement?.WindowChatChannel
					?.Where(window => window?.Caption?.RegexMatchSuccessIgnoreCase(intelChannel) ?? false)
					?.FirstOrDefault()?.ParticipantView?.Entry
					?.Select(participant => participant?.NameLabel?.Text)
					?.ToArray();

				var listOverviewEntryAllies =
					memoryMeasurement?.WindowOverview?.FirstOrDefault().ListView?.Entry
					?.Where(entry => (entry?.DistanceMax ?? int.MaxValue) < 150000)
					?.Where(entry => entry?.ListBackgroundColor?.Any(bot.IsFriendBackgroundColor) ?? false)
					?.ToArray();

				var listOverviewEntryAlliesCloseFriendsFiltered =
					listOverviewEntryAllies
					?.Where(entry => !(intelCloseFriends?.Contains(entry?.Name) ?? false))
					?.ToArray();

				var beingAttacked = listOverviewEntryToAttack?.Any(entry => entry?.IsAttackingMe ?? false) ?? false;

				// Avoid anom if friend is there
				if (droneInLocalSpaceCount == 0 && (listOverviewEntryAlliesCloseFriendsFiltered?.Length ?? 0) > 0)
				{
					Completed = true;
					yield break;
				}

				if (null != targetSelected)
					if (shouldAttackTarget)
					{
						if (targetSelected.Assigned == null && droneInLocalSpaceCount == 5 && (!targetSelected?.LabelText?.Any(label=> label?.Text?.RegexMatchSuccessIgnoreCase(@"pith\s") ?? false) ?? false))
							yield return new BotTask { Effects = new[] { VirtualKeyCode.VK_F.KeyboardPress() } };
						yield return bot.EnsureIsActive(setModuleWeapon);
					}
					else
						yield return targetSelected.ClickMenuEntryByRegexPattern(bot, "unlock");

				if (listOverviewEntryToAttack?.Length > 0)
				{
					if (0 < droneInBayCount && droneInLocalSpaceCount < 5 && (beingAttacked || ((listOverviewEntryAllies?.Length ?? 0) > 0)))
						yield return droneGroupInBay.ClickMenuEntryByRegexPattern(bot, @"launch");

					if (droneInLocalSpaceIdle && shouldAttackTarget)
						yield return new BotTask { Effects = new[] { VirtualKeyCode.VK_F.KeyboardPress() } };

					if ((memoryMeasurement?.ShipUi?.Indication?.LabelText?.Any(label => label?.Text?.RegexMatchSuccessIgnoreCase(@"wreck\s") ?? false) ?? false) || memoryMeasurement?.ShipUi?.Indication?.ManeuverType != ShipManeuverTypeEnum.Orbit)
						yield return new BotTask() { Effects = new[] {
							// Orbit target
							VirtualKeyCode.VK_W.KeyDown(),
							overviewEntryTarget.MouseClick(BotEngine.Motor.MouseButtonIdEnum.Left),
							VirtualKeyCode.VK_W.KeyUp(),
						} };
				}

				if (null != overviewEntryLockTarget && TargetCountMax > numCurrentTargets && (overviewEntryLockTarget?.DistanceMax ?? 30000) <= targetingRangeDistance)
					yield return new BotTask() { Effects = new[] {
						// Lock Target
						VirtualKeyCode.CONTROL.KeyDown(),
						overviewEntryLockTarget.MouseClick(BotEngine.Motor.MouseButtonIdEnum.Left),
						VirtualKeyCode.CONTROL.KeyUp(),
					} };

				if (listOverviewEntryToAttack?.Length == 0)
					if (droneInLocalSpaceCount > 0)
					{
						if (!droneInLocalSpaceReturning)
						{
							var returnDrones = new[] { VirtualKeyCode.SHIFT, VirtualKeyCode.VK_R };
							yield return new BotTask() { Effects = new[] { returnDrones.KeyboardPressCombined() } };
						}
					}
					else
						Completed = true;
			}
		}

		public IEnumerable<MotionParam> Effects => null;
	}
}
