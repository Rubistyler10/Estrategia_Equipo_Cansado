class RunUntilFailure : Decorator{
    public RunUntilFailure(BehaviourTask task) : base(task)
    {
        children[0] = task;
    }

    public override TerminationType Run(){
        while(true){
            TerminationType result = children[0].Run();

            if (result == TerminationType.FAILURE) break;
        }

        // Doesn't fail because its job is to repeat as much as possible,
        // even if "as much as possible" is 0
        return TerminationType.SUCCESS;
    }
}