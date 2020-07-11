using System;
using System.Threading;
using System.Threading.Tasks;

namespace Open.Threading.Tasks
{
	public class ActionRunner : ICancellable
	{
		public ActionRunner(Action action, TaskScheduler? scheduler = default)
		{
			_action = action;
			_scheduler = scheduler; // No need to hold a refernce to the default, just keep it null.
			LastStart = DateTime.MaxValue;
			LastComplete = DateTime.MaxValue;
		}

		public static ActionRunner Create(Action action, TaskScheduler? scheduler = default)
			=> new ActionRunner(action, scheduler);

		public static ActionRunner Create<T>(Func<T> action, TaskScheduler? scheduler = default)
			=> new ActionRunner(() => { action(); }, scheduler);

		Action? _action;
		// ReSharper disable once NotAccessedField.Global
		protected TaskScheduler? _scheduler;

		protected int _count;
		public int Count => _count;

		public DateTime LastStart
		{
			get;
			protected set;
		}

		public bool HasBeenRun => LastStart < DateTime.Now;

		public DateTime LastComplete
		{
			get;
			protected set;
		}

		public bool HasCompleted => LastComplete < DateTime.Now;

		public bool Cancel(bool onlyIfNotRunning)
		{
			var t = _task;
			return t != null
				&& t == Interlocked.CompareExchange(ref _task, null, t)
				&& t.Cancel(onlyIfNotRunning);
		}

		public bool Cancel()
		{
			return Cancel(false);
		}

		public void Dispose()
		{
			Cancel();
			_action = null;
			_scheduler = null;
		}

		Action GetAction()
		{
			var a = _action;
			if (a is null)
				throw new ObjectDisposedException(typeof(ActionRunner).ToString());
			return a;
		}

		public bool IsScheduled => _task?.IsActive() ?? false;

		/// <summary>
		/// Indiscriminately invokes the action.
		/// </summary>
		public void RunSynchronously()
		{
			GetAction().Invoke();
		}

		CancellableTask? _task;
		CancellableTask Prepare()
		{
			LastStart = DateTime.Now;
			var task = CancellableTask.Init(GetAction());
			task
				.ContinueWith(t =>
				{
					if (t.Status == TaskStatus.RanToCompletion)
					{
						LastComplete = DateTime.Now;
						Interlocked.Increment(ref _count);
					}
					Interlocked.CompareExchange(ref _task, null, task);
				},
				TaskContinuationOptions.ExecuteSynchronously);
			return task;
		}

		public CancellableTask Run()
			=> Defer(TimeSpan.Zero);

		public CancellableTask Defer(TimeSpan delay, bool clearSchedule = true)
		{
			if (clearSchedule)
			{
				Cancel(true); // Don't cancel defered if already running.
			}

			CancellableTask? task;
			if ((task = _task) != null) return task;
			task = Prepare();
			if (null == Interlocked.CompareExchange(ref _task, task, null))
			{
				task.Start(delay);
			}
			return task;
		}

		public CancellableTask Defer(int millisecondsDelay, bool clearSchedule = true)
			=> Defer(TimeSpan.FromMilliseconds(millisecondsDelay), clearSchedule);

	}
}
