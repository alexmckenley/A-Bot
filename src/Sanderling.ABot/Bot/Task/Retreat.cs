using BotEngine.Common;
using System.Collections.Generic;
using System.Linq;
using Sanderling.Motor;
using Sanderling.ABot.Parse;
using Sanderling.Interface.MemoryStruct;
using Sanderling.Parse;
using System;
using Bib3;
using WindowsInput.Native;

namespace Sanderling.ABot.Bot.Task
{
	public class RetreatTask : IBotTask
	{
		public Bot Bot;

		public IEnumerable<IBotTask> Component
		{
			get
			{
				var memoryMeasurement = Bot?.MemoryMeasurementAtTime?.Value;
				var memoryMeasurementAccu = Bot?.MemoryMeasurementAccu;

				if (!memoryMeasurement.ManeuverStartPossible())
					yield break;

				var retreatBookmark = Bot?.ConfigSerialAndStruct.Value?.RetreatBookmark;

				// Disable Afterburner
				var subsetModuleAfterburner =
					memoryMeasurementAccu?.ShipUiModule?.Where(module => module?.TooltipLast?.Value?.IsAfterburner ?? false);
				yield return Bot.EnsureIsInactive(subsetModuleAfterburner);

				var droneListView = memoryMeasurement?.WindowDroneView?.FirstOrDefault()?.ListView;
				var droneGroupWithNameMatchingPattern = new Func<string, DroneViewEntryGroup>(namePattern =>
						droneListView?.Entry?.OfType<DroneViewEntryGroup>()?.FirstOrDefault(group => group?.LabelTextLargest()?.Text?.RegexMatchSuccessIgnoreCase(namePattern) ?? false));
				var droneGroupInLocalSpace = droneGroupWithNameMatchingPattern("local space");
				var droneInLocalSpaceCount = droneGroupInLocalSpace?.Caption?.Text?.CountFromDroneGroupCaption();
				var setDroneInLocalSpace =
					droneListView?.Entry?.OfType<DroneViewEntryItem>()
					?.Where(drone => droneGroupInLocalSpace?.RegionCenter()?.B < drone?.RegionCenter()?.B)
					?.ToArray();
				var droneInLocalSpaceSetStatus =
					setDroneInLocalSpace?.Select(drone => drone?.LabelText?.Select(label => label?.Text?.StatusStringFromDroneEntryText()))?.ConcatNullable()?.WhereNotDefault()?.Distinct()?.ToArray();
				var droneInLocalSpaceReturning =
					droneInLocalSpaceSetStatus?.Any(droneStatus => droneStatus.RegexMatchSuccessIgnoreCase("returning")) ?? false;

				var retreatAction = droneInLocalSpaceCount > 0 ? @"align to" : @"^dock";
				// Recall drones before retreating
				if (droneInLocalSpaceCount > 0 && !droneInLocalSpaceReturning)
				{
					var returnDrones = new[] { VirtualKeyCode.SHIFT, VirtualKeyCode.VK_R };
					yield return new BotTask() { Effects = new[] { returnDrones.KeyboardPressCombined() } };
				}

				if (droneInLocalSpaceCount > 0 && (memoryMeasurement?.ShipUi?.Indication?.LabelText?.Any(label => label?.Text == @"Aligning") ?? false))
					yield break;

				yield return new MenuPathTask
				{
					RootUIElement = memoryMeasurement?.InfoPanelCurrentSystem?.ListSurroundingsButton,
					Bot = Bot,
					ListMenuListPriorityEntryRegexPattern = new[] { new[] { retreatBookmark }, new[] { retreatAction, ParseStatic.MenuEntryWarpToAtLeafRegexPattern } },
				};
			}
		}

		public IEnumerable<MotionParam> Effects => null;
	}
}
