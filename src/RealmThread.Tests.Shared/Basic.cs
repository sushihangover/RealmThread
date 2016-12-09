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
using System.Threading;

namespace SushiHangover.Tests
{
	public abstract class Basic : IClassFixture<GCFixture>, IDisposable
	{
		//protected static Dictionary<int, long> results;
		//protected static string dbName;
		protected static string nameOfRunningTest;
		//readonly Random prng = new Random();

		protected abstract Realms.Realm CreateRealmsInstance(string path);

		[Theory]
		[Repeat(Utility.COUNT)]
		[TestMethodName]
		public void ThreadCreate()
		{
			var callingThread = Thread.CurrentThread.ManagedThreadId;

			string path;
			using (Utility.WithEmptyDirectory(out path))
			using (var fixture = CreateRealmsInstance(path))
			using (var t = new RealmThread(fixture.Config))
			{
				Assert.NotEqual(callingThread, t.ManagedThreadId);
			}
		}

		[Theory]
		[Repeat(Utility.COUNT)]
		[TestMethodName]
		public void ThreadInvoke()
		{
			string path;
			using (Utility.WithEmptyDirectory(out path))
			using (var fixture = CreateRealmsInstance(path))
			using (var t = new RealmThread(fixture.Config))
			{
				// Add a record on a thread
				t.Invoke(r =>
				{
					r.Write(() =>
					{
						var obj = r.CreateObject<KeyValueRecord>();
						obj.Key = "key";
					});
				});
				fixture.Refresh();
				Assert.NotNull(fixture.ObjectForPrimaryKey<KeyValueRecord>("key"));
			}
		}

		[Theory]
		[Repeat(Utility.COUNT)]
		[TestMethodName]
		public void ThreadInvoke_AutoRefresh()
		{
			string path;
			using (Utility.WithEmptyDirectory(out path))
			using (var fixture = CreateRealmsInstance(path))
			using (var t = new RealmThread(fixture.Config))
			{
				// Add a record on the current thread
				fixture.Write(() =>
				{
					var obj = fixture.CreateObject<KeyValueRecord>();
					obj.Key = "key";
				});
				t.Invoke(r =>
				{
					// Before each action a Refresh is performed so the record should be automaticially available to this thread
					Assert.NotNull(r.ObjectForPrimaryKey<KeyValueRecord>("key"));
				});
			}
		}

		[Theory]
		[Repeat(Utility.COUNT)]
		[TestMethodName]
		public void ThreadInvoke_InTransaction()
		{
			string path;
			using (Utility.WithEmptyDirectory(out path))
			using (var fixture = CreateRealmsInstance(path))
			using (var t = new RealmThread(fixture.Config))
			{
				Assert.False(t.InTransaction);
				t.BeginTransaction();
				Assert.True(t.InTransaction);
			}
		}

		[Theory]
		[Repeat(Utility.COUNT)]
		[TestMethodName]
		public void ThreadInvoke_InTransaction_AutoCommit()
		{
			string path;
			using (Utility.WithEmptyDirectory(out path))
			using (var fixture = CreateRealmsInstance(path))
			{
				using (var realmThread = new RealmThread(fixture.Config, true))
				{
					realmThread.BeginTransaction();
					var keyValueRecord = new KeyValueRecord(); // a captured variable
					realmThread.Invoke(r =>
					{
						var obj = r.CreateObject<KeyValueRecord>();
						obj.Key = "key";
						keyValueRecord.Key = obj.Key;
					});
					Console.WriteLine($"{keyValueRecord.Key}:{keyValueRecord.Value}");
					Assert.Equal("key", keyValueRecord.Key);
				}
				fixture.Refresh();
				Assert.NotNull(fixture.ObjectForPrimaryKey<KeyValueRecord>("key"));
			}
		}

		[Theory]
		[Repeat(Utility.COUNT)]
		[TestMethodName]
		public void ThreadInvoke_InTransaction_AutoRollback()
		{
			string path;
			using (Utility.WithEmptyDirectory(out path))
			using (var fixture = CreateRealmsInstance(path))
			{
				using (var t = new RealmThread(fixture.Config, false))
				{
					t.BeginTransaction();
					t.Invoke(r =>
					{
						var obj = r.CreateObject<KeyValueRecord>();
						obj.Key = "key";
					});
				}
				fixture.Refresh();
				Assert.Null(fixture.ObjectForPrimaryKey<KeyValueRecord>("key"));
			}
		}

		[Theory]
		[Repeat(Utility.COUNT)]
		[TestMethodName]
		public void ThreadInvoke_Transaction_Begin_Commit()
		{
			string path;
			using (Utility.WithEmptyDirectory(out path))
			using (var fixture = CreateRealmsInstance(path))
			using (var t = new RealmThread(fixture.Config))
			{
				t.BeginTransaction();
				t.Invoke(r =>
				{
					var obj = r.CreateObject<KeyValueRecord>();
					obj.Key = "key";
				});
				fixture.Refresh();
				Assert.Null(fixture.ObjectForPrimaryKey<KeyValueRecord>("key")); // Should not be available yet
				t.CommitTransaction();
				fixture.Refresh();
				Assert.NotNull(fixture.ObjectForPrimaryKey<KeyValueRecord>("key")); // Should now be available
			}
		}

		[Theory]
		[Repeat(Utility.COUNT)]
		[TestMethodName]
		public void ThreadInvoke_Transaction_Begin_RollBack()
		{
			string path;
			using (Utility.WithEmptyDirectory(out path))
			using (var fixture = CreateRealmsInstance(path))
			using (var t = new RealmThread(fixture.Config))
			{
				t.BeginTransaction();
				t.Invoke(r =>
				{
					var obj = r.CreateObject<KeyValueRecord>();
					obj.Key = "key";
				});
				fixture.Refresh();
				Assert.Null(fixture.ObjectForPrimaryKey<KeyValueRecord>("key")); // Should not be available yet
				t.RollbackTransaction();
				fixture.Refresh();
				Assert.Null(fixture.ObjectForPrimaryKey<KeyValueRecord>("key")); // Should not be available
			}
		}

		[Theory]
		[Repeat(Utility.COUNT)]
		[TestMethodName]
		public void ThreadInvoke_Transaction_ViaCapturedVariable()
		{
			string path;
			using (Utility.WithEmptyDirectory(out path))
			using (var fixture = CreateRealmsInstance(path))
			using (var t = new RealmThread(fixture.Config))
			{
				Transaction trans = null;
				t.Invoke(r =>
				{
					trans = r.BeginWrite();
				});
				t.Invoke(r =>
				{
					var obj = r.CreateObject<KeyValueRecord>();
					obj.Key = "key";
				});
				fixture.Refresh();
				Assert.Null(fixture.ObjectForPrimaryKey<KeyValueRecord>("key")); // Should not be available yet
				t.Invoke(r =>
				{
					if (trans != null)
						trans.Commit();
				});
				fixture.Refresh();
				Assert.NotNull(fixture.ObjectForPrimaryKey<KeyValueRecord>("key")); // Should now be available
			}
		}

		[Theory]
		[Repeat(Utility.COUNT)]
		[TestMethodName]
		public void ThreadBeginInvoke()
		{
			string path;
			using (Utility.WithEmptyDirectory(out path))
			using (var fixture = CreateRealmsInstance(path))
			using (var t = new RealmThread(fixture.Config))
			{
				t.BeginInvoke(r =>
				{
					r.Write(() =>
					{
						var obj = r.CreateObject<KeyValueRecord>();
						obj.Key = "key";
					});
				});
				t.Invoke(r =>
				{
					Assert.NotNull(r.ObjectForPrimaryKey<KeyValueRecord>("key"));
				});
			}
		}

		[Theory]
		[Repeat(Utility.COUNT)]
		[TestMethodName]
		public async Task ThreadInvokeAsync()
		{
			string path;
			using (Utility.WithEmptyDirectory(out path))
			using (var fixture = CreateRealmsInstance(path))
			using (var t = new RealmThread(fixture.Config))
			{
				await t.InvokeAsync(async r =>
				{
					await r.WriteAsync((aNewRealm) =>
					{
						var obj = aNewRealm.CreateObject<KeyValueRecord>();
						obj.Key = "key";
					});
				});
				t.Invoke(r =>
				{
					Assert.NotNull(r.ObjectForPrimaryKey<KeyValueRecord>("key"));
				});
				fixture.Refresh();
				Assert.NotNull(fixture.ObjectForPrimaryKey<KeyValueRecord>("key"));
			}
		}

		[Theory]
		[Repeat(Utility.COUNT)]
		[TestMethodName]
		public async Task ThreadInvokeAsync2()
		{
			string path;
			using (Utility.WithEmptyDirectory(out path))
			using (var fixture = CreateRealmsInstance(path))
			using (var t = new RealmThread(fixture.Config))
			{
				await t.InvokeAsync(async r =>
				{
					await Task.FromResult(true); // Simulate some Task, i.e. a httpclient request.... 
					r.Write(() =>
					{
						var obj = r.CreateObject<KeyValueRecord>();
						obj.Key = "key";
					});
				});
				t.Invoke(r =>
				{
					Assert.NotNull(r.ObjectForPrimaryKey<KeyValueRecord>("key"));
				});
				fixture.Refresh();
				Assert.NotNull(fixture.ObjectForPrimaryKey<KeyValueRecord>("key"));
			}
		}

		protected class TestMethodNameAttribute : BeforeAfterTestAttribute
		{
			public override void Before(MethodInfo methodUnderTest)
			{
				var x = GetType().FullName.Replace("+TestMethodNameAttribute", "") + ".";
				nameOfRunningTest = x + methodUnderTest.Name;
			}

			public override void After(MethodInfo methodUnderTest)
			{
			}
		}

		public void Dispose()
		{
		}

	}

	public class RealmThreadBasic : Basic, IDisposable
	{
		protected override Realms.Realm CreateRealmsInstance(string path)
		{
			return Realms.Realm.GetInstance(Path.Combine(path, "realm.db"));
		}
	}

}
