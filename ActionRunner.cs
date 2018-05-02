using System;
using System.Threading;
using System.Threading.Tasks;
using Open.Threading;

namespace Open.Threading.Tasks
{
	public class ActionRunner : ICancellable
	{
		public ActionRunner(Action action, TaskScheduler scheduler = null)
		{
			_action = action;
			_scheduler = scheduler; // No need to hold a refernce to the default, just keep it null.
			LastStart = DateTime.MaxValue;
			LastComplete = DateTime.MaxValue;
		}

		public static ActionRunner Create(Action action, TaskScheduler scheduler = null)
		{
			return new ActionRunner(action, scheduler);
		}

		public static ActionRunner Create<T>(Func<T> action, TaskScheduler scheduler = null)
		{
			return new ActionRunner(() => { action(); }, scheduler);
		}

		Action _action;
		protected TaskScheduler _scheduler;

		protected int _count;
		public int Count
		{
			get { return _count; }
		}

		public DateTime LastStart
		{
			get;
			protected set;
		}

		public bool HasBeenRun
		{
			get
			{
				return LastStart < DateTime.Now;
			}
		}

		public DateTime LastComplete
		{
			get;
			protected set;
		}

		public bool HasCompleted
		{
			get
			{
				return LastComplete < DateTime.Now;
			}
		}

		public Exception LastFault
		{
			get;
			protected set;
		}

		public bool Cancel(bool onlyIfNotRunning)
		{
			var t = _task;
			return t?.Cancel(onlyIfNotRunning) ?? false
				&& t == Interlocked.CompareExchange(ref _task, null, t);
		}

		public bool Cancel()
		{
			return Cancel(false);
		}

		public void Dispose()
		{
			Cancel();
			_action = null;
		}

		Action GetAction()
		{
			var a = _action;
			if (a == null)
				throw new ObjectDisposedException(typeof(ActionRunner).ToString());
			return a;
		}

		public bool IsScheduled
		{
			get
			{
				return _task?.IsActive() ?? false;
			}
		}

		/// <summary>
		/// Indiscriminately invokes the action.
		/// </summary>
		public void RunSynchronously()
		{
			GetAction().Invoke();
		}

		readonly object _taskLock = new object();
		CancellableTask _task;
		CancellableTask Prepare()
		{
			LastStart = DateTime.Now;
			var task = CancellableTask.Init(GetAction());
			task
				.OnFaulted(ex =>
				{
					LastFault = ex;
				})
				.OnFullfilled(() =>
				{
					LastComplete = DateTime.Now;
					Interlocked.Increment(ref _count);
				})
				.ContinueWith(t =>
				{
					Interlocked.CompareExchange(ref _task, null, task);
				});
			return task;
		}

		public CancellableTask Run()
		{
			return Defer(TimeSpan.Zero);
		}

		public CancellableTask Defer(TimeSpan delay, bool clearSchedule = true)
		{
			if (clearSchedule)
			{
				Cancel(true); // Don't cancel defered if already running.
			}

			CancellableTask task = null;
			if ((task = _task) == null)
			{
				task = Prepare();
				if (null == Interlocked.CompareExchange(ref _task, task, null))
				{
					task.Start(delay);
				}
			}
			return task;
		}

		public CancellableTask Defer(int millisecondsDelay, bool clearSchedule = true)
		{
			return Defer(TimeSpan.FromMilliseconds(millisecondsDelay), clearSchedule);
		}

	}
}
