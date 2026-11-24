public abstract class Decorator : BehaviourTask
{
    public Decorator(BehaviourTask task){
        children = new[] {task};
    }
}