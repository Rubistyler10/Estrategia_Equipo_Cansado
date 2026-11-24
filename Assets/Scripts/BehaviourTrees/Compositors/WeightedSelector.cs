// Currently does nothing, but it allows for future implementation of weights.
public class WeightedSelector : BehaviourTask
{
    public WeightedSelector(params BehaviourTask[] tasks){
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