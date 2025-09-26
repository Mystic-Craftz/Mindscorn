using UnityEngine;
using UnityEngine.AI;

public class BossChaseState : IState
{
    private BossAI boss;
    private float originalSpeed;
    private bool originalUpdateRotation;

    public BossChaseState(BossAI boss)
    {
        this.boss = boss;
    }

    public void Enter()
    {
        boss.lockStateTransition = false;

        if (boss.agent != null)
        {
            originalUpdateRotation = boss.agent.updateRotation;
            boss.agent.updateRotation = false;
            boss.agent.updatePosition = true;
            boss.agent.isStopped = false;

            boss.agent.stoppingDistance = boss.attackRange;

            originalSpeed = boss.agent.speed;
            boss.agent.speed = boss.chaseSpeed;
        }

        boss.queuedDash = Random.value < boss.dashChance;
        if (boss.queuedDash && boss.agent != null)
        {
            boss.isDashing = true;
            boss.agent.speed = boss.chaseSpeed * boss.dashSpeedMultiplier;
            boss.anim?.ForceUnlock();
            boss.anim?.PlayAnimation(boss.dashing, 0.1f);
        }
        else
        {
            boss.isDashing = false;
            boss.anim?.ForceUnlock();
            boss.anim?.SetMoveSpeed(boss.agent != null ? boss.agent.speed : boss.chaseSpeed);
        }

        Debug.Log("Entered Chase State");
    }

    public void Update()
    {
        if (boss.player == null)
        {
            Debug.Log("Player is null in Chase State");
            return;
        }

        if (boss.sensor == null || !boss.sensor.PlayerInSight)
        {
            Debug.Log("Player lost sight, transitioning to Search State");
            boss.anim?.SetMoveSpeed(0f);
            boss.queuedDash = false;
            boss.isDashing = false;
            boss.stateMachine.ChangeState(boss.searchState);
            return;
        }

        float distance = Vector3.Distance(boss.transform.position, boss.player.position);
        float stoppingDistance = boss.agent != null ? boss.agent.stoppingDistance : boss.attackRange;
        float tolerance = 0.2f;

        Debug.Log($"Chase State - Distance: {distance}, StoppingDistance: {stoppingDistance}, WithinRange: {distance <= stoppingDistance + tolerance}");

        if (distance > stoppingDistance + tolerance)
        {

            if (boss.agent != null && boss.agent.isOnNavMesh)
            {
                boss.agent.isStopped = false;
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

            bool moving = boss.agent != null && boss.agent.isOnNavMesh && boss.agent.velocity.sqrMagnitude > 0.01f;
            boss.anim?.SetMoveSpeed(moving ? (boss.agent != null ? boss.agent.speed : boss.chaseSpeed) : 0f);
        }
        else
        {
            Debug.Log("Within attack range, transitioning to Attack State");

            if (boss.agent != null && boss.agent.isOnNavMesh)
            {
                boss.agent.isStopped = true;
                boss.agent.ResetPath();
            }

            boss.anim?.SetMoveSpeed(0f);

            if (boss.gameObject.activeInHierarchy)
            {
                boss.StartCoroutine(TransitionToAttackAfterDelay(0.05f));
            }
            else
            {
                boss.stateMachine.ChangeState(boss.attackState);
            }
        }
    }

    private System.Collections.IEnumerator TransitionToAttackAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        boss.stateMachine.ChangeState(boss.attackState);
    }

    public void Exit()
    {
        Debug.Log("Exiting Chase State");

        if (boss.agent != null)
        {
            boss.agent.speed = originalSpeed;
            boss.agent.updateRotation = originalUpdateRotation;
            boss.agent.updatePosition = true;
            boss.agent.isStopped = false;
        }

        boss.anim?.SetMoveSpeed(0f);
    }
}