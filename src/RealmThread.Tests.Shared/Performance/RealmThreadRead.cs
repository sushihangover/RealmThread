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
using RealmThread.Tests.Shared;
using Xunit;
using Xunit.Sdk;

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
		public async Task Find_Sequential()
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

					var rt = new RealmThread(cache.Config);
					rt.BeginInvoke(threadSafeRealm =>
					{
						foreach (var v in toFetch)
						{
							var record = threadSafeRealm.Find<KeyValueRecord>(v);
							Assert.NotNull(record);
						}
					});
					rt.Dispose();

					st.Stop();
				});

				return st.ElapsedMilliseconds;
			});
		}

		[Theory]
		[Repeat(Utility.COUNT)]
		[TestMethodName]
		public async Task Find_Parallel_SingleRealmThread()
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

					using (var rt = new RealmThread(cache.Config))
					{
						Parallel.ForEach(
							toFetch,
							key =>
							{
								rt.BeginInvoke(threadSafeRealm =>
								{
									var record = threadSafeRealm.Find<KeyValueRecord>(key);
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
		public async Task Find_Parallel_MultiRealmThread()
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
						() => new RealmThread(cache.Config),
						(key, loopState, realmThread) =>
						{
							realmThread.BeginInvoke(threadSafeRealm =>
							{
								threadSafeRealm.Find<KeyValueRecord>(key);
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

				using (var realmThread = new RealmThread(cache.Config))
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
				IsReadOnly = false
			};

			var cache = RealmThread.GetInstance(config);

			var keys = cache.All<KeyValueRecord>().Count();
			if (keys == giantDbSize) return cache;

			using (var realmThread = new RealmThread(cache.Config))
			{
				realmThread.Invoke((obj) =>
				{
					obj.Write(() => { obj.RemoveAll(); });
				});
			}

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

				using (var rt = new RealmThread(targetCache.Config))
				{
					await rt.InvokeAsync(async (Realms.Realm r) =>
					{ 
						await r.WriteAsync((updateRealm) =>
						{
							foreach (var item in toWrite)
							{
								var c = new KeyValueRecord { Key = item.Key, Value = item.Value };
								updateRealm.Add(c); // update: false
							}
						});
					});
				}
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
			using (var cache = RealmThread.GetInstance(Path.Combine(dirPath, "perf.realm")))
			{
				using (var rt = new RealmThread(cache.Config))
				{
					rt.Invoke((r) =>
					{
						r.Write(() => { r.RemoveAll(); });
					});
				}
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
			return RealmNoSyncContext.GetInstance(Path.Combine(path, "realm.db"));
		}
	}
}
