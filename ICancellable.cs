using System;
using System.Diagnostics.CodeAnalysis;

namespace Open.Threading.Tasks
{
	public interface ICancellable : IDisposable
	{
		/// <summary>
		/// Returns true if cancelled.
		/// Returns false if already run or already cancelled or unable to cancel.
		/// </summary>
		/// <returns></returns>
		bool Cancel();
	}
}
