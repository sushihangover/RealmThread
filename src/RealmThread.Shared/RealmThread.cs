using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Realms;

namespace SushiHangover
{
	/// <summary>
	/// A Realm Action Pump
	/// </summary>
	public class RealmThread : IDisposable
	{
		readonly BlockingCollection<RealmWork> workQueue;
		readonly InternalThread realmThread;
		readonly bool autoCommmitOnDispose;

		/// <summary>
		/// Gets the managed thread identifier.
		/// </summary>
		/// <value>The managed thread identifier.</value>
		public int ManagedThreadId
		{
			get { return realmThread.ManagedThreadId; }
		}

		/// <summary>
		/// Gets a value indicating whether this <see cref="T:SushiHangover.RealmThread"/> is in transaction.
		/// </summary>
		/// <value><c>true</c> if in transaction; otherwise, <c>false</c>.</value>
		public bool InTransaction
		{
			get { return realmThread.InTransaction; }
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:SushiHangover.RealmThread"/> class.
		/// </summary>
		/// <param name="realmConfig">RealmConfiguration</param>
		public RealmThread(RealmConfigurationBase realmConfig) : this(realmConfig, false)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:SushiHangover.RealmThread"/> class.
		/// </summary>
		/// <param name="realmConfig">RealmConfiguration</param>
		/// <param name="autoCommmit">If set to <c>true</c> auto commmit open transaction on Dispose</param>
		public RealmThread(RealmConfigurationBase realmConfig, bool autoCommmit)
		{
			autoCommmitOnDispose = autoCommmit;
			workQueue = new BlockingCollection<RealmWork>();
			realmThread = new InternalThread(workQueue);
			realmThread.Start(realmConfig);
		}

		/// <summary>
		/// Invokes a 'Fire and Forget' Action on an independent Realm thread.
		/// </summary>
		/// <param name="action">Action.</param>
		public void BeginInvoke(Action<Realms.Realm> action)
		{
			if (action == null) throw new ArgumentNullException(nameof(action));

			var workItem = new RealmWork(action);
			workQueue.Add(workItem);
		}

		/// <summary>
		/// Invoke the specified action on an independent Realm thread.
		/// </summary>
		/// <param name="action">Action.</param>
		public void Invoke(Action<Realms.Realm> action)
		{
			if (action == null) throw new ArgumentNullException(nameof(action));

			using (var waitEvent = new ManualResetEventSlim(false))
			{
				var workItem = new RealmWork(action, waitEvent);
				workQueue.Add(workItem);
				waitEvent.Wait();
			}
		}

		/// <summary>
		/// Invoke an Task on an independent Realm thread.
		/// </summary>
		/// <returns>Task</returns>
		/// <param name="func">Func.</param>
		public Task InvokeAsync(Func<Realms.Realm, Task> func)
		{
			if (func == null) throw new ArgumentNullException(nameof(func));

			using (var wait = new ManualResetEventSlim(false))
			{
				var workItem = new RealmWork(func, wait, null);
				workQueue.Add(workItem);
				wait.Wait();
				return Task.FromResult(true);
			}
		}

		/// <summary>
		/// Begin a Realm transaction on this thread.
		/// </summary>
		public void BeginTransaction()
		{
			if (InTransaction) throw new RealmThreadInTransaction("Currently this RealmThread is already in a transaction");

			Action<Realms.Realm> action = (r) =>
			{
				realmThread.trans = r.BeginWrite();
			};

			using (var waitEvent = new ManualResetEventSlim(false))
			{
				var workItem = new RealmWork(action, waitEvent);
				workQueue.Add(workItem);
				waitEvent.Wait();
			}
		}

		/// <summary>
		/// Commits the Realm transaction that is active on this thread.
		/// </summary>
		public void CommitTransaction()
		{
			if (!InTransaction) throw new RealmThreadNotInTransaction("No active transaction on this thread");

			Action<Realms.Realm> action = r =>
			{
				try
				{
					realmThread.trans.Commit();
				}
				finally
				{
					realmThread.trans = null;
				}
			};

			using (var waitEvent = new ManualResetEventSlim(false))
			{
				var workItem = new RealmWork(action, waitEvent);
				workQueue.Add(workItem);
				waitEvent.Wait();
			}
		}

		/// <summary>
		/// Rollbacks the Ream transaction that is active on this thread.
		/// </summary>
		public void RollbackTransaction()
		{
			if (!InTransaction) throw new RealmThreadNotInTransaction("No active transaction on this thread");

			Action<Realms.Realm> action = (r) =>
			{
				try
				{
					realmThread.trans.Rollback();
				}
				finally
				{
					realmThread.trans = null;
				}
			};

			using (var waitEvent = new ManualResetEventSlim(false))
			{
				var workItem = new RealmWork(action, waitEvent);
				workQueue.Add(workItem);
				waitEvent.Wait();
			}
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

		public static Realms.Realm GetInstance(RealmConfiguration config)
		{
			return GetInstance((RealmConfigurationBase)config);
		}

		public static Realms.Realm GetInstance(string databasePath)
		{
			var config = new RealmConfiguration(databasePath);
			return GetInstance(config);
		}

		// TODO: How would you marshall a RealmObject/RealmResults across the thread? 
		// Create a POCO from a RealmObject? 
		// or an IEnumerator<POCO> from a RealmResult?
		//public Task<T> GetAsync<T>(Func<Realms.Realm, Task> asyncMethod) where T : Realms.RealmObject
		//{
		//	if (asyncMethod == null) throw new ArgumentNullException(nameof(asyncMethod));

		//	using (var wait = new ManualResetEventSlim(false))
		//	{
		//		Realms.RealmObject foreignResult = default(Realms.RealmObject);
		//		Action<Task<Realms.RealmObject>> capturedResult = (Task<Realms.RealmObject> obj) =>
		//		{
		//			foreignResult = obj.GetAwaiter().GetResult();
		//		};
		//		var workItem = new RealmWork(asyncMethod, wait, capturedResult);
		//		workQueue.Add(workItem);
		//		wait.Wait();
		//		return Task.FromResult((T)foreignResult);
		//	}
		//}

		/// <summary>
		/// Releases all resource used by the <see cref="T:SushiHangover.RealmThread"/> object.
		/// </summary>
		/// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="T:SushiHangover.RealmThread"/>. The
		/// <see cref="Dispose"/> method leaves the <see cref="T:SushiHangover.RealmThread"/> in an unusable state. After
		/// calling <see cref="Dispose"/>, you must release all references to the <see cref="T:SushiHangover.RealmThread"/> so
		/// the garbage collector can reclaim the memory that the <see cref="T:SushiHangover.RealmThread"/> was occupying.</remarks>
		public void Dispose()
		{
			if (InTransaction)
			{
				if (autoCommmitOnDispose) CommitTransaction(); else RollbackTransaction();
			}
			realmThread.Dispose();
		}
	}

	class InternalThread : IDisposable
	{
		readonly Thread thread;
		readonly BlockingCollection<RealmWork> workQueue;
		int managedThreadId;
		public Transaction trans;

		public int ManagedThreadId
		{
			get { return managedThreadId; }
		}

		public bool InTransaction
		{
			get { return trans != null; }
		}

		public InternalThread(BlockingCollection<RealmWork> workQueue)
		{
			this.workQueue = workQueue;
			thread = new Thread(new ParameterizedThreadStart(Run));
			thread.Name = "Realm Worker Thread";
		}

		public void Start(object realm)
		{
			thread?.Start(realm);
		}

		public void Join()
		{
			thread?.Join();
		}

		public void Run(object parentRealmConfig)
		{
			managedThreadId = Thread.CurrentThread.ManagedThreadId;
			//using (var localRealm = Realms.Realm.GetInstance(parentRealmConfig as RealmConfiguration))
			using (var localRealm = RealmThread.GetInstance(parentRealmConfig as RealmConfigurationBase))
			{
				foreach (var workItem in workQueue.GetConsumingEnumerable())
				{
					localRealm.Refresh();

					var prevCtx = SynchronizationContext.Current;
					try
					{
						// Establish the new context
						var syncCtx = new SingleThreadSynchronizationContext(workItem.WorkAction != null);
						SynchronizationContext.SetSynchronizationContext(syncCtx);

						Task t = null;

						if (workItem.CompleteEvent == null)
						{
							// Invoke the function
							syncCtx.OperationStarted();
							workItem.WorkAction(localRealm);
							syncCtx.OperationCompleted();
						}
						else if ((workItem.WorkAction != null) && (workItem.CompleteEvent != null))
						{
							// Invoke the function
							syncCtx.OperationStarted();
							workItem.WorkAction(localRealm);
							syncCtx.OperationCompleted();
							workItem.CompleteEvent.Set();
						}
						else
						{
							// Invoke the function and alert the context to when it completes
							t = workItem.WorkFunc(localRealm);
							if (t == null) throw new InvalidOperationException("No task provided.");
							t.ContinueWith(delegate
							{
								syncCtx.Complete();
								workItem.CompleteEvent.Set();
							}, TaskScheduler.Default);
						}

						// Pump continuations and propagate any exceptions
						syncCtx.RunOnCurrentThread();
						if (workItem.WorkFunc != null)
						{
							if (workItem.ForeignTask == null)
							{
								t.GetAwaiter().GetResult();
							}
							else
							{
								workItem.ForeignTask((Task<RealmObject>)t);
							}
						}
					}
					finally
					{
						SynchronizationContext.SetSynchronizationContext(prevCtx);
					}
				}
			}
		}

		public void Stop()
		{
			workQueue.CompleteAdding();
		}

		public void Dispose()
		{
			if (workQueue != null && !workQueue.IsCompleted)
			{
				workQueue.CompleteAdding();
			}
			thread.Join();
			workQueue.Dispose();
		}
	}

}
