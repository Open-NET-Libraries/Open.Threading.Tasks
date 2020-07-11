using System;
using System.Threading;
using System.Threading.Tasks;

namespace Open.Threading.Tasks
{
	/// <summary>
	/// A Task sub-class that simplifies cancelling.
	/// </summary>
	public class CancellableTask : Task, ICancellable
	{
		protected CancellationTokenSource? TokenSource;

		public bool Cancel(bool onlyIfNotRunning)
		{
			if (onlyIfNotRunning && Status == TaskStatus.Running || IsCanceled || IsFaulted || IsCompleted)
				return false;

			var ts = Interlocked.Exchange(ref TokenSource, null); // Cancel can only be called once.
			if (ts is null) return false;
			using (ts)
			{
				if (ts.IsCancellationRequested) return false;
				ts.Cancel();
			}

			return true;
		}

		public bool Cancel() => Cancel(false);

		protected static void Blank() { }

		protected override void Dispose(bool disposing)
		{
			Cancel();
			base.Dispose(disposing);
		}

		protected CancellableTask(Action? action, CancellationToken token)
			: base(action ?? Blank, token)
		{
		}

		protected CancellableTask(Action? action)
			: base(action ?? Blank)
		{
		}

		protected CancellableTask(CancellationToken token)
			: this(Blank, token)
		{
		}

		protected CancellableTask()
			: this(Blank)
		{
		}

		// Only allow for static initilialization because this owns the TokenSource.
		public static CancellableTask Init(Action? action = null)
		{
			var ts = new CancellationTokenSource();
			var token = ts.Token;
			return new CancellableTask(action, token)
			{
				TokenSource = ts // Could potentially call cancel before run actually happens.
			};
		}

		public void Start(TimeSpan delay, TaskScheduler? scheduler = null)
		{
			if (delay < TimeSpan.Zero)
			{
				RunSynchronously();
			}
			else if (delay == TimeSpan.Zero)
			{
				if (scheduler is null)
					Start();
				else
					Start(scheduler);
			}
			else
			{
				var runState = 0;

				ContinueWith(t =>
				{
					// If this is arbitrarily run before the delay, then cancel the delay.
					if (Interlocked.Increment(ref runState) < 2)
						Cancel();
				});

				Delay(delay, TokenSource!.Token)
					.OnFullfilled(() =>
					{
						Interlocked.Increment(ref runState);
						this.EnsureStarted(scheduler);
					});
			}
		}

		public void Start(int millisecondsDelay, TaskScheduler? scheduler = default)
			=> Start(TimeSpan.FromMilliseconds(millisecondsDelay), scheduler);

		public static CancellableTask StartNew(TimeSpan delay, Action? action = null, TaskScheduler? scheduler = default)
		{
			var task = new CancellableTask(action);
			task.Start(delay, scheduler);
			return task;
		}

		public static CancellableTask StartNew(int millisecondsDelay, Action? action = null)
			=> StartNew(TimeSpan.FromMilliseconds(millisecondsDelay), action);

		public static CancellableTask StartNew(Action action, TimeSpan? delay = null, TaskScheduler? scheduler = default)
			=> StartNew(delay ?? TimeSpan.Zero, action, scheduler);

		public static CancellableTask StartNew(Action<CancellationToken> action, TimeSpan? delay = null, TaskScheduler? scheduler = default)
		{
			var ts = new CancellationTokenSource();
			var token = ts.Token;
			var task = new CancellableTask(() => action(token), token)
			{
				TokenSource = ts
			};
			task.Start(delay ?? TimeSpan.Zero, scheduler);
			return task;
		}
	}

}
