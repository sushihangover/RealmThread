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
using RealmThread.Tests.Shared;

namespace SushiHangover.Tests
{
	public abstract class RealmRead : IClassFixture<GCFixture>, IDisposable
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
					using (var localRealm = RealmThread.GetInstance(cache.Config))
					{
						st.Start();

						foreach (var v in toFetch)
						{
							localRealm.Find<KeyValueRecord>(v);
						}

						st.Stop();
					}
				});

				return st.ElapsedMilliseconds;
			});
		}

		[Theory]
		[Repeat(Utility.COUNT)]
		[TestMethodName]
		public async Task ObjectForPrimaryKey_Sequential_MultiInstance()
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

					foreach (var key in toFetch)
					{
						using (var localRealm = RealmThread.GetInstance(cache.Config))
						{
							var obj = localRealm.Find<KeyValueRecord>(key);
							Assert.Equal(key, obj.Key);
						}
					}

					st.Stop();
				});

				return st.ElapsedMilliseconds;
			});
		}

		[Theory]
		[Repeat(Utility.COUNT)]
		[TestMethodName]
		public async Task ObjectForPrimaryKey_Sequential_MultiInstance_Via_Task()
		{
			await GeneratePerfRangesForBlock2(async (cache, size, keys) =>
			{
				var st = new Stopwatch();
				var toFetch = Enumerable.Range(0, size)
					.Select(_ => keys[prng.Next(0, keys.Count - 1)])
					.ToArray();

				await Task.Run(async () =>
				{
					st.Start();

					foreach (var key in toFetch)
					{
						await Task.Run(() =>
						{
							using (var localRealm = RealmThread.GetInstance(cache.Config))
							{
								localRealm.Refresh();
								var obj = localRealm.Find<KeyValueRecord>(key);
								Assert.NotNull(obj);
								Assert.Equal(key, obj.Key);
							}
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
		public async Task ObjectForPrimaryKey_Parallel()
		{
			await GeneratePerfRangesForBlock2(async (cache, size, keys) =>
			{
				var st = new Stopwatch();
				var toFetch = Enumerable.Range(0, size)
					.Select(_ => keys[prng.Next(0, keys.Count - 1)])
					.ToArray();

				st.Start();

				// The following code crashes with "Invalid IL code" on iOS(?), Android not tested...
				// Invalid IL code in Tests.Performance.RealmRead 
				///< Sequential_ObjectForPrimaryKey_Parallel > c__async22 
				///< Sequential_ObjectForPrimaryKey_Parallel > c__AnonStorey23:<> m__1(string): IL_0008: stfld     0x04000443
				//at System.Reactive.Linq.ObservableImpl.Select`2 + _[TSource, TResult].OnNext(TSource value)[0x00008] in < d0067ed104ac455987b6feb85f80156b >:0

				//var scheduler = System.Reactive.Concurrency.TaskPoolScheduler.Default;
				//await toFetch
				//	.ToObservable(scheduler)
				//	.Select(key => Observable.Defer(() => GetRecordViaPrimaryKey(cache, key)))
				//	.ToArray();

				await Task.Run(() =>
				{
					Parallel.ForEach(
						toFetch,
						new ParallelOptions { MaxDegreeOfParallelism = 4 },
						key =>
						{
							using (var localRealm = RealmThread.GetInstance(cache.Config))
							{
								localRealm.Refresh();
								var obj = localRealm.Find<KeyValueRecord>(key);
								Assert.NotNull(obj);
								Assert.Equal(key, obj.Key);
							}
						}
					);
				});

				st.Stop();
				return st.ElapsedMilliseconds;
			});
		}

		[Theory]
		[Repeat(Utility.COUNT)]
		[TestMethodName]
		public async Task LinqWhereKey_Sequential()
		{
			await GeneratePerfRangesForBlock2(async (cache, size, keys) =>
			{
				var st = new Stopwatch();
				var toFetch = Enumerable.Range(0, size)
					.Select(_ => keys[prng.Next(0, keys.Count - 1)])
					.ToArray();

				await Task.Run(() =>
				{
					var localRealm = RealmThread.GetInstance(cache.Config);
					st.Start();

					foreach (var key in toFetch)
					{
						var objs = localRealm.All<KeyValueRecord>().Where((KeyValueRecord c) => c.Key == key);
						Assert.Equal(1, objs.Count());
					}
					st.Stop();
					localRealm.Dispose();
				});

				return st.ElapsedMilliseconds;
			});
		}

		[Theory]
		[Repeat(Utility.COUNT)]
		[TestMethodName]
		public async Task LinqWhereKey_Sequential_MultiInstance()
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

					foreach (var key in toFetch)
					{
						using (var localRealm = RealmThread.GetInstance(cache.Config))
						{
							var objs = localRealm.All<KeyValueRecord>().Where((KeyValueRecord c) => c.Key == key);
							Assert.Equal(1, objs.Count());
						}
					}

					st.Stop();
				});

				return st.ElapsedMilliseconds;
			});
		}

		[Theory]
		[Repeat(Utility.COUNT)]
		[TestMethodName]
		public async Task LinqWhereKey_Sequential_MultiInstance_Via_Task()
		{
			await GeneratePerfRangesForBlock2(async (cache, size, keys) =>
			{
				var st = new Stopwatch();
				var toFetch = Enumerable.Range(0, size)
					.Select(_ => keys[prng.Next(0, keys.Count - 1)])
					.ToArray();

				await Task.Run(async () =>
				{
					st.Start();

					foreach (var key in toFetch)
					{
						await Task.Run(() =>
						{
							using (var localRealm = RealmThread.GetInstance(cache.Config))
							{
								var objs = localRealm.All<KeyValueRecord>().Where((KeyValueRecord c) => c.Key == key);
								Assert.Equal(1, objs.Count());
							}
						});
					}

					st.Stop();
				});

				return st.ElapsedMilliseconds;
			});
		}

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
					realmThread.Invoke((r) =>
					{
						r.Write(() => { 
							keys = r.All<KeyValueRecord>().ToList().Select(x => x.Key).ToList();
						});
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
			var config = new RealmConfiguration(Path.Combine(path, "realm.db"))
			{
				ObjectClasses = new Type[] { typeof(KeyValueRecord) }
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
					await rt.InvokeAsync(async(Realms.Realm arg) =>
					{
						await arg.WriteAsync((r) =>
						{
							foreach (var item in toWrite)
							{
								var c = new KeyValueRecord { Key = item.Key, Value = item.Value };
								r.Add(c); // update: false
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
			using (var cache = RealmThread.GetInstance(Path.Combine(dirPath, "realm.db")))
			{
				using (var realmThread = new RealmThread(cache.Config))
				{
					realmThread.Invoke((obj) =>
					{
						obj.Write(() => { obj.RemoveAll(); });
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
				//Log.WriteLine($"~~~~~~~~ Completed:\t{methodUnderTest.Name} ~~~~~~~~");
			}
		}
	}

	public class RealmCoreRead : RealmRead, IDisposable
	{
		protected override Realms.Realm CreateRealmsInstance(string path)
		{
			return RealmNoSyncContext.GetInstance(Path.Combine(path, "realm.db"));
		}
	}
}
