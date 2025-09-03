public class BossStateMachine
{
    private IState currentState;
    private BossAI boss;

    public IState CurrentState => currentState;

    public BossStateMachine(BossAI owner, IState initialState)
    {
        boss = owner;
        currentState = initialState;
        currentState.Enter();
    }

    public void ChangeState(IState newState)
    {
        if (currentState == newState) return;
        currentState?.Exit();
        currentState = newState;
        currentState?.Enter();
    }

    public void Update()
    {
        currentState?.Update();
    }
}
