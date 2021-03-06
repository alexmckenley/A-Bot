﻿using Bib3;
using Sanderling.Parse;
using BotEngine.Interface;
using System.Linq;
using System.Collections.Generic;
using System;
using Sanderling.Motor;
using Sanderling.ABot.Bot.Task;
using Sanderling.ABot.Bot.Memory;
using Sanderling.ABot.Serialization;

namespace Sanderling.ABot.Bot
{
	public class Bot
	{
		static public readonly Func<Int64> GetTimeMilli = Bib3.Glob.StopwatchZaitMiliSictInt;

		public BotStepInput StepLastInput { private set; get; }

		public PropertyGenTimespanInt64<BotStepResult> StepLastResultTimespan { private set; get; }

		public BotStepResult StepLastResult { private set; get; }

		int motionId;

		int stepIndex;

		public DateTime saveShipCooldown = DateTime.MinValue;

		public string cooldownReason = @"";

		public FromProcessMeasurement<IMemoryMeasurement> MemoryMeasurementAtTime { private set; get; }

		readonly public Accumulator.MemoryMeasurementAccumulator MemoryMeasurementAccu = new Accumulator.MemoryMeasurementAccumulator();

		readonly public OverviewMemory OverviewMemory = new OverviewMemory();

		readonly IDictionary<Int64, int> MouseClickLastStepIndexFromUIElementId = new Dictionary<Int64, int>();

		readonly IDictionary<Accumulation.IShipUiModule, int> ToggleLastStepIndexFromModule = new Dictionary<Accumulation.IShipUiModule, int>();

		public KeyValuePair<Deserialization, Config> ConfigSerialAndStruct { private set; get; }

		public Int64? MouseClickLastAgeStepCountFromUIElement(Interface.MemoryStruct.IUIElement uiElement)
		{
			if (null == uiElement)
				return null;

			var interactionLastStepIndex = MouseClickLastStepIndexFromUIElementId?.TryGetValueNullable(uiElement.Id);

			return stepIndex - interactionLastStepIndex;
		}

		public Int64? ToggleLastAgeStepCountFromModule(Accumulation.IShipUiModule module) =>
			module == null ? null :
			stepIndex - ToggleLastStepIndexFromModule?.TryGetValueNullable(module);

		IEnumerable<IBotTask[]> StepOutputListTaskPath() =>
			((IBotTask)new BotTask { Component = RootTaskListComponent() })
			?.EnumeratePathToNodeFromTreeDFirst(node => node?.Component)
			?.Where(taskPath => (taskPath?.LastOrDefault()).ShouldBeIncludedInStepOutput())
			?.TakeSubsequenceWhileUnwantedInferenceRuledOut();

		void MemorizeStepInput(BotStepInput input)
		{
			ConfigSerialAndStruct = (input?.ConfigSerial?.String).DeserializeIfDifferent(ConfigSerialAndStruct);

			MemoryMeasurementAtTime = input?.FromProcessMemoryMeasurement?.MapValue(measurement => measurement?.Parse());

			MemoryMeasurementAccu.Accumulate(MemoryMeasurementAtTime);

			OverviewMemory.Aggregate(MemoryMeasurementAtTime);
		}

		void MemorizeStepResult(BotStepResult stepResult)
		{
			var setMotionMouseWaypointUIElement =
				stepResult?.ListMotion
				?.Select(motion => motion?.MotionParam)
				?.Where(motionParam => 0 < motionParam?.MouseButton?.Count())
				?.Select(motionParam => motionParam?.MouseListWaypoint)
				?.ConcatNullable()?.Select(mouseWaypoint => mouseWaypoint?.UIElement)?.WhereNotDefault();

			foreach (var mouseWaypointUIElement in setMotionMouseWaypointUIElement.EmptyIfNull())
				MouseClickLastStepIndexFromUIElementId[mouseWaypointUIElement.Id] = stepIndex;
		}

		public BotStepResult Step(BotStepInput input)
		{
			var beginTimeMilli = GetTimeMilli();

			StepLastInput = input;

			Exception exception = null;

			var listMotion = new List<MotionRecommendation>();

			IBotTask[][] outputListTaskPath = null;

			try
			{
				MemorizeStepInput(input);

				outputListTaskPath = StepOutputListTaskPath()?.ToArray();

				foreach (var moduleToggle in outputListTaskPath.ConcatNullable().OfType<ModuleToggleTask>().Select(moduleToggleTask => moduleToggleTask?.module).WhereNotDefault())
					ToggleLastStepIndexFromModule[moduleToggle] = stepIndex;

				foreach (var taskPath in outputListTaskPath.EmptyIfNull())
				{
					foreach (var effectParam in (taskPath?.LastOrDefault()?.ApplicableEffects()).EmptyIfNull())
					{
						listMotion.Add(new MotionRecommendation
						{
							Id = motionId++,
							MotionParam = effectParam,
						});
					}
				}
			}
			catch (Exception e)
			{
				exception = e;
			}

			var stepResult = new BotStepResult
			{
				Exception = exception,
				ListMotion = listMotion?.ToArrayIfNotEmpty(),
				OutputListTaskPath = outputListTaskPath,
			};

			MemorizeStepResult(stepResult);

			StepLastResultTimespan = new PropertyGenTimespanInt64<BotStepResult>(stepResult, beginTimeMilli, GetTimeMilli());

			StepLastResult = stepResult;

			++stepIndex;

			return stepResult;
		}

		IEnumerable<IBotTask> RootTaskListComponent() =>
			StepLastInput?.RootTaskListComponentOverride ??
			RootTaskListComponentDefault();

		IEnumerable<IBotTask> RootTaskListComponentDefault()
		{
			yield return new BotTask { Component = EnumerateConfigDiagnostics() };

			yield return new EnableInfoPanelCurrentSystem { MemoryMeasurement = MemoryMeasurementAtTime?.Value };

			var saveShipTask = new SaveShipTask { Bot = this };

			yield return saveShipTask;

			yield return this.EnsureIsActive(MemoryMeasurementAccu?.ShipUiModule
				?.Where(module => module.ShouldBeActivePermanent(this)));

			yield return new ModuleHoverTask { bot = this };

			var afterburnerTask = new AfterburnerTask { bot = this };

			yield return afterburnerTask;

			var intelTask = new IntelTask { bot = this };

			yield return intelTask;

			if (!saveShipTask.AllowRoam)
				yield break;

			var closeTelecomTask = new CloseTelecomTask { bot = this };

			yield return closeTelecomTask;

			var combatTask = new CombatTask { bot = this };

			yield return combatTask;

			if (!saveShipTask.AllowAnomalyEnter)
				yield break;

			yield return new UndockTask { bot = this };

			if (combatTask.Completed)
				yield return new AnomalyEnter { bot = this };
		}

		IEnumerable<IBotTask> EnumerateConfigDiagnostics()
		{
			var configDeserializeException = ConfigSerialAndStruct.Key?.Exception;

			if (null != configDeserializeException)
				yield return new DiagnosticTask { MessageText = "error parsing configuration: " + configDeserializeException.Message };
			else
				if (null == ConfigSerialAndStruct.Value)
				yield return new DiagnosticTask { MessageText = "warning: no configuration supplied." };
		}
	}
}
