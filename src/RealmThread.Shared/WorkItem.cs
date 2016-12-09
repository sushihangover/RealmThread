using System;
using System.Threading;
using System.Threading.Tasks;

namespace SushiHangover
{
	public class WorkItem<T, RO>
	{
		readonly public Action<T> WorkAction;
		readonly public Func<T, Task> WorkFunc;
		readonly public ManualResetEventSlim CompleteEvent;
		readonly public Action<Task<RO>> ForeignTask;

		public WorkItem(Action<T> action)
		{
			WorkAction = action;
		}

		public WorkItem(Action<T> action, ManualResetEventSlim completeEvent)
		{
			WorkAction = action;
			CompleteEvent = completeEvent;
		}

		public WorkItem(Func<T, Task> func, ManualResetEventSlim completeEvent, Action<Task<RO>> task)
		{
			WorkFunc = func;
			CompleteEvent = completeEvent;
			ForeignTask = task;
		}
	}

}
