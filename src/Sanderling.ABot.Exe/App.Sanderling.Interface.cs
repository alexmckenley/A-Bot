﻿using Bib3.Synchronization;
using BotEngine.Interface;
using Sanderling.Interface.MemoryStruct;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sanderling.ABot.Exe
{
	/// <summary>
	/// This Type must reside in an Assembly that can be resolved by the default assembly resolver.
	/// </summary>
	public class InterfaceAppDomainSetup
	{
		static InterfaceAppDomainSetup()
		{
			BotEngine.Interface.InterfaceAppDomainSetup.Setup();
		}
	}

	partial class App
	{
        readonly Sensor sensor = new Sensor();

		private bool waitingForMeasurementLock = false;

		private static Mutex measurementMutex = new Mutex(false, "SanderlingAbotMeasurementMutex");

		FromProcessMeasurement<IMemoryMeasurement> MemoryMeasurementLast;

		readonly SimpleInterfaceServerDispatcher SensorServerDispatcher = new SimpleInterfaceServerDispatcher
		{
			InterfaceAppDomainSetupType = typeof(InterfaceAppDomainSetup),
			InterfaceAppDomainSetupTypeLoadFromMainModule = true,
			LicenseClientConfig = Sanderling.ExeConfig.LicenseClientDefault,
		};

		readonly Bib3.RateLimit.IRateLimitStateInt MemoryMeasurementRequestRateLimit = new Bib3.RateLimit.RateLimitStateIntSingle();

		void InterfaceExchange()
		{
			var eveOnlineClientProcessId = EveOnlineClientProcessId;

			var measurementRequestTime = MeasurementRequestTime() ?? 0;

			if (eveOnlineClientProcessId.HasValue && measurementRequestTime <= GetTimeStopwatch())
				if (MemoryMeasurementRequestRateLimit.AttemptPass(GetTimeStopwatch(), 700) && !this.waitingForMeasurementLock)
				{
					this.waitingForMeasurementLock = true;
					Task.Run(() => {
						App.measurementMutex.WaitOne();
						botLock.WhenLockIsAvailableEnter(30000, () =>
						{
							MeasurementMemoryTake(eveOnlineClientProcessId.Value, measurementRequestTime);
							App.measurementMutex.ReleaseMutex();
							this.waitingForMeasurementLock = false;
						});
					});
				}
		}

		void MeasurementMemoryTake(int processId, Int64 measurementBeginTimeMinMilli)
		{
			var measurement = sensor.MeasurementTake(processId, measurementBeginTimeMinMilli);

			if (null == measurement)
				return;

			MemoryMeasurementLast = measurement;
		}
	}
}
