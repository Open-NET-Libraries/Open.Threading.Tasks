using System;
using System.Threading;
using System.Threading.Tasks;

namespace Open.Threading.Tasks
{
	public class TimeoutHandler : IDisposable
	{
		CancellationTokenSource? TokenSource;
		TimeoutHandler(TimeSpan delay, Action<TimeSpan> onTimeout)
		{
			TokenSource = new CancellationTokenSource();
			Task.Delay(delay, TokenSource.Token).ContinueWith(t =>
			{
				Interlocked.Exchange(ref TokenSource, null)?.Dispose();
				if (!t.IsCanceled) onTimeout(delay);
			});
		}

		public static TimeoutHandler New(TimeSpan delay, Action<TimeSpan> onTimeout)
			=> new TimeoutHandler(delay, onTimeout);

		public static bool New(TimeSpan delay, out IDisposable timeout, Action<TimeSpan> onTimeout)
		{
			timeout = New(delay, onTimeout);
			return true;
		}

		public static TimeoutHandler New(double delay, Action<double> onTimeout)
			=> New(TimeSpan.FromMilliseconds(delay), ts=> onTimeout(ts.TotalMilliseconds));

		public static bool New(double delay, out IDisposable timeout, Action<double> onTimeout)
		{
			timeout = New(delay, onTimeout);
			return true;
		}

		public void Dispose()
		{
			var ts = Interlocked.Exchange(ref TokenSource, null);
			if (ts is null) return;
			ts.Cancel();
			ts.Dispose();
		}
	}
}
