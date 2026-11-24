/* 
THIS CODE WAS WRITTEN BY GEMINI, NOT INTENDED FOR ACTUAL USE.

This is an example of how to implement asynchronous behaviour tree nodes in Unity using async/await.
The base class EnemyTask is a domain-specific extension of BehaviourTask, which allows for easy creation 
of enemy AI tasks that can run asynchronously.

The MoveToTargetTask is a concrete implementation that moves an enemy towards a target using Unity's NavMeshAgent, 
while also supporting cancellation and proper cleanup.

Things to note:
    - Run() override is used to call RunAsync(), all actions are done on run, so we are
    supposed to call the asyn version inside of it.
    - We tell the function to wait for the next frame using Task.Yield(), which is a built-in .NET function that allows us to yield control back to Unity's main thread.
    - We use CancellationToken to allow for proper cleanup and cancellation of the async task if needed,
    but the cancellation is handled by AsyncRunner, so we don't have to remembet to do it every time.
    - try/catch/finally is preferable to handle task completion status.
*/


using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

// Domain-specific base class stays completely intact
public abstract class EnemyTask : BehaviourTask
{
    protected readonly NavMeshAgent agent;
    protected readonly Transform target;

    protected EnemyTask(NavMeshAgent agent, Transform target)
    {
        this.agent = agent;
        this.target = target;
    }
}

// Inherits from EnemyTask AND uses async/await cleanly
public class MoveToTargetTask : EnemyTask
{
    private readonly float stoppingDistance;

    public MoveToTargetTask(NavMeshAgent agent, Transform target, float stoppingDistance = 1.5f)
        : base(agent, target)
    {
        this.stoppingDistance = stoppingDistance;
    }

    public override TerminationType Run() => RunAsync(MoveRoutine);

    private async Task<TerminationType> MoveRoutine(CancellationToken token)
    {
        if (target == null || !agent.isOnNavMesh)
            return TerminationType.FAILURE;

        agent.SetDestination(target.position);

        try
        {
            // Wait until path calculation finishes and agent reaches the target
            while (agent.pathPending || agent.remainingDistance > stoppingDistance)
            {
                token.ThrowIfCancellationRequested();
                agent.SetDestination(target.position);
                await Task.Yield(); // Resumes next frame on Unity's main thread
            }

            return TerminationType.SUCCESS;
        }
        catch (System.OperationCanceledException)
        {
            return TerminationType.FAILURE;
        }
        finally
        {
            // Runs whether the node succeeds, fails, or gets aborted mid-walk
            if (agent != null && agent.isOnNavMesh)
            {
                agent.ResetPath();
            }
        }
    }
}