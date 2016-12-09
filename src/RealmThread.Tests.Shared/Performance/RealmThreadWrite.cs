using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;
using Log = System.Diagnostics.Debug;

namespace SushiHangover.Tests
{
	public abstract class Write : IClassFixture<GCFixture>, IDisposable
	{
		protected abstract Realms.Realm CreateRealmInstance(string path);
		protected static Dictionary<int, long> results;
		protected static string dbName;
		protected static string nameOfRunningTest;

		[Theory]
		[Repeat(Utility.COUNT)]
		[TestMethodName]
		public async Task Write_Read_Parallel_On_Different_Threads()
		{
			await GeneratePerfRangesForRealm(async (cache, size) =>
			{
				await cache.WriteAsync((obj) =>
				{
					obj.RemoveAll();
				});
				var toWrite = PerfHelper.GenerateRandomDatabaseContents(size);

				var st = new Stopwatch();
				st.Start();

				await Task.Run(() =>
				{
					using (var blockingQueue = new BlockingCollection<string>())
					using (var realmThreadWrite = new RealmThread(cache.Config))
					using (var realmThreadRead = new RealmThread(cache.Config))
					{
						Parallel.Invoke(() =>
						{
							realmThreadWrite.Invoke(threadSafeWriteRealm =>
							{
								foreach (var kvp in toWrite)
								{
									// Individual record write transactions so the other RealmThread can read asap
									threadSafeWriteRealm.Write(() =>
									{
										var obj = threadSafeWriteRealm.CreateObject(typeof(KeyValueRecord).Name);
										obj.Key = kvp.Key;
										obj.Value = kvp.Value;
									});
									blockingQueue.Add(kvp.Key);
								}
								blockingQueue.CompleteAdding();
							});
						},
						() =>
						{
							realmThreadRead.Invoke((threadSafeReadRealm) =>
							{
								foreach (var key in blockingQueue.GetConsumingEnumerable())
								{
									// Refresh() is automatically called at the beginning of each BeginInvoke, 
									// so if we are within the RealmPump block and need to see the latest changes 
									// from other Realm instances, call Refresh manually
									threadSafeReadRealm.Refresh();
									var record = threadSafeReadRealm.ObjectForPrimaryKey<KeyValueRecord>(key);
									Assert.NotNull(record);
									Assert.Equal(key, record.Key);
								}
							});
						});
					}
				});
				st.Stop();
				return st.ElapsedMilliseconds;
			});
		}

		[Theory]
		[Repeat(Utility.COUNT)]
		[TestMethodName]
		public async Task CreateObject_OneTrans_Invoke()
		{
			await GeneratePerfRangesForRealm(async (cache, size) =>
			{
				var toWrite = PerfHelper.GenerateRandomDatabaseContents(size);

				var st = new Stopwatch();
				st.Start();

				await Task.Run(() =>
				{
					using (var realmThread = new RealmThread(cache.Config))
					{
						realmThread.BeginTransaction();
						realmThread.BeginInvoke((threadSafeRealm) =>
						{
							foreach (var kvp in toWrite)
							{
								var obj = threadSafeRealm.CreateObject(typeof(KeyValueRecord).Name);
								obj.Key = kvp.Key;
								obj.Value = kvp.Value;
							}
						});
						realmThread.CommitTransaction();
					}
				});

				st.Stop();
				return st.ElapsedMilliseconds;
			});
		}

		[Theory]
		[Repeat(Utility.COUNT)]
		[TestMethodName]
		public async Task CreateObject_OneTrans_InvokeAsync()
		{
			await GeneratePerfRangesForRealm(async (cache, size) =>
			{
				var toWrite = PerfHelper.GenerateRandomDatabaseContents(size);

				var st = new Stopwatch();
				st.Start();

				using (var realmThread = new RealmThread(cache.Config))
				{
					await realmThread.InvokeAsync(async (threadSafeRealm) =>
					{
						// Mock something using an await in this lamba
						await Task.FromResult(true);

						threadSafeRealm.Write(() =>
						{
							foreach (var kvp in toWrite)
							{
								var obj = threadSafeRealm.CreateObject(typeof(KeyValueRecord).Name);
								obj.Key = kvp.Key;
								obj.Value = kvp.Value;
							}
						});
					});
				}

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
			using (var cache = Realms.Realm.GetInstance(Path.Combine(dirPath, "perf.realm")))
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
				//Log.WriteLine($"~~~~~~~~ Finished:\t{nameOfRunningTest} ~~~~~~~~");
			}
		}
	}

	public class RealmThreadCoreWrite : Write, IDisposable
	{
		protected override Realms.Realm CreateRealmInstance(string path)
		{
			return Realms.Realm.GetInstance(Path.Combine(path, "perf.realm"));
		}
	}


}
