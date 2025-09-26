using UnityEngine;
using UnityEngine.AI;

public class BossChaseState : IState
{
    private BossAI boss;
    private const float moveThresholdSqr = 0.01f;
    private float originalSpeed;
    private bool originalUpdateRotation;

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

            originalUpdateRotation = boss.agent.updateRotation;
            boss.agent.updateRotation = false;

            boss.agent.stoppingDistance = boss.attackRange;
            originalSpeed = boss.agent.speed;
            boss.agent.speed = boss.chaseSpeed;
        }

        boss.queuedDash = Random.value < boss.dashChance;
        // dash
        if (boss.queuedDash && boss.agent != null)
        {
            boss.isDashing = true;
            boss.agent.speed = boss.chaseSpeed * boss.dashSpeedMultiplier;
            boss.anim?.ForceUnlock();
            boss.anim?.PlayAnimation(boss.dashing, 0.1f);
        }
        else // normal chase
        {
            boss.isDashing = false;
            boss.anim?.ForceUnlock();
            boss.anim?.SetMoveSpeed(boss.agent != null ? boss.agent.speed : boss.chaseSpeed);
        }
    }

    public void Update()
    {
        if (boss.player != null && boss.sensor != null && boss.sensor.PlayerInSight)
        {
            if (boss.agent != null && boss.agent.isOnNavMesh)
            {
                boss.agent.SetDestination(boss.player.position);
                boss.agent.speed = boss.isDashing ? boss.chaseSpeed * boss.dashSpeedMultiplier : boss.chaseSpeed;
            }

            boss.lastKnownPlayerPosition = boss.player.position;

            Vector3 dir = boss.player.position - boss.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                boss.transform.rotation = Quaternion.Slerp(boss.transform.rotation, targetRot, boss.rotationSpeed * Time.deltaTime);
            }

            bool moving = boss.agent != null && boss.agent.isOnNavMesh && boss.agent.velocity.sqrMagnitude > moveThresholdSqr;
            if (moving)
            {
                if (!boss.isDashing)
                    boss.anim?.SetMoveSpeed(boss.agent.speed);
            }
            else
            {
                boss.anim?.SetMoveSpeed(0f);
            }


            float tolerance = 0.1f;
            float stopping = (boss.agent != null) ? boss.agent.stoppingDistance : boss.attackRange;
            float direct = Vector3.Distance(boss.transform.position, boss.player.position);
            bool reached = direct <= stopping + tolerance;

            if (reached)
            {
                boss.stateMachine.ChangeState(boss.attackState);
            }

            return;
        }

        // Player not in sight, switch to search
        boss.anim?.SetMoveSpeed(0f);
        boss.queuedDash = false;
        boss.isDashing = false;
        boss.stateMachine.ChangeState(boss.searchState);
    }

    public void Exit()
    {
        if (boss.agent != null)
        {
            boss.agent.speed = originalSpeed;
            boss.agent.updateRotation = originalUpdateRotation;
        }

        boss.anim?.SetMoveSpeed(0f);
    }
}
