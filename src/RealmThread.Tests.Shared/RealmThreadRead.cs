using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Realms;
using Xunit;
using Xunit.Sdk;
using Log = System.Diagnostics.Debug;

namespace SushiHangover.Tests
{
	public abstract class Read : IClassFixture<GCFixture>, IDisposable
	{
		protected static Dictionary<int, long> results;
		protected static string dbName;
		protected static string nameOfRunningTest;
		readonly Random prng = new Random();

		protected abstract Realms.Realm CreateRealmsInstance(string path);

		[Theory]
		[Repeat(Utility.COUNT)]
		[TestMethodName]
		public async Task ObjectForPrimaryKey_Sequential()
		{
			await GeneratePerfRangesForBlock2(async (cache, size, keys) =>
			{
				var st = new Stopwatch();
				var toFetch = Enumerable.Range(0, size)
					.Select(_ => keys[prng.Next(0, keys.Count - 1)])
					.ToArray();

				await Task.Run(() =>
				{
					st.Start();

					var realmThread = new RealmThread(cache);
					realmThread.BeginInvoke(threadSafeRealm =>
					{
						foreach (var v in toFetch)
						{
							threadSafeRealm.ObjectForPrimaryKey<KeyValueRecord>(v);
						}
					});
					realmThread.Dispose();

					st.Stop();
				});

				return st.ElapsedMilliseconds;
			});
		}

		[Theory]
		[Repeat(Utility.COUNT)]
		[TestMethodName]
		public async Task ObjectForPrimaryKey_Parallel_SingleRealmThread()
		{
			await GeneratePerfRangesForBlock2(async (cache, size, keys) =>
			{
				var st = new Stopwatch();
				var toFetch = Enumerable.Range(0, size)
					.Select(_ => keys[prng.Next(0, keys.Count - 1)])
					.ToArray();

				await Task.Run(() =>
				{
					st.Start();

					using (var realmThread = new RealmThread(cache))
					{
						Parallel.ForEach(
							toFetch,
							key =>
							{
								realmThread.BeginInvoke(threadSafeRealm =>
								{
									var record = threadSafeRealm.ObjectForPrimaryKey<KeyValueRecord>(key);
									Assert.NotNull(record);
								});
							});
					}

					st.Stop();
				});
				return st.ElapsedMilliseconds;
			});
		}

		[Theory]
		[Repeat(Utility.COUNT)]
		[TestMethodName]
		public async Task ObjectForPrimaryKey_Parallel_MultiRealmThread()
		{
			await GeneratePerfRangesForBlock2(async (cache, size, keys) =>
			{
				var st = new Stopwatch();
				var toFetch = Enumerable.Range(0, size)
					.Select(_ => keys[prng.Next(0, keys.Count - 1)])
					.ToArray();

				await Task.Run(() =>
				{
					st.Start();

					var partitoner = Partitioner.Create(toFetch, EnumerablePartitionerOptions.NoBuffering);
					Parallel.ForEach(
						partitoner,
						() => new RealmThread(cache),
						(key, loopState, realmThread) =>
						{
							realmThread.BeginInvoke(threadSafeRealm =>
							{
								threadSafeRealm.ObjectForPrimaryKey<KeyValueRecord>(key);
							});
							return realmThread;
						},
						(localRealmThread) =>
						{
							localRealmThread.Dispose();
						}
					);

					st.Stop();
				});
				return st.ElapsedMilliseconds;
			});
		}

		//[Fact]
		//[TestMethodName]
		//public async Task GetAsync_RealmObject()
		//{
		//	await GeneratePerfRangesForBlock2(async (cache, size, keys) =>
		//	{
		//		var st = new Stopwatch();
		//		var toFetch = Enumerable.Range(0, size)
		//			.Select(_ => keys[prng.Next(0, keys.Count - 1)])
		//			.ToArray();

		//		await Task.Run(async () =>
		//		{
		//			st.Start();

		//			using (var realmThread = new RealmThread(cache))
		//			{
		//				var obj = await realmThread.GetAsync<KeyValueRecord>(threadsafeRealm =>
		//				{
		//					var c = new KeyValueRecord() { Key = "Foo", Value = new byte[1] };
		//					return Task.FromResult(c);
		//				});
		//			}

		//			st.Stop();
		//		});

		//		return st.ElapsedMilliseconds;
		//	});
		//}

		public async Task GeneratePerfRangesForBlock2(Func<Realms.Realm, int, List<string>, Task<long>> block)
		{
			results = new Dictionary<int, long>();
			dbName = default(string);

			var dirPath = default(string);
			using (Utility.WithEmptyDirectory(out dirPath))
			using (var cache = await GenerateRealmDB(dirPath))
			{
				List<string> keys = null;

				using (var realmThread = new RealmThread(cache))
				{
					realmThread.BeginInvoke(r =>
					{
						keys = r.All<KeyValueRecord>().ToList().Select(x => x.Key).ToList();
					});
				}
				dbName = dbName ?? cache.GetType().Name;

				foreach (var size in PerfHelper.GetPerfRanges())
				{
					results[size] = await block(cache, size, keys.ToList());
				}
			}
		}

		async Task<Realms.Realm> GenerateRealmDB(string path)
		{
			path = path ?? IntegrationTestHelper.GetIntegrationTestRootDirectory();

			var giantDbSize = PerfHelper.GetPerfRanges().Last();
			var config = new RealmConfiguration(Path.Combine(path, "perf.realm"))
			{
				ObjectClasses = new Type[] { typeof(KeyValueRecord) },
				ReadOnly = false
			};
			var cache = Realms.Realm.GetInstance(config);

			var keys = cache.All<KeyValueRecord>().Count();
			if (keys == giantDbSize) return cache;

			await cache.WriteAsync(r =>
			{
				r.RemoveAll();
			});
			await GenerateRealmDB(cache, giantDbSize);

			return cache;
		}

		protected static async Task<List<string>> GenerateRealmDB(Realms.Realm targetCache, int size)
		{
			var ret = new List<string>();

			// Write out in groups of 4096
			while (size > 0)
			{
				var toWriteSize = Math.Min(4096, size);
				var toWrite = PerfHelper.GenerateRandomDatabaseContents(toWriteSize);

				await targetCache.WriteAsync((Realms.Realm realm) =>
				{
					foreach (var item in toWrite)
					{
						var c = new KeyValueRecord { Key = item.Key, Value = item.Value };
						realm.Manage<KeyValueRecord>(c); // update: false
					}
				});
				foreach (var k in toWrite.Keys) ret.Add(k);
				size -= toWrite.Count;
			}
			return ret;
		}

		protected async Task GeneratePerfRangesForRealm(Func<Realms.Realm, int, Task<long>> block)
		{
			results = new Dictionary<int, long>();
			dbName = default(string);

			var dirPath = default(string);
			using (Utility.WithEmptyDirectory(out dirPath))
			using (var cache = Realms.Realm.GetInstance(Path.Combine(dirPath, "perf.realm")))
			{
				cache.RemoveAll();
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
				//Log.WriteLine($"~~~~~~~~ Starting:\t{methodUnderTest.Name} ~~~~~~~~");
			}

			public override void After(MethodInfo methodUnderTest)
			{
				//Log.WriteLine($"~~~~~~~~ Finished:\t{methodUnderTest.Name} ~~~~~~~~");
			}
		}

	}

	public class RealmThreadRead : Read, IDisposable
	{
		protected override Realms.Realm CreateRealmsInstance(string path)
		{
			var realm = Realms.Realm.GetInstance(Path.Combine(path, "perf.relam"));
			return realm;
		}
	}

}
