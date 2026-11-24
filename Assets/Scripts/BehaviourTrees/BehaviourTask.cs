using System;
using System.Threading;
using System.Threading.Tasks;

public abstract class BehaviourTask
{

    // We use an enmu for Termination Types to allow for more flexibility
    // than just using a boolean for success or failure.
    // Useful since we do want to have a way to know if something is still
    // not done without claiming it failed.
    public enum TerminationType
    {
        SUCCESS,
        FAILURE,
        IN_PROGRESS
    }

    protected BehaviourTask[] children;
    private AsyncRunner asyncRunner;
    protected Blackboard blackboard;

    internal void BindBlackboard(Blackboard blackboard)
    {
        this.blackboard = blackboard;
        if (children == null) return;
        foreach (var child in children)
        {
            child.BindBlackboard(blackboard);
        }
    }

    // We use Run for any synchronous tasks (any task that won't require several frames)
    public abstract TerminationType Run();

    // We instead call RunAsync, you guessed it, when we want asynchronous work
    protected TerminationType RunAsync(
        Func<CancellationToken, Task<TerminationType>> asyncWork)
    {
        asyncRunner ??= new AsyncRunner();
        return asyncRunner.Tick(asyncWork);
    }

    // The aborting is implemented in case we need to stop all work
    // for whatever reason, async or not.
    // More likely to be required on async work though, since the conditions
    // can change between frames due to player input.
    public virtual void Abort()
    {
        asyncRunner?.Abort();

        if (children == null) return;
        foreach (var child in children)
        {
            child.Abort();
        }
    }
}