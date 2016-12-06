using System;
using System.Collections.Generic;
using System.Linq;

// iOS
// cat  ~/Library/Logs/CoreSimulator/{SIMGUID}/system.log | grep -e "RealmThread/" |  cut -d : -f 4-

// Android
// adb logcat -s REALMTHREAD

// comma sep. columns in output for easy parsing, cut/paste to Excel for quick charting...

#if __IOS__
// Application Output Windows/Pad only
//using Log = System.Diagnostics.Debug; 
using Log = System.Console;
#endif

namespace SushiHangover.Tests
{
#if __ANDROID__

	public static class Log
	{
		public static void WriteLine(string msg)
		{
			Android.Util.Log.Info("REALMTHREAD", msg);
		}
	}

#endif

	public static class PerformanceData
	{
		public static void Publish(this Dictionary<int, long> This, string dbName, string nameOfTest)
		{
			// Filter log by 'RealmThread/'
			var times = string.Format($"\tRealmThread/{dbName},{nameOfTest}");
			foreach (var kvp in This)
			{
				times += string.Format($",{kvp.Value}");
			}
			var tRec = This.Sum(x => x.Key);
			float tTime = This.Sum(x => x.Value);
			times += ($",{tTime / tRec}:ms/rec");
			Log.WriteLine($"{times}");
		}
	}

}
