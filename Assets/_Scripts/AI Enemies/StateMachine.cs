public class StateMachine
{
    private IState currentState;
    private MonsterAI monsterAI;

    public IState CurrentState => currentState;


    public StateMachine(MonsterAI owner, IState initialState)
    {
        this.monsterAI = owner;
        this.currentState = initialState;
        monsterAI.currentStateName = currentState.GetType().Name;
        currentState.Enter();
    }

    public void ChangeState(IState newState)
    {
        if (monsterAI.isProcessingHit)
        {
            monsterAI.queuedStateAfterHit = newState;
            return;
        }

        if (monsterAI.isResurrecting)
        {
            if (newState == monsterAI.hitState)
                return;

            monsterAI.queuedStateAfterResurrection = newState;
            return;
        }

        if (currentState == newState) return;
        currentState?.Exit();
        currentState = newState;
        monsterAI.currentStateName = currentState.GetType().Name;
        currentState?.Enter();
    }

    public void Update()
    {
        currentState.Update();
    }
}
