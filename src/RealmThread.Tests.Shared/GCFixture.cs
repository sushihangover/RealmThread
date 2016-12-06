using System;

namespace SushiHangover.Tests
{
	// Force a GC *before* each performance xUnit test begins
	public class GCFixture : IDisposable 
	{
		public GCFixture()
		{
			GC.Collect();
		}
		public void Dispose()
		{
			GC.Collect();
		}
	}
}
