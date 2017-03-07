using System;
using System.Threading;
using Realms;

namespace RealmThread.Tests.Shared
{
	public static class RealmNoSyncContext
	{
		public static Realms.Realm GetInstance(string path)
		{
			var config = new RealmConfiguration(path);
			return GetInstance(config);
		}

		public static Realms.Realm GetInstance(RealmConfigurationBase config)
		{
			var context = SynchronizationContext.Current;
			SynchronizationContext.SetSynchronizationContext(null);

			Realms.Realm realm = null;
			try
			{
				realm = Realms.Realm.GetInstance(config);
			}
			finally
			{
				SynchronizationContext.SetSynchronizationContext(context);
			}
			return realm;
		}
	}
}
