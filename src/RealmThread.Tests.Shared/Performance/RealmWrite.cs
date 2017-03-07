using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;
using RealmThread.Tests.Shared;

namespace SushiHangover.Tests
{
	public abstract class RealmWrite : IClassFixture<GCFixture>, IDisposable
	{
		protected abstract Realms.Realm CreateRealmInstance(string path);
		protected static Dictionary<int, long> results;
		protected static string dbName;
		protected static string nameOfRunningTest;

		[Theory]
		[Repeat(Utility.COUNT)]
		[TestMethodName]
		public async Task Manage_updateFalse_OneTrans()
		{
			await GeneratePerfRangesForRealm(async (cache, size) =>
			{
				var toWrite = PerfHelper.GenerateRandomDatabaseContents(size);

				var st = new Stopwatch();
				st.Start();

				using (var realmThread = new RealmThread(cache.Config))
				{
					realmThread.Invoke((realm) =>
					{
						realm.Write(() =>
						{
							foreach (var kvp in toWrite)
							{
								var c = new KeyValueRecord { Key = kvp.Key, Value = kvp.Value };
								realm.Add(c, update: false);
							}
						});
					});
				}

				st.Stop();
				await Task.Delay(1); // cheap hack
				return st.ElapsedMilliseconds;
			});
		}

		[Theory]
		[Repeat(Utility.COUNT)]
		[TestMethodName]
		public async Task Manage_updateTrue_OneTrans()
		{
			await GeneratePerfRangesForRealm(async (cache, size) =>
			{
				var toWrite = PerfHelper.GenerateRandomDatabaseContents(size);

				var st = new Stopwatch();
				st.Start();

				using (var realmThread = new RealmThread(cache.Config))
				{
					realmThread.Invoke((realm) =>
					{
						realm.Write(() =>
						{
							foreach (var kvp in toWrite)
							{
								var c = new KeyValueRecord { Key = kvp.Key, Value = kvp.Value };
								realm.Add(c, update: true);
							}
						});
					});
				}

				st.Stop();
				await Task.Delay(1); // cheap hack
				return st.ElapsedMilliseconds;
			});
		}

		protected async Task GeneratePerfRangesForRealm(Func<Realms.Realm, int, Task<long>> block)
		{
			results = new Dictionary<int, long>();
			dbName = default(string);
			var dirPath = default(string);
			using (Utility.WithEmptyDirectory(out dirPath))
			using (var cache = RealmThread.GetInstance(Path.Combine(dirPath, "realm.db")))
			{
				dbName = "Realm";

				foreach (var size in PerfHelper.GetPerfRanges())
				{
					results[size] = await block(cache, size);
				}
			}
		}

		public void Dispose()
		{
			results.Publish(dbName, nameOfRunningTest);
		}

		protected class TestMethodNameAttribute : BeforeAfterTestAttribute
		{
			public override void Before(MethodInfo methodUnderTest)
			{
				var x = GetType().FullName.Replace("+TestMethodNameAttribute", "") + ".";
				nameOfRunningTest = x + methodUnderTest.Name;
				//Log.WriteLine($"~~~~~~~~ Starting:\t{nameOfRunningTest} ~~~~~~~~");
			}

			public override void After(MethodInfo methodUnderTest)
			{
			}
		}
	}

	public class RealmCoreWrite : RealmWrite, IDisposable
	{
		protected override Realms.Realm CreateRealmInstance(string path)
		{
			return RealmNoSyncContext.GetInstance(Path.Combine(path, "realm.db"));
		}
	}
}
