﻿using System;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;

namespace Open.Threading.Tasks;

public static class TaskExtensions
{
	/// <summary>
	/// Returns true if the target Task has not yet run, is waiting, or is running, else returns false.
	/// </summary>
	public static bool IsActive(this Task target)
	{
		if (target is null) throw new ArgumentNullException(nameof(target));
		Contract.EndContractBlock();

		return target.Status switch
		{
			TaskStatus.Created or
			TaskStatus.Running or
			TaskStatus.WaitingForActivation or
			TaskStatus.WaitingForChildrenToComplete or
			TaskStatus.WaitingToRun => true,
			_ => false,
		};
	}

	/// <summary>
	/// Checks the status of the task and attempts to start it if waiting to start (TaskStatus.Created).
	/// </summary>
	/// <param name="target">The task to ensure start.</param>
	/// <param name="scheduler">Optional scheduler to use.</param>
	/// <returns>True if start attempt was successful.</returns>
	public static bool EnsureStarted(this Task target, TaskScheduler? scheduler = default)
	{
		if (target is null) throw new ArgumentNullException(nameof(target));
		if (target.Status != TaskStatus.Created) return false;
		try
		{
			if (scheduler is null)
				target.Start();
			else
				target.Start(scheduler);

			return true;
		}
		catch (InvalidOperationException)
		{
			// Even though we've checked the status, it's possible it could have been started.  We can't guarantee proper handling without a trap here.
#pragma warning disable RCS1236 // Use exception filter.
			if (target.Status == TaskStatus.Created)
				throw; // Something wierd must have happened if we arrived here.
#pragma warning restore RCS1236 // Use exception filter.
		}

		return false;
	}

	/// <summary>
	/// Utility method that can be chained with other methods for reacting to Task results.  Only invokes the action if completed and not cancelled.
	/// </summary>
	/// <typeparam name="TTask">The return type is the same as the target.</typeparam>
	/// <param name="target">The task.</param>
	/// <param name="action">The action to perform if fullfulled.</param>
	/// <returns>The target object.  Allows for method chaining.</returns>
	public static TTask OnFullfilled<TTask>(this TTask target, Action action)
		where TTask : Task
	{
		if (target is null) throw new ArgumentNullException(nameof(target));
		target.ContinueWith(task =>
		{
			if (task.Status == TaskStatus.RanToCompletion) action();
		});

		return target;
	}

	/// <summary>
	/// Utility method that can be chained with other methods for reacting to Task results.  Only invokes the action if completed and not cancelled.
	/// </summary>
	/// <typeparam name="T">The return type is the same as the target.</typeparam>
	/// <param name="target">The task.</param>
	/// <param name="action">The action to perform if fullfulled.</param>
	/// <returns>The target object.  Allows for method chaining.</returns>
	public static Task<T> OnFullfilled<T>(this Task<T> target, Action<T> action)
	{
		if (target is null) throw new ArgumentNullException(nameof(target));
		target.ContinueWith(task =>
		{
			if (task.Status == TaskStatus.RanToCompletion) action(task.Result);
		});

		return target;
	}

	/// <summary>
	/// Utility method that can be chained with other methods for reacting to Task results.  Only invokes the action if completed and not cancelled.
	/// </summary>
	/// <typeparam name="TTask">The task type.</typeparam>
	/// <typeparam name="T">The return type of the task.</typeparam>
	/// <param name="target">The task.</param>
	/// <param name="action">The action to perform if fullfulled.</param>
	/// <returns>The target object.  Allows for method chaining.</returns>
	public static TTask OnFullfilled<TTask, T>(this TTask target, Func<T> action)
		where TTask : Task
	{
		if (target is null) throw new ArgumentNullException(nameof(target));
		target.ContinueWith(task =>
		{
			if (task.Status == TaskStatus.RanToCompletion) action();
		});

		return target;
	}

	/// <summary>
	/// Utility method that can be chained with other methods for reacting to Task results. Only invokes the action if faulted.
	/// </summary>
	/// <typeparam name="TTask">The return type is the same as the target.</typeparam>
	/// <param name="target">The task.</param>
	/// <param name="action">The action to perform if faulted.</param>
	/// <returns>The target object.  Allows for method chaining.</returns>
	public static TTask OnFaulted<TTask>(this TTask target, Action<Exception> action)
		where TTask : Task
	{
		if (target is null) throw new ArgumentNullException(nameof(target));
		target.ContinueWith(task =>
		{
			if (task.IsFaulted) action(task.Exception);
		});

		return target;
	}

	/// <summary>
	/// Utility method that can be chained with other methods for reacting to Task results.  Only invokes the action if cancelled.
	/// </summary>
	/// <typeparam name="TTask">The return type is the same as the target.</typeparam>
	/// <param name="target">The task.</param>
	/// <param name="action">The action to perform if cancelled.</param>
	/// <returns>The target object.  Allows for method chaining.</returns>
	public static TTask OnCancelled<TTask>(this TTask target, Action action)
		where TTask : Task
	{
		if (target is null) throw new ArgumentNullException(nameof(target));
		target.ContinueWith(task =>
		{
			if (!task.IsCanceled) action();
		});

		return target;
	}

	/// <summary>
	/// Utility method that can be chained with other methods for reacting to Task results.  Only invokes the action if cancelled.
	/// </summary>
	/// <typeparam name="TTask">The task type.</typeparam>
	/// <typeparam name="T">The return type of the task.</typeparam>
	/// <param name="target">The task.</param>
	/// <param name="action">The action to perform if cancelled.</param>
	/// <returns>The target object.  Allows for method chaining.</returns>
	public static TTask OnCancelled<TTask, T>(this TTask target, Func<T> action)
		where TTask : Task
	{
		if (target is null) throw new ArgumentNullException(nameof(target));
		target.ContinueWith(task =>
		{
			if (!task.IsCanceled) action();
		});

		return target;
	}
}
