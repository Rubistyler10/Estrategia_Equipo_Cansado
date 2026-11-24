/// <summary>
/// A selector is type of node that runs through its children in order until one of them succeeds.
/// As a composite node, it should never be a leaf node, and should always have children.
/// </summary>
/// <remarks>
/// Selectors don't make use of the async system, since the asynchronous behaviour
/// is meant to be used on leaves of the tree.
/// </remarks>
public class Selector : BehaviourTask
{
    public Selector(params BehaviourTask[] tasks){
        children = tasks;
    }

    public override TerminationType Run(){
        foreach (BehaviourTask task in children){
            if (task.Run() == TerminationType.SUCCESS)
                return TerminationType.SUCCESS;
        }
        return TerminationType.FAILURE;
    }
}