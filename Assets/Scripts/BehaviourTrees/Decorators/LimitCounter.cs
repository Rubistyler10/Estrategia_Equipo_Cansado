class LimitCounter : Decorator{
    int runLimit;
    int runSoFar;

    public LimitCounter(BehaviourTask task, int runLimit) : base(task)
    {
        children[0] = task;
        this.runLimit = runLimit;
        runSoFar = 0;
    }

    public override TerminationType Run(){
        if (runSoFar >= runLimit)
            return TerminationType.FAILURE;
        
        runSoFar++;
        return children[0].Run();
    }
}