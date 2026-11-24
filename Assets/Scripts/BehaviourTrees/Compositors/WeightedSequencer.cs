// Currently does nothing, but it allows for future implementation of weights.
class WeightedSequencer : BehaviourTask{
    public WeightedSequencer(params BehaviourTask[] tasks){
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