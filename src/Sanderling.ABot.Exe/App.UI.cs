using Sanderling.ABot.UI;
using System.Collections.Generic;
using System.Windows.Input;

namespace Sanderling.ABot.Exe
{
	partial class App
	{
		static public IEnumerable<IEnumerable<Key>> SetKeyBotMotionDisable => new[]
		{
			new[] { Key.LeftCtrl, Key.LeftAlt },
			new[] { Key.LeftCtrl, Key.RightAlt },
			new[] { Key.RightCtrl, Key.LeftAlt },
			new[] { Key.RightCtrl, Key.RightAlt },
		};

		static public IEnumerable<IEnumerable<Key>> SetKeyBotMotionEnable => new[]
		{
			new[] { Key.LeftCtrl, Key.LeftAlt, Key.LeftShift },
			new[] { Key.LeftCtrl, Key.RightAlt, Key.LeftShift },
			new[] { Key.RightCtrl, Key.LeftAlt, Key.LeftShift },
			new[] { Key.RightCtrl, Key.RightAlt, Key.LeftShift },
		};

		MainWindow Window => MainWindow as MainWindow;

		Main MainControl => Window?.Main;

		Sanderling.UI.InterfaceToEve InterfaceToEveControl => Window?.Main?.Interface;

		public int? EveOnlineClientProcessId => InterfaceToEveControl?.ProcessChoice?.ChoosenProcessId;

		void UIPresent()
		{
			MainControl?.Present(SensorServerDispatcher, MemoryMeasurementLast, bot);
		}
	}
}
