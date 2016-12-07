using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;
using System.Reflection;
using System.Reactive;
using Realms;
using Log = System.Diagnostics.Debug;

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
		public async Task CreateObject_OneTrans()
		{
			await GeneratePerfRangesForRealm(async (cache, size) =>
			{
				var toWrite = PerfHelper.GenerateRandomDatabaseContents(size);

				var st = new Stopwatch();
				st.Start();

				await cache.WriteAsync((r) =>
				{
					foreach (var kvp in toWrite)
					{
						var c = r.CreateObject(typeof(KeyValueRecord).Name);
						c.Key = kvp.Key;
						c.Value = kvp.Value;
					}
				});

				st.Stop();
				return st.ElapsedMilliseconds;
			});
		}

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

				await cache.WriteAsync((r) =>
				{
					foreach (var kvp in toWrite)
					{
						var c = new KeyValueRecord();
						c.Key = kvp.Key;
						c.Value = kvp.Value;
						r.Manage(c, update: false);
					}
				});

				st.Stop();
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

				await cache.WriteAsync((r) =>
				{
					foreach (var kvp in toWrite)
					{
						var c = new KeyValueRecord();
						c.Key = kvp.Key;
						c.Value = kvp.Value;
						r.Manage(c, update: true);
					}
				});

				st.Stop();
				return st.ElapsedMilliseconds;
			});
		}

		protected async Task GeneratePerfRangesForRealm(Func<Realms.Realm, int, Task<long>> block)
		{
			results = new Dictionary<int, long>();
			dbName = default(string);
			var dirPath = default(string);
			using (Utility.WithEmptyDirectory(out dirPath))
			using (var cache = Realms.Realm.GetInstance(Path.Combine(dirPath, "realm.db")))
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
			return Realms.Realm.GetInstance(Path.Combine(path, "realm.db"));
		}
	}


}
