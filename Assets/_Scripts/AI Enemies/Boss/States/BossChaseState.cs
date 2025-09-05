using UnityEngine;
using UnityEngine.AI;

public class BossChaseState : IState
{
    private BossAI boss;
    private const float moveThresholdSqr = 0.01f;

    public BossChaseState(BossAI boss)
    {
        this.boss = boss;
    }

    public void Enter()
    {
        if (boss.agent != null)
        {
            boss.agent.isStopped = false;
            boss.agent.updatePosition = true;
            boss.agent.updateRotation = true;
            boss.agent.speed = boss.chaseSpeed;
            boss.agent.stoppingDistance = boss.attackRange;
        }
    }

    public void Update()
    {
        if (boss.player != null && boss.sensor != null && boss.sensor.PlayerInSight)
        {
            if (boss.agent != null && boss.agent.isOnNavMesh)
            {
                boss.agent.SetDestination(boss.player.position);
                boss.agent.speed = boss.chaseSpeed;
            }

            // update last known location while player is seen
            boss.lastKnownPlayerPosition = boss.player.position;

            bool moving = boss.agent != null && boss.agent.isOnNavMesh && boss.agent.velocity.sqrMagnitude > moveThresholdSqr;
            boss.anim?.SetMoveSpeed(moving ? 1f : 0f);

            return;
        }

        boss.anim?.SetMoveSpeed(0f);
        boss.stateMachine.ChangeState(boss.searchState);
    }

    public void Exit()
    {

    }
}
