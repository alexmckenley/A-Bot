﻿using BotEngine.Interface;
using BotEngine.UI;
using Sanderling.UI;
using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Sanderling.ABot.UI
{
	public partial class Main : UserControl
	{
		static public event Action<Interface.MemoryStruct.IMemoryMeasurement> SimulateMeasurement;

		public void BotMotionDisable() => ToggleButtonMotionEnable?.LeftButtonDown();

		public void BotMotionEnable() => ToggleButtonMotionEnable?.RightButtonDown();

		public bool IsBotMotionEnabled => ToggleButtonMotionEnable?.ButtonRecz?.IsChecked ?? false;

		public Main()
		{
			InitializeComponent();
		}

		public void Present(
			SimpleInterfaceServerDispatcher interfaceServerDispatcher,
			FromProcessMeasurement<Interface.MemoryStruct.IMemoryMeasurement> measurement,
			Bot.Bot bot)
		{
			Interface?.Present(interfaceServerDispatcher, measurement);

			InterfaceHeader?.SetStatus(Interface.InterfaceStatusEnum());

			BotStepResultTextBox.Text = bot?.StepLastResultTimespan?.RenderBotStepToUIText();
		}

		private void BotStepResultCopyToClipboardButton_Click(object sender, RoutedEventArgs e)
		{
			Clipboard.SetText(BotStepResultTextBox.Text);
		}

		private void SimulateMeasurement_Drop(object sender, DragEventArgs e)
		{
			Bib3.FCL.GBS.Extension.CatchNaacMessageBoxException(() =>
			{
				var file = Bib3.FCL.Glob.LaadeFrüühestInhaltDataiAusDropFileDrop(e);

				try
				{
					var memoryMeasurementJson = Encoding.UTF8.GetString(file.Value);

					var graph = Bib3.RefNezDiferenz.Extension.ListeWurzelDeserialisiireVonJson(memoryMeasurementJson)?.FirstOrDefault();

					if (null == graph)
						throw new ArgumentException("failed to read graph.");

					var measurement =
						(graph as BotEngine.Interface.FromProcessMeasurement<Interface.MemoryStruct.IMemoryMeasurement>)?.Value ??
						graph as Interface.MemoryStruct.IMemoryMeasurement;

					if (null == measurement)
						throw new ArgumentException("unexpected type:" + graph?.GetType());

					SimulateMeasurement?.Invoke(measurement);
				}
				catch (Exception exc)
				{
					throw new Exception("Loaded from file " + file.Key, exc);
				}
			});
		}
	}
}
