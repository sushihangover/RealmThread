using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace SushiHangover
{
	/// <summary>
	/// A Realm Action Pump
	/// </summary>
	public class RealmThread : IDisposable
	{
		readonly BlockingCollection<RealmWork> workQueue;
		readonly InternalThread realmThread;
		public int ManagedThreadId
		{
			get { return realmThread.ManagedThreadId; }
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:SushiHangover.RealmThread"/> class.
		/// </summary>
		/// <param name="realm">Realm.</param>
		public RealmThread(Realms.Realm realm)
		{
			////D.WriteLine("RealmThread Constructor");
			workQueue = new BlockingCollection<RealmWork>();
			realmThread = new InternalThread(workQueue);
			realmThread.Start(realm);
		}

		/// <summary>
		/// Invokes a "Fire & Forget" Action on an independent Realm thread.
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
			realmThread.Dispose();
		}
	}

	class InternalThread : IDisposable
	{
		readonly Thread _thread;
		readonly BlockingCollection<RealmWork> _workQueue;
		int _Id;
		public int ManagedThreadId
		{
			get { return _Id; }
		}

		public InternalThread(BlockingCollection<RealmWork> workQueue)
		{
			_workQueue = workQueue;
			_thread = new Thread(new ParameterizedThreadStart(Run));
			_thread.Name = "Realm Worker Thread";
		}

		public void Start(object realm)
		{
			_thread?.Start(realm);
		}

		public void Join()
		{
			_thread?.Join();
		}

		public void Run(object parentRealm)
		{
			_Id = Thread.CurrentThread.ManagedThreadId;
			using (var localRealm = Realms.Realm.GetInstance((parentRealm as Realms.Realm).Config))
			{

				//D.WriteLine($"RealmThread Starting Thread: {_Id}");
				foreach (var workItem in _workQueue.GetConsumingEnumerable())
				{
					//D.WriteLine($"RealmThread Start Action: {_Id}");
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
								workItem.ForeignTask((Task<Realms.RealmObject>)t);
							}
						}
					}
					finally
					{
						SynchronizationContext.SetSynchronizationContext(prevCtx);
					}
					//D.WriteLine($"RealmThread Finish Action: {_Id}");
				}
				//D.WriteLine($"RealmThread Stopping Thread: {_Id}");
			}
		}

		public void Stop()
		{
			_workQueue.CompleteAdding();
		}

		public void Dispose()
		{
			if (_workQueue != null && !_workQueue.IsCompleted)
			{
				_workQueue.CompleteAdding();
			}
			_thread.Join();
			_workQueue.Dispose();
		}
	}

}
