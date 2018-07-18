using System;
using System.Diagnostics.CodeAnalysis;

namespace Open.Threading.Tasks
{
	[SuppressMessage("ReSharper", "UnusedMemberInSuper.Global")]
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
