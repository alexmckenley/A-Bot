using BotEngine.Common;
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
		const int TargetCountMax = 2;

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

				if (null != targetSelected)
					if (shouldAttackTarget)
					{
						if (targetSelected.Assigned == null && droneInLocalSpaceCount == 5)
							yield return new BotTask { Effects = new[] { VirtualKeyCode.VK_F.KeyboardPress() } };
						yield return bot.EnsureIsActive(setModuleWeapon);
					}
					else
						yield return targetSelected.ClickMenuEntryByRegexPattern(bot, "unlock");

				if (listOverviewEntryToAttack.Length > 0)
				{
					if (0 < droneInBayCount && droneInLocalSpaceCount < 5)
						yield return droneGroupInBay.ClickMenuEntryByRegexPattern(bot, @"launch");

					if (droneInLocalSpaceIdle && shouldAttackTarget)
					{
						yield return new BotTask { Effects = new[] { VirtualKeyCode.VK_F.KeyboardPress() } };
					}
				}

				var overviewEntryLockTarget =
					listOverviewEntryToAttack?.FirstOrDefault(entry => !((entry?.MeTargeted ?? false) || (entry?.MeTargeting ?? false)));

				var numCurrentTargets = listOverviewEntryToAttack?.Where(entry => ((entry?.MeTargeted ?? false) || (entry?.MeTargeting ?? false))).Count();

				if (null != overviewEntryLockTarget && !(TargetCountMax <= numCurrentTargets))
					yield return new BotTask() { Effects = new[] {
						// Lock Target
						VirtualKeyCode.CONTROL.KeyDown(),
						overviewEntryLockTarget.MouseClick(BotEngine.Motor.MouseButtonIdEnum.Left),
						VirtualKeyCode.CONTROL.KeyUp(),
					} };

				if (!(0 < listOverviewEntryToAttack?.Length))
					if (0 < droneInLocalSpaceCount)
					{
						var returnDrones = new[] { VirtualKeyCode.SHIFT, VirtualKeyCode.VK_R };
						yield return new BotTask() { Effects = new[] { returnDrones.KeyboardPressCombined() } };
					}
					else
						Completed = true;
			}
		}

		public IEnumerable<MotionParam> Effects => null;
	}
}
