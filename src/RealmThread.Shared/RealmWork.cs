using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using D = System.Diagnostics.Debug;

namespace SushiHangover
{
	public sealed class RealmWork : WorkItem<Realms.Realm, Realms.RealmObject>
	{
		public RealmWork(Action<Realms.Realm> action) : base(action)
		{
		}

		public RealmWork(Action<Realms.Realm> action, ManualResetEventSlim autoResetEvent) : base(action, autoResetEvent)
		{
		}

		public RealmWork(Func<Realms.Realm, Task> func, ManualResetEventSlim autoResetEvent, Action<Task<Realms.RealmObject>> task) : base(func, autoResetEvent, task)
		{
		}
	}

}
