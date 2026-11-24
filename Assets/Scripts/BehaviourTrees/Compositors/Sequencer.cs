/// <summary>
/// A sequencer is type of node that runs through its children in order until one of them fails.
/// As a composite node, it should never be a leaf node, and should always have children
/// </summary>
/// <remarks>
/// Sequencers don't make use of the async system, since the asynchronous behaviour
/// is meant to be used on leaves of the tree.
/// </remarks>
class Sequencer : BehaviourTask{
    public Sequencer(params BehaviourTask[] tasks){
        children = tasks;
    }

    public override TerminationType Run(){
        foreach (BehaviourTask task in children){
            if (task.Run() == TerminationType.FAILURE)
                return TerminationType.FAILURE;
        }
        return TerminationType.SUCCESS;
    }
}