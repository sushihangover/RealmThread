using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using D = System.Diagnostics.Debug;

namespace SushiHangover
{
	public sealed class FauxVoid
	{
		FauxVoid() { }
		readonly static FauxVoid nothing = new FauxVoid();
		public static FauxVoid AtAll { get { return nothing; } }
	}

}
