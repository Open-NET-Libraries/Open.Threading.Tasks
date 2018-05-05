using System;
using System.Threading;
using System.Threading.Tasks;

namespace Open.Threading.Tasks
{
	public class TimeoutHandler : IDisposable
	{

		CancellationTokenSource TokenSource;
		TimeoutHandler(int delay, Action<int> onComplete)
		{
			TokenSource = new CancellationTokenSource();
			Task.Delay(delay, TokenSource.Token).ContinueWith(t =>
			{
				if (!t.IsCanceled) onComplete(delay);
			});
		}

		public static TimeoutHandler New(int delay, Action<int> onComplete)
		{
			return new TimeoutHandler(delay, onComplete);
		}

		public static bool New(int delay, out IDisposable timeout, Action<int> onComplete)
		{
			timeout = New(delay, onComplete);
			return true;
		}

		public void Dispose()
		{
			TokenSource.Cancel();
		}
	}
}
