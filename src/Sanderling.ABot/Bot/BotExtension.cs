using Bib3;
using BotEngine.Common;
using BotEngine.Interface;
using Sanderling.ABot.Bot.Task;
using Sanderling.Interface.MemoryStruct;
using Sanderling.Motor;
using Sanderling.Parse;
using System.Collections.Generic;
using System.Linq;

namespace Sanderling.ABot.Bot
{
	static public class BotExtension
	{
		static readonly EWarTypeEnum[][] listEWarPriorityGroup = new[]
		{
			new[] { EWarTypeEnum.ECM },
			new[] { EWarTypeEnum.Web},
			new[] { EWarTypeEnum.WarpDisrupt, EWarTypeEnum.WarpScramble },
		};

		static public int AttackPriorityIndexForOverviewEntryEWar(IEnumerable<EWarTypeEnum> setEWar)
		{
			var setEWarRendered = setEWar?.ToArray();

			return
				listEWarPriorityGroup.FirstIndexOrNull(priorityGroup => priorityGroup.ContainsAny(setEWarRendered)) ??
				(listEWarPriorityGroup.Length + (0 < setEWarRendered?.Length ? 0 : 1));
		}

		static public int AttackPriorityIndex(
			this Bot bot,
			Sanderling.Parse.IOverviewEntry entry) =>
			AttackPriorityIndexForOverviewEntryEWar(bot?.OverviewMemory?.SetEWarTypeFromOverviewEntry(entry));

		static public Sanderling.Parse.IOverviewEntry[] SortTargets(this Bot bot, IEnumerable<Sanderling.Parse.IOverviewEntry> list) => list
			?.Where(entry => entry?.MainIcon?.Color?.IsRed() ?? false)
			?.Where(entry => (entry?.DistanceMax ?? int.MaxValue) < 100000)
			?.OrderBy(entry => bot.AttackPriorityIndex(entry))
			?.OrderBy(entry => entry?.Name?.RegexMatchSuccessIgnoreCase(@"battery|tower|sentry|web|strain|splinter|render|raider|friar|reaver|mortifier")) //Frigate
			?.OrderBy(entry => entry?.Name?.RegexMatchSuccessIgnoreCase(@"coreli|centi|alvi|pithi|corpii|gistii|cleric|engraver")) //Frigate
			?.OrderBy(entry => entry?.Name?.RegexMatchSuccessIgnoreCase(@"corelior|centior|alvior|pithior|corpior|gistior")) //Destroyer
			?.OrderBy(entry => entry?.Name?.RegexMatchSuccessIgnoreCase(@"corelum|centum|alvum|pithum|corpum|gistum|prophet")) //Cruiser
			?.OrderBy(entry => entry?.Name?.RegexMatchSuccessIgnoreCase(@"corelatis|centatis|alvatis|pithatis|copatis|gistatis|apostle")) //Battlecruiser
			?.OrderBy(entry => entry?.Name?.RegexMatchSuccessIgnoreCase(@"core\s|centus|alvus|pith\s|corpus|gist\s")) //Battleship
			?.ThenBy(entry => entry?.DistanceMax ?? int.MaxValue)
			?.ToArray();

		static public bool IsFriendBackgroundColor(this Bot bot, ColorORGB color) =>
			(color.OMilli == 500 && color.RMilli == 0 && color.GMilli == 150 && color.BMilli == 600) || (color.OMilli == 500 && color.RMilli == 100 && color.GMilli == 600 && color.BMilli == 100);

		static public bool IsEnemyBackgroundColor(this Bot bot, ColorORGB color) =>
			(color.OMilli == 500 && color.RMilli == 750 && color.GMilli == 0 && color.BMilli == 600);

		static public bool ShouldBeIncludedInStepOutput(this IBotTask task) =>
			(task?.ContainsEffect() ?? false) || task is DiagnosticTask;

		static public bool LastContainsEffect(this IEnumerable<IBotTask> listTask) =>
			listTask?.LastOrDefault()?.ContainsEffect() ?? false;

		static public IEnumerable<MotionParam> ApplicableEffects(this IBotTask task) =>
			task?.Effects?.WhereNotDefault();

		static public bool ContainsEffect(this IBotTask task) =>
			0 < task?.ApplicableEffects()?.Count();

		static public IEnumerable<IBotTask[]> TakeSubsequenceWhileUnwantedInferenceRuledOut(this IEnumerable<IBotTask[]> listTaskPath) =>
			listTaskPath
			?.EnumerateSubsequencesStartingWithFirstElement()
			?.OrderBy(subsequenceTaskPath => 1 == subsequenceTaskPath?.Count(BotExtension.LastContainsEffect))
			?.LastOrDefault();

		static public IUIElementText TitleElementText(this IModuleButtonTooltip tooltip)
		{
			var tooltipHorizontalCenter = tooltip?.RegionCenter()?.A;

			var setLabelIntersectingHorizontalCenter =
				tooltip?.LabelText
				?.Where(label => label?.Region.Min0 < tooltipHorizontalCenter && tooltipHorizontalCenter < label?.Region.Max0);

			return
				setLabelIntersectingHorizontalCenter
				?.OrderByCenterVerticalDown()?.FirstOrDefault();
		}

		static public bool ShouldBeActivePermanent(this Accumulation.IShipUiModule module, Bot bot) =>
			new[]
			{
				module?.TooltipLast?.Value?.IsHardener,
				bot?.ConfigSerialAndStruct.Value?.ModuleActivePermanentSetTitlePattern
					?.Any(activePermanentTitlePattern => module?.TooltipLast?.Value?.TitleElementText()?.Text?.RegexMatchSuccessIgnoreCase(activePermanentTitlePattern) ?? false),
			}
			.Any(sufficientCondition => sufficientCondition ?? false);
	}
}
