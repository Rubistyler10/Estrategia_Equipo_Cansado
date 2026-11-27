# Disclaimer
This are notes I took while figuring everything out, I tried to make them in a way that would understandable to other people, but it is not proper documentation. Regardless, they do serve a simple explanation of the stuff implemented for behaviour trees.

If any example differs from the final implementation, always take the actual implementation as the correct approach for the project.


# Tasks
First we define the basic tasks that form the the nodes of a behaviour tree, **should be as self-contained as possible, only doing one conceptual _task_ (duh) per node.**

```C#
public abstract class Task{
    // We use an enum for termination types in case we want more than
    // a binary success/failure return that would usually be a bool.
    // Its a small change that future-proofs the implementation because
    // it would be a gigantic pain to have to change it after making several behaviours
    public enum TerminationType{
        SUCCESS,
        FAILURE,
    }

    // Holds a list of the children (if any) of this task
    public Task[] children;
    // Always terminates with either success (True) or failure (False)
    public abstract TerminationType run();
}

```

## Composition
We will want to create chains of tasks and different possible paths for the tree to run through.
To compose Tasks we use 2 specific implementations of them:
- **Selector** is an _if/else_, it tries running its children in order. If one fails, it runs the next, if the current succeeds it ends.
- **Sequence** runs all of its children in order and only continues **if the previous task is successfull**.

```C#
class Selector : Task{
    public Selector(params Task[] tasks){
        children = tasks;
    }

    TerminationType run(){
        foreach (Task task in children){
            if (task.run() == TerminationType.SUCCESS)
                return TerminationType.SUCCESS;
        }
        return TerminationType.FAILURE;
    }
}
```

```C#
class Sequence : Task{
    public Sequence(params Task[] tasks){
        children = tasks;
    }

    TerminationType run(){
        foreach (Task task in children){
            if (task.run() == TerminationType.FAILURE)
                return TerminationType.FAILURE;
        }
        return TerminationType.SUCCESS;
    }
}
```
**Compositors are the glue that connects all tasks**. The root of a tree will _always_ be a compositor.

The order of actions can become very predictable by only using Selectors and Sequences, so for cases where the order of the actions taken doesn't matter, we create a random version.

(I considered making them inherit from their non-random versions, but they are simple enough for the duplicated code to not really matter)
```C#
class RandomSelector : Task{
    public RandomSelector(params Task[] tasks){
        children = tasks;
    }

    TerminationType run(){
        Task[] shuffled = Random.Shuffle<Task>(children);
        foreach (Task task in shuffled){
            if (task.run() == TerminationType.SUCCESS)
                return TerminationType.SUCCESS;
        }
        return TerminationType.FAILURE;
    }
}
```

```C#
class RandomSequence : Task{
    public RandomSequence(params Task[] tasks){
        children = tasks;
    }

    TerminationType run(){
        Task[] shuffled = Random.Shuffle<Task>(children);
        foreach (Task task in shuffled){
            if (task.run() == TerminationType.FAILURE)
                return TerminationType.FAILURE;
        }
        return TerminationType.SUCCESS;
    }
}
```

## Conditions, Actions
For checks and actions to take, we just make use of tasks as they are. 
Both conditions and actions look the same since they just have to 'run()' their code and inform of their termination type.
They do not have any children, **they do their thing, give an output and dip**.

```C#
class ThisIsACondition : Task{
    TerminationType run(){
        if (conditionToCheck == true){
            return TerminationType.SUCCESS;
        }
        else return TerminationType.FAILURE;
    }
}
```

```C#
class ThisIsAnAction : Taks{
    TerminationType run(){
        thing.do;

        if (thing.isDone) return TerminationType.SUCCESS;
        else return TerminationType.FAILURE;
    }
}
```

## Decorators
The Decorators are wrappers for another **individual** task that **modify** its behaviour.

```C#
abstract class Decorator : Task{
    public Decorator(Task task){
        child = task;
    }

    // It still inherits the [children] variable from Task,
    // but stays unused in favour of a single Task variable.
    Task child;
}
```

A common and useful type of decorator is a _filter_, for example. This decorator decides whether its child will be run, with some conditions.
If it doesn't let its child run, returns Failure. If it does allow it, it echoes whatever the child returns after running.

Some examples of filters:
```C#
class LimitCounter : Decorator{
    int runLimit;
    int runSoFar;

    TerminationType run(){
        if (runSoFar >= runLimit)
            return TerminationType.FAILURE;
        
        runSoFar++;
        return child.run();
    }
}
```

```C#
class RunUntilFailure : Decorator{
    TerminationType run(){
        while(true){
            TerminationType result = child.run();

            if (result == TerminationType.FAILURE) break;
        }

        // Doesn't fail because its job is to repeat as much as possible,
        // even if "as much as possible" is 0
        return TerminationType.SUCCESS;
    }
}
```

> Want to decorate multiple tasks? Use a decorator on a sequencer, for example!
> Want to run a task over and over for a specific number of times (a for loop)? Concatenate a RunUntilFailure and a RunLimit!

# Concurrency
Some of the tasks may need several frames to complete, so a basic implementation that tries to run the whole branch of the tree in a single frame would be insufficient. To deal with this we have to use tools that allow to control the flow of instructions, waiting for certain tasks to finish before continuing or just stopping the flow for a certain amount of time.
> Since we need the methods to actually return something (TerminationType), coroutines have been passed as an option.

The chosen way to make this work is via _Async_ and _Await_ functions, which implement this _wait for condition_ behaviour.
Thanks to the enum on our base Task class, we can add an INPROGRESS termination type and adapt our sequencers and selectors to take them into account and not move on until we get a failure or success state.

The initial method of implementation considered for the async system involved inheritance, but since C# is single-inheritance we run into problems when we want to create other task subclasses that involve inheritance.
"Then why not use decorators?" It doesn't really help much, as we would need the task wrapped by the decorator to also implement the async approach, so we can't just wrap the tasks like that.

A solution: We embed the async running system innto the base task class, but we make it so we only create an asyncrunner when we actually call for one, this way, we avoid adapting the whole system to use async even when it isn't needed.


### Parallel Tasks
Async and Await actually allow us to do parallel tasks thorugh the WhenAll and WhenAny methods. We can use this to create parallel sequencers or even parallel selectors (although the latter is much more situational and probably a waste of time 99% of the cases we would initially think it could be used).

# Building Trees
Building the trees is actually pretty easy, since it is just about connecting pieces. Unfortunately, as we don't have a GUI for it, we do have to deal with the slightly less visually appealing way of doing through text. Indentation is our best friend here:

```C#
new Selector(
    new Sequencer(
        new CheckActiveUnitsTask(),
        new RunUntilFailure(
            new Sequencer(
                new SelectUnitTask(),
                new MoveUnitASyncTask(),
                new EnemyInRangeCondition(),
                new SelectEnemyTask(),
                new AttackEnemyASyncTask()
            )
        )
        new ShouldRecruitCondition(),
        new RecruitUnitTask()
    )
)
```
> I **STRONGLY** advise to check the Behaviour Trees section in Chapter 5 of the book to get visual examples of how the trees operate.

You can also create a class for a tree inheriting BehaviourTask to make smaller trees and then combining them. Remember to set the children _in the constructor_.
```C#
public class BehaviourTreeExample : BehaviourTask
{
    BehaviourTreeExample()
    {
        children = new BehaviourTask[]
        {
            new Selector(
                new Sequencer(
                    new CheckActiveUnitsTask(),
                    new RunUntilFailure(
                        new Sequencer(
                            new SelectUnitTask(),
                            new MoveUnitASyncTask(),
                            new EnemyInRangeCondition(),
                            new SelectEnemyTask(),
                            new AttackEnemyASyncTask()
                        )
                    )
                    new ShouldRecruitCondition(),
                    new RecruitUnitTask()
                )
            )
        };
    }

    public override TerminationType Run()
    {
        throw new System.NotImplementedException();
    }
}
```

```C#
// ... class definition stuff here
BehaviourTreeExample2(){
    // As you can see, this means that we run the full
    // BehaviourTreeExample until its final state is failure,
    // then it starts running BehaviourTreeExample3.
    // If BTE doesn't fail, then BTE2 would also return success
    // and it would be over.
    new Selector(
        new BehaviourTreeExample(),
        new BehaviourTreeExample3(),
        // You get the gist
    )
};
// ... rest of the stuff
``` 
# Blackboards

The blackboard is the communication system between tasks in a Behaviour Tree or between different Behaviour Trees.
It provides an enum of ObjectiveTypes and allows for creation of objectives with additional info to work with.
**There can only be 1 objective per type**.