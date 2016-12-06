using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Xunit;
using D = System.Diagnostics.Debug;

namespace SushiHangover.Tests
{
	public class UserObject
	{
		public string Bio { get; set; }
		public string Name { get; set; }
		public string Blog { get; set; }
	}

	public class UserModel
	{
		UserObject _user;
		public UserModel(UserObject user)
		{
			_user = user;
		}

		public string Name { get; set; }
		public int Age { get; set; }
	}

	public class ServiceProvider : IServiceProvider
	{
		public object GetService(Type t)
		{
			if (t == typeof(UserModel))
			{
				return new UserModel(new UserObject());
			}
			return null;
		}
	}

	public abstract class BlobCacheExtensionsFixture
	{
		protected abstract IBlobCache CreateBlobCache(string path);

		[Fact]
		public async Task DownloadUrlTest()
		{
			string path;

			using (Utility.WithEmptyDirectory(out path))
			{
				var fixture = CreateBlobCache(path);
				using (fixture)
				{
					var bytes = await fixture.DownloadUrl(@"https://httpbin.org/html");
					Assert.True(bytes.Length > 0);
				}
			}
		}

		[Fact]
		public async Task GettingNonExistentKeyShouldThrow()
		{
			string path;
			using (Utility.WithEmptyDirectory(out path))
			using (var fixture = CreateBlobCache(path))
			{
				Exception thrown = null;
				try
				{
					#pragma warning disable CS0219
					var result = await fixture.GetObject<UserObject>("WEIFJWPIEFJ")
						.Timeout(TimeSpan.FromSeconds(3));
					#pragma warning restore CS0219
				}
				catch (Exception ex)
				{
					thrown = ex;
				}

				Assert.True(thrown.GetType() == typeof(KeyNotFoundException));
			}
		}

		[Fact]
		public async Task ObjectsShouldBeRoundtrippable()
		{
			string path;
			var input = new UserObject { Bio = "A totally cool cat!", Name = "octocat", Blog = "http://www.github.com" };
			UserObject result;

			using (Utility.WithEmptyDirectory(out path))
			{
				using (var fixture = CreateBlobCache(path))
				{
					if (fixture is InMemoryBlobCache) return;
					await fixture.InsertObject("key", input);
				}

				using (var fixture = CreateBlobCache(path))
				{
					result = await fixture.GetObject<UserObject>("key");
				}
			}

			Assert.Equal(input.Blog, result.Blog);
			Assert.Equal(input.Bio, result.Bio);
			Assert.Equal(input.Name, result.Name);
		}

		[Fact]
		public async Task ArraysShouldBeRoundtrippable()
		{
			string path;
			var input = new[] { new UserObject { Bio = "A totally cool cat!", Name = "octocat", Blog = "https://www.github.com" }, new UserObject { Bio = "zzz", Name = "sleepy", Blog = "http://example.com" } };
			UserObject[] result;

			using (Utility.WithEmptyDirectory(out path))
			{
				using (var fixture = CreateBlobCache(path))
				{
					if (fixture is InMemoryBlobCache) return;

					await fixture.InsertObject("key", input);
				}

				using (var fixture = CreateBlobCache(path))
				{
					result = await fixture.GetObject<UserObject[]>("key");
				}
			}

			Assert.Equal(input.First().Blog, result.First().Blog);
			Assert.Equal(input.First().Bio, result.First().Bio);
			Assert.Equal(input.First().Name, result.First().Name);
			Assert.Equal(input.Last().Blog, result.Last().Blog);
			Assert.Equal(input.Last().Bio, result.Last().Bio);
			Assert.Equal(input.Last().Name, result.Last().Name);
		}

		[Fact]
		public async Task ObjectsCanBeCreatedUsingObjectFactory()
		{
			string path;
			var input = new UserModel(new UserObject()) { Age = 123, Name = "Old" };
			UserModel result;

			using (Utility.WithEmptyDirectory(out path))
			{
				using (var fixture = CreateBlobCache(path))
				{
					if (fixture is InMemoryBlobCache) return;

					await fixture.InsertObject("key", input);
				}

				using (var fixture = CreateBlobCache(path))
				{
					result = await fixture.GetObject<UserModel>("key");
				}
			}

			Assert.Equal(input.Age, result.Age);
			Assert.Equal(input.Name, result.Name);
		}

		[Fact]
		public async Task ArraysShouldBeRoundtrippableUsingObjectFactory()
		{
			string path;
			var input = new[] { new UserModel(new UserObject()) { Age = 123, Name = "Old" }, new UserModel(new UserObject()) { Age = 123, Name = "Old" } };
			UserModel[] result;
			using (Utility.WithEmptyDirectory(out path))
			{
				using (var fixture = CreateBlobCache(path))
				{
					if (fixture is InMemoryBlobCache) return;

					await fixture.InsertObject("key", input);
				}

				using (var fixture = CreateBlobCache(path))
				{
					result = await fixture.GetObject<UserModel[]>("key");
				}
			}

			Assert.Equal(input.First().Age, result.First().Age);
			Assert.Equal(input.First().Name, result.First().Name);
			Assert.Equal(input.Last().Age, result.Last().Age);
			Assert.Equal(input.Last().Name, result.Last().Name);
		}

		//[Fact]
		[Fact(Skip = "Crashes Realm/App")]
		public async Task GetOrFetchObject()
		{	
			// FIXME: Fixed now? Updated many packages, so not sure what the origin is/was...
			// Enabling test for now.

			// [Fact(Skip = "Hangs in xUnit Device Runner")]
			// Test runs to completation with no asserts, does not show pass/failed, 
			// and appears to hang within the application looper
			// Need to determine if thus due to an xUnit Device Runner issue or actually an issue.
			        
			int fetchCount = 0;
			var fetcher = new Func<IObservable<Tuple<string, string>>>(() =>
			{
				fetchCount++;
				return Observable.Return(new Tuple<string, string>("Foo", "Bar"));
			});

			string path;
			using (Utility.WithEmptyDirectory(out path))
			{
				using (var fixture = CreateBlobCache(path))
				{
					var result = await fixture.GetOrFetchObject("Test", fetcher);
					Assert.Equal("Foo", result.Item1);
					Assert.Equal("Bar", result.Item2);
					Assert.Equal(1, fetchCount);

					 //Does object exist in the cache now?
					var obj = await fixture.GetObject<Tuple<string, string>>("Test");
					Assert.NotNull(obj); // Should throw a no key exception **before** this Assert if it does not exist

					 //2nd time around, we should be grabbing from cache
					result = await fixture.GetOrFetchObject("Test", fetcher);
					Assert.Equal("Foo", result.Item1);
					Assert.Equal("Bar", result.Item2);
					Assert.Equal(1, fetchCount);

					// Testing persistence makes zero sense for InMemoryBlobCache
					if (fixture is InMemoryBlobCache) return;
				}

				using (var fixture = CreateBlobCache(path))
				{
					var result = await fixture.GetOrFetchObject("Test", fetcher);
					Assert.Equal("Foo", result.Item1);
					Assert.Equal("Bar", result.Item2);
					Assert.Equal(1, fetchCount);
				}
			}
			// Testing that we get here: https://github.com/xunit/xunit/issues/866 see notes are top of test
			Assert.True(true);
		}

		//[Fact(Skip = "TestScheduler tests aren't gonna work with new SQLite")]
		//public void FetchFunctionShouldDebounceConcurrentRequests()
		//{
		//    (new TestScheduler()).With(sched =>
		//    {
		//        string path;
		//        using (Utility.WithEmptyDirectory(out path))
		//        {
		//            int callCount = 0;
		//            var fetcher = new Func<IObservable<int>>(() => 
		//            {
		//                callCount++;
		//                return Observable.Return(42).Delay(TimeSpan.FromMilliseconds(1000), sched);
		//            });

		//            var fixture = CreateBlobCache(path);
		//            try
		//            {
		//                var result1 = fixture.GetOrFetchObject("foo", fetcher).CreateCollection();

		//                Assert.Equal(0, result1.Count);

		//                sched.AdvanceToMs(250);

		//                // Nobody's returned yet, cache is empty, we should have called the fetcher
		//                // once to get a result
		//                var result2 = fixture.GetOrFetchObject("foo", fetcher).CreateCollection();
		//                Assert.Equal(0, result1.Count);
		//                Assert.Equal(0, result2.Count);
		//                Assert.Equal(1, callCount);

		//                sched.AdvanceToMs(750);

		//                // Same as above, result1-3 are all listening to the same fetch
		//                var result3 = fixture.GetOrFetchObject("foo", fetcher).CreateCollection();
		//                Assert.Equal(0, result1.Count);
		//                Assert.Equal(0, result2.Count);
		//                Assert.Equal(0, result3.Count);
		//                Assert.Equal(1, callCount);

		//                // Fetch returned, all three collections should have an item
		//                sched.AdvanceToMs(1250);
		//                Assert.Equal(1, result1.Count);
		//                Assert.Equal(1, result2.Count);
		//                Assert.Equal(1, result3.Count);
		//                Assert.Equal(1, callCount);

		//                // Making a new call, but the cache has an item, this shouldn't result
		//                // in a fetcher call either
		//                var result4 = fixture.GetOrFetchObject("foo", fetcher).CreateCollection();
		//                sched.AdvanceToMs(2500);
		//                Assert.Equal(1, result1.Count);
		//                Assert.Equal(1, result2.Count);
		//                Assert.Equal(1, result3.Count);
		//                Assert.Equal(1, result4.Count);
		//                Assert.Equal(1, callCount);

		//                // Making a new call, but with a new key - this *does* result in a fetcher
		//                // call. Result1-4 shouldn't get any new items, and at t=3000, we haven't
		//                // returned from the call made at t=2500 yet
		//                var result5 = fixture.GetOrFetchObject("bar", fetcher).CreateCollection();
		//                sched.AdvanceToMs(3000);
		//                Assert.Equal(1, result1.Count);
		//                Assert.Equal(1, result2.Count);
		//                Assert.Equal(1, result3.Count);
		//                Assert.Equal(1, result4.Count);
		//                Assert.Equal(0, result5.Count);
		//                Assert.Equal(2, callCount);

		//                // Everything is done, we should have one item in result5 now
		//                sched.AdvanceToMs(4000);
		//                Assert.Equal(1, result1.Count);
		//                Assert.Equal(1, result2.Count);
		//                Assert.Equal(1, result3.Count);
		//                Assert.Equal(1, result4.Count);
		//                Assert.Equal(1, result5.Count);
		//                Assert.Equal(2, callCount);
		//            }
		//            finally
		//            {
		//                // Since we're in TestScheduler, we can't use the normal 
		//                // using statement, we need to kick off the async dispose,
		//                // then start the scheduler to let it run
		//                fixture.Dispose();
		//                sched.Start();
		//            }
		//        }
		//    });
		//}

		[Fact]
		public async Task FetchFunctionShouldPropagateThrownExceptionAsObservableException()
		{
			var fetcher = new Func<IObservable<Tuple<string, string>>>(() =>
			{
				throw new InvalidOperationException();
			});

			string path;
			using (Utility.WithEmptyDirectory(out path))
			{
				using (var fixture = CreateBlobCache(path))
				{
					var result = await fixture.GetOrFetchObject("Test", fetcher)
						  .Catch(Observable.Return(new Tuple<string, string>("one", "two")));
					Assert.Equal("one", result.Item1);
					Assert.Equal("two", result.Item2);
				}
			}
		}

		[Fact]
		public async Task FetchFunctionShouldPropagateObservedExceptionAsObservableException()
		{
			var fetcher = new Func<IObservable<Tuple<string, string>>>(() =>
				Observable.Throw<Tuple<string, string>>(new InvalidOperationException()));

			string path;
			using (Utility.WithEmptyDirectory(out path))
			{
				var fixture = CreateBlobCache(path);
				using (fixture)
				{
					var result = await fixture.GetOrFetchObject("Test", fetcher)
											  .Catch(Observable.Return(new Tuple<string, string>("one", "two")));
					Assert.Equal("one", result.Item1);
					Assert.Equal("two", result.Item2);
				}
			}
		}

		//[Fact(Skip = "TestScheduler tests aren't gonna work with new SQLite")]
		//public void GetOrFetchShouldRespectExpiration()
		//{
		//    (new TestScheduler()).With(sched => 
		//    {
		//        string path;
		//        using (Utility.WithEmptyDirectory(out path))
		//        {
		//            var fixture = CreateBlobCache(path);
		//            using (fixture)
		//            {
		//                var result = default(string);
		//                fixture.GetOrFetchObject("foo",
		//                    () => Observable.Return("bar"),
		//                    sched.Now + TimeSpan.FromMilliseconds(1000))
		//                    .Subscribe(x => result = x);

		//                sched.AdvanceByMs(250);
		//                Assert.Equal("bar", result);

		//                fixture.GetOrFetchObject("foo",
		//                    () => Observable.Return("baz"),
		//                    sched.Now + TimeSpan.FromMilliseconds(1000))
		//                    .Subscribe(x => result = x);

		//                sched.AdvanceByMs(250);
		//                Assert.Equal("bar", result);

		//                sched.AdvanceByMs(1000);
		//                fixture.GetOrFetchObject("foo",
		//                    () => Observable.Return("baz"),
		//                    sched.Now + TimeSpan.FromMilliseconds(1000))
		//                    .Subscribe(x => result = x);

		//                sched.AdvanceByMs(250);
		//                Assert.Equal("baz", result);
		//            }
		//        }
		//    });
		//}

		[Fact]
		public async Task GetAndFetchLatestShouldInvalidateObjectOnError()
		{
			var fetcher = new Func<IObservable<string>>(() =>
			{
				return Observable.Throw<string>(new InvalidOperationException());
			});

			string path;
			using (Utility.WithEmptyDirectory(out path))
			{
				var fixture = CreateBlobCache(path);

				using (fixture)
				{
					if (fixture is InMemoryBlobCache) return;

					await fixture.InsertObject("foo", "bar");

					await fixture.GetAndFetchLatest("foo", fetcher, shouldInvalidateOnError: true)
						.Catch(Observable.Return("get and fetch latest error"))
						.ToList();

					var result = await fixture.GetObject<string>("foo")
						.Catch(Observable.Return("get error"));

					Assert.Equal("get error", result);
				}
			}
		}

		[Fact]
		public async Task GetAndFetchLatestCallsFetchPredicate()
		{
			var fetchPredicateCalled = false;

			Func<DateTimeOffset, bool> fetchPredicate = d =>
			{
				fetchPredicateCalled = true;

				return true;
			};

			var fetcher = new Func<IObservable<string>>(() => Observable.Return("baz"));

			string path;
			using (Utility.WithEmptyDirectory(out path))
			{
				var fixture = CreateBlobCache(path);

				using (fixture)
				{
					if (fixture is InMemoryBlobCache) return;

					await fixture.InsertObject("foo", "bar");
					await fixture.GetAndFetchLatest("foo", fetcher, fetchPredicate);
					Assert.True(fetchPredicateCalled);
				}
			}
		}

		[Fact]
		public async Task KeysByTypeTest()
		{
			string path;
			var input = new[]
			{
				"Foo",
				"Bar",
				"Baz"
			};

			var inputItems = input.Select(x => new UserObject { Name = x, Bio = "A thing" }).ToArray();
			var fixture = default(IBlobCache);

			using (Utility.WithEmptyDirectory(out path))
			using (fixture = CreateBlobCache(path))
			{
				foreach (var item in input.Zip(inputItems, (Key, Value) => new { Key, Value }))
				{
					await fixture.InsertObject(item.Key, item.Value);
				}

				var allObjectsCount = await fixture.GetAllObjects<UserObject>().Select(x => x.Count());
				Assert.Equal(input.Length, (await fixture.GetAllKeys()).Count());
				Assert.Equal(input.Length, allObjectsCount);

				await fixture.InsertObject("Quux", new UserModel(null));

				allObjectsCount = await fixture.GetAllObjects<UserObject>().Select(x => x.Count());
				Assert.Equal(input.Length + 1, (await fixture.GetAllKeys()).Count());
				Assert.Equal(input.Length, allObjectsCount);

				await fixture.InvalidateObject<UserObject>("Foo");

				allObjectsCount = await fixture.GetAllObjects<UserObject>().Select(x => x.Count());
				Assert.Equal(input.Length + 1 - 1, (await fixture.GetAllKeys()).Count());
				Assert.Equal(input.Length - 1, allObjectsCount);

				await fixture.InvalidateAllObjects<UserObject>();

				allObjectsCount = await fixture.GetAllObjects<UserObject>().Select(x => x.Count());
				Assert.Equal(1, (await fixture.GetAllKeys()).Count());
				Assert.Equal(0, allObjectsCount);
			}
		}

		[Fact]
		public async Task GetAllKeysSmokeTest()
		{
			string path;

			using (Utility.WithEmptyDirectory(out path))
			{
				var fixture = default(IBlobCache);
				using (fixture = CreateBlobCache(path))
				{
					await Observable.Merge(
						fixture.InsertObject("Foo", "bar"),
						fixture.InsertObject("Bar", 10),
						fixture.InsertObject("Baz", new UserObject { Bio = "Bio", Blog = "Blog", Name = "Name" })
					);

					var keys = await fixture.GetAllKeys();
					Assert.Equal(3, keys.Count());
					Assert.True(keys.Any(x => x.Contains("Foo")));
					Assert.True(keys.Any(x => x.Contains("Bar")));
				}

				if (fixture is InMemoryBlobCache) return;

				using (fixture = CreateBlobCache(path))
				{
					var keys = await fixture.GetAllKeys();
					Assert.Equal(3, keys.Count());
					Assert.True(keys.Any(x => x.Contains("Foo")));
					Assert.True(keys.Any(x => x.Contains("Bar")));
				}
			}
		}
	}

#if (INMEMORY && !PERF)

	public class InMemoryBlobCacheFixture : BlobCacheExtensionsFixture
	{
		protected override IBlobCache CreateBlobCache(string path)
		{
			BlobCache.ApplicationName = "TestRunner";
			//return new InMemoryBlobCache(RxApp.MainThreadScheduler);
			return new InMemoryBlobCache();
		}
	}

#endif

#if (SQLITE && !PERF)

	public class SqliteBlobExtFixture : BlobCacheExtensionsFixture
	{
		protected override IBlobCache CreateBlobCache(string path)
		{
			BlobCache.ApplicationName = "TestRunner";
			return new BlockingDisposeObjectCache(new SQLiteBlobCache(Path.Combine(path, "sqlite.db")));
		}

		[Fact]
		public async Task VacuumCompactDatabase()
		{
			string path;

			using (Utility.WithEmptyDirectory(out path))
			{
				var dbPath = Path.Combine(path, "sqlite.db");

				using (var fixture = new BlockingDisposeCache(CreateBlobCache(path)))
				{
					Assert.True(File.Exists(dbPath));

					byte[] buf = new byte[256 * 1024];
					var rnd = new Random();
					rnd.NextBytes(buf);

					await fixture.Insert("dummy", buf);
				}

				var size = new FileInfo(dbPath).Length;
				Assert.True(size > 0);

				using (var fixture = new BlockingDisposeCache(CreateBlobCache(path)))
				{
					await fixture.InvalidateAll();
					await fixture.Vacuum();
				}

				Assert.True(new FileInfo(dbPath).Length < size);
			}
		}
	}

	public class EncryptedSqliteBlobExtFixture : BlobCacheExtensionsFixture
	{
		protected override IBlobCache CreateBlobCache(string path)
		{
			BlobCache.ApplicationName = "TestRunner";
			return new BlockingDisposeObjectCache(new SQLiteEncryptedBlobCache(Path.Combine(path, "sqlite.db")));
		}
	}

#endif

#if (REALM && !PERF)

	public class RealmBlobExtFixture : BlobCacheExtensionsFixture
	{
		protected override IBlobCache CreateBlobCache(string path)
		{
			BlobCache.ApplicationName = "TestRunner";
			return new BlockingDisposeObjectCache(new RealmBlobCache(Path.Combine(path, "realm.db")));
		}

		[Fact]
		public async Task VacuumDatabase()
		{
			// https://realm.io/docs/xamarin/latest/#deleting-objects
			// Note: the Realm file will maintain its size on disk to efficiently reuse that space for future objects.
			string path;

			using (Utility.WithEmptyDirectory(out path))
			{
				var dbPath = Path.Combine(path, "realm.db");

				using (var fixture = new BlockingDisposeCache(CreateBlobCache(path)))
				{
					Assert.True(File.Exists(dbPath));

					byte[] buf = new byte[256 * 1024];
					var rnd = new Random();
					rnd.NextBytes(buf);

					await fixture.Insert("dummy", buf);
				}
				var size = new FileInfo(dbPath).Length;
				Assert.True(size > 0);

				using (var fixture = new BlockingDisposeCache(CreateBlobCache(path)))
				{
					await fixture.InvalidateAll();
					await fixture.Vacuum();
				}
				var size2 = new FileInfo(dbPath).Length;
				Assert.True(size2 <= size);
			}
		}
	}

	//public class EncryptedRealmBlobExtFixture : BlobCacheExtensionsFixture
	//{
	//	protected override IBlobCache CreateBlobCache(string path)
	//	{
	//		BlobCache.ApplicationName = "TestRunner";
	//		return new BlockingDisposeObjectCache(new RealmEncryptedBlobCache(Path.Combine(path, "sqlite.db")));
	//	}
	//}

#endif

	class BlockingDisposeCache : IBlobCache
	{
		protected readonly IBlobCache _inner;
		public BlockingDisposeCache(IBlobCache cache)
		{
			BlobCache.EnsureInitialized();
			_inner = cache;
		}

		public virtual void Dispose()
		{
			_inner.Dispose();
			_inner.Shutdown.Wait();
		}

		public IObservable<Unit> Insert(string key, byte[] data, DateTimeOffset? absoluteExpiration = null)
		{
			return _inner.Insert(key, data, absoluteExpiration);
		}

		public IObservable<byte[]> Get(string key)
		{
			return _inner.Get(key);
		}

		public IObservable<IEnumerable<string>> GetAllKeys()
		{
			return _inner.GetAllKeys();
		}

		public IObservable<DateTimeOffset?> GetCreatedAt(string key)
		{
			return _inner.GetCreatedAt(key);
		}

		public IObservable<Unit> Flush()
		{
			return _inner.Flush();
		}

		public IObservable<Unit> Invalidate(string key)
		{
			return _inner.Invalidate(key);
		}

		public IObservable<Unit> InvalidateAll()
		{
			return _inner.InvalidateAll();
		}

		public IObservable<Unit> Vacuum()
		{
			return _inner.Vacuum();
		}

		public IObservable<Unit> Shutdown
		{
			get { return _inner.Shutdown; }
		}

		public IScheduler Scheduler
		{
			get { return _inner.Scheduler; }
		}
	}

	class BlockingDisposeObjectCache : BlockingDisposeCache, IObjectBlobCache
	{
		public BlockingDisposeObjectCache(IObjectBlobCache cache) : base(cache) { }

		public IObservable<Unit> InsertObject<T>(string key, T value, DateTimeOffset? absoluteExpiration = null)
		{
			return ((IObjectBlobCache)_inner).InsertObject(key, value, absoluteExpiration);
		}

		public IObservable<T> GetObject<T>(string key)
		{
			return ((IObjectBlobCache)_inner).GetObject<T>(key);
		}

		public IObservable<IEnumerable<T>> GetAllObjects<T>()
		{
			return ((IObjectBlobCache)_inner).GetAllObjects<T>();
		}

		public IObservable<Unit> InvalidateObject<T>(string key)
		{
			return ((IObjectBlobCache)_inner).InvalidateObject<T>(key);
		}

		public IObservable<Unit> InvalidateAllObjects<T>()
		{
			return ((IObjectBlobCache)_inner).InvalidateAllObjects<T>();
		}

		public IObservable<DateTimeOffset?> GetObjectCreatedAt<T>(string key)
		{
			return ((IObjectBlobCache)_inner).GetObjectCreatedAt<T>(key);
		}
	}
}
