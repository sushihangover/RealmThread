using System;
namespace SushiHangover
{
	internal static class PCLHelpers
	{
		internal static void ThrowProxyShouldNeverBeUsed()
		{
			throw new PlatformNotSupportedException("The PCL build of RealmThread is linked which probably means you need to use NuGet or otherwise link a platform-specific SushiHangover.RealmThread.dll to your main application.");
		}
	}
}
