using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// AsyncRunner is a helper class that takes care of all the async jobs.
/// It encapsulates the task state, polling, exception logging, and cancellation lifecycle.
/// </summary>
public class AsyncRunner
{
    private Task<BehaviourTask.TerminationType> runningTask;
    // Unlike coroutines, Async functions require manual cleanup in the form
    // of cancellation tokens
    private CancellationTokenSource cts;

    // We can receive a method as an argument, so we pass the async method we are running
    // and handle Cancelation Tokens and other stuff here instead of in every async method we write
    public BehaviourTask.TerminationType Tick(
        Func<CancellationToken, Task<BehaviourTask.TerminationType>> asyncWork)
    {
        // 1. First tick: kick off the async method
        // We give the method a Cancellation Token so we can clean
        // up the memory usage after the function is done.
        if (runningTask == null)
        {
            cts = new CancellationTokenSource();
            runningTask = asyncWork(cts.Token);
        }

        // 2. Still running: pause the Behaviour Tree on this node
        // The IsCompleted check is part of the .NET Task system for async functions
        // It returns false when we are using a Yield to wait for the next frame to continue running
        if (!runningTask.IsCompleted)
        {
            return BehaviourTask.TerminationType.IN_PROGRESS;
        }

        // 3. Finished: extract result, reset state for future runs, and return
        // These are some extra error check catches, aside from the final result extraction.
        BehaviourTask.TerminationType result;
        // IsCanceled is also part of the built-in Task<> type.
        if (runningTask.IsCanceled)
        {
            result = BehaviourTask.TerminationType.FAILURE;
        }
        // IsFaulted is also part of the built-in Task<> type.
        else if (runningTask.IsFaulted)
        {
            Debug.LogException(runningTask.Exception?.InnerException ?? runningTask.Exception);
            result = BehaviourTask.TerminationType.FAILURE;
        }
        else
        {
            result = runningTask.Result;
        }

        // Disposing of the CancellationToken and thus freeing the memory
        // used for the async task
        cts?.Dispose();
        cts = null;
        runningTask = null;

        return result;
    }

    public void Abort()
    {
        // Cancel the token so the awaiting method exits on its next resume,
        // and clear references so this node can restart cleanly if re-entered.
        if (cts != null && !cts.IsCancellationRequested)
        {
            cts.Cancel();
        }
        cts = null;
        runningTask = null;
    }
}