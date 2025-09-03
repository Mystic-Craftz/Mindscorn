using UnityEngine;

public class BossWanderState : IState
{
    private BossAI boss;
    private Vector3 wanderTarget;

    public BossWanderState(BossAI boss)
    {
        this.boss = boss;
    }

    public void Enter()
    {

    }

    public void Update()
    {

    }

    public void Exit() { }


}
