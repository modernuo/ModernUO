using System;
using System.Threading;
using System.Threading.Tasks;

namespace Server.Tasks;

public static class TaskExtensions
{
    public static void OnFaultedCurrentSyncContext(this Task task, Action<Task> onFaulted, CancellationToken cancellationToken = default)
    {
        var scheduler = TaskScheduler.FromCurrentSynchronizationContext();
        task.ContinueWith(onFaulted, cancellationToken, TaskContinuationOptions.OnlyOnFaulted, scheduler);
    }

    public static void ContinueWithOnCurrentSyncContext<T>(
        this Task<T> task,
        Action<Task<T>> onRanToCompletion,
        Action<Task<T>> onFaulted,
        CancellationToken cancellationToken = default
    )
    {
        var scheduler = TaskScheduler.FromCurrentSynchronizationContext();
        task.ContinueWith(onRanToCompletion, cancellationToken, TaskContinuationOptions.OnlyOnRanToCompletion, scheduler);
        task.ContinueWith(onFaulted, cancellationToken, TaskContinuationOptions.OnlyOnFaulted, scheduler);
    }

    public static void ContinueWithOnCurrentSyncContext<T>(
        this Task<T> task,
        Action<Task<T>> onCompletion,
        CancellationToken cancellationToken = default
    )
    {
        var scheduler = TaskScheduler.FromCurrentSynchronizationContext();
        task.ContinueWith(onCompletion, cancellationToken, TaskContinuationOptions.None, scheduler);
    }

    public static void ContinueWithOnGameThread<T>(
        this Task<T> task,
        Action<Task<T>> onCompletion,
        CancellationToken cancellationToken = default
    ) => task.ContinueWith(onCompletion, cancellationToken, TaskContinuationOptions.None, Core.LoopContextTaskScheduler);

    public static void ContinueWithOnGameThread(
        this Task task,
        Action<Task> onCompletion,
        CancellationToken cancellationToken = default
    ) => task.ContinueWith(onCompletion, cancellationToken, TaskContinuationOptions.None, Core.LoopContextTaskScheduler);
}
