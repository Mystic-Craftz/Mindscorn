using UnityEngine;

public class BossSearchState : IState
{
    private BossAI boss;
    private float timer;

    public BossSearchState(BossAI boss)
    {
        this.boss = boss;
    }

    public void Enter()
    {
        timer = boss.searchDuration;
    }

    public void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            boss.stateMachine.ChangeState(boss.wanderState);
        }
    }

    public void Exit() { }
}
