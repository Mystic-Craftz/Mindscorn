using UnityEngine;

public class BossAttackState : IState
{
    private BossAI boss;

    public BossAttackState(BossAI boss)
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
