using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using D = System.Diagnostics.Debug;

// https://blogs.msdn.microsoft.com/pfxteam/2012/01/20/await-synchronizationcontext-and-console-apps/

namespace SushiHangover
{
	/// <summary>Provides a SynchronizationContext that's single-threaded.</summary>
	sealed class SingleThreadSynchronizationContext : SynchronizationContext
	{
		/// <summary>The queue of work items.</summary>
		private readonly BlockingCollection<KeyValuePair<SendOrPostCallback, object>> m_queue =
			new BlockingCollection<KeyValuePair<SendOrPostCallback, object>>();
		
		/// <summary>The processing thread.</summary>
		//private readonly Thread m_thread = Thread.CurrentThread;

		/// <summary>The number of outstanding operations.</summary>
		private int m_operationCount = 0;
		/// <summary>Whether to track operations m_operationCount.</summary>
		private readonly bool m_trackOperations;

		/// <summary>Initializes the context.</summary>
		/// <param name="trackOperations">Whether to track operation count.</param>
		internal SingleThreadSynchronizationContext(bool trackOperations)
		{
			m_trackOperations = trackOperations;
		}

		/// <summary>Dispatches an asynchronous message to the synchronization context.</summary>
		/// <param name="d">The System.Threading.SendOrPostCallback delegate to call.</param>
		/// <param name="state">The object passed to the delegate.</param>
		public override void Post(SendOrPostCallback d, object state)
		{
			if (d == null) throw new ArgumentNullException("d");
			m_queue.Add(new KeyValuePair<SendOrPostCallback, object>(d, state));
		}

		/// <summary>Not supported.</summary>
		public override void Send(SendOrPostCallback d, object state)
		{
			throw new NotSupportedException("Synchronously sending is not supported.");
		}

		/// <summary>Runs an loop to process all queued work items.</summary>
		public void RunOnCurrentThread()
		{
			foreach (var workItem in m_queue.GetConsumingEnumerable())
				workItem.Key(workItem.Value);
		}

		/// <summary>Notifies the context that no more work will arrive.</summary>
		public void Complete() { m_queue.CompleteAdding(); }

		/// <summary>Invoked when an async operation is started.</summary>
		public override void OperationStarted()
		{
			if (m_trackOperations)
				Interlocked.Increment(ref m_operationCount);
		}

		/// <summary>Invoked when an async operation is completed.</summary>
		public override void OperationCompleted()
		{
			if (m_trackOperations && Interlocked.Decrement(ref m_operationCount) == 0)
				Complete();
		}
	}
}
