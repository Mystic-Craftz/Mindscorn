using UnityEngine;
using UnityEngine.AI;
using System.Collections;

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
        boss.singingPlayedThisChase = false;

        if (boss.agent != null)
        {
            // save and restore later
            originalUpdateRotation = boss.agent.updateRotation;

            // Let the NavMeshAgent handle rotation and position while chasing
            boss.agent.updateRotation = true;
            boss.agent.updatePosition = true;
            boss.agent.isStopped = false;
            boss.agent.stoppingDistance = boss.chaseStoppingDistance;
            originalSpeed = boss.agent.speed;
            boss.agent.speed = boss.chaseSpeed;
        }

        boss.anim?.ForceUnlock();
        boss.anim?.PlayAnimation("Locomotion");
        boss.anim?.SetMoveSpeed(boss.chaseSpeed);
        boss.TryPlaySingingOnce();
    }

    public void Update()
    {
        if (boss.player == null) return;

        if (IsWithinAttackRange())
        {
            Debug.Log($"Boss in attack range! Distance: {Vector3.Distance(boss.transform.position, boss.player.position)}, Attack Range: {boss.attackRange}");
            TransitionToAttack();
            return;
        }

        // ----------  roll for special attack while NOT in attack range ----------
        if (boss.player != null && boss.sensor != null && boss.sensor.PlayerInSight && boss.canRollForSpecial)
        {
            if (Random.value < boss.specialAttackChancePerSecond * Time.deltaTime)
            {
                boss.pendingSpecialAttack = true;
                boss.canRollForSpecial = false;
                boss.specialRollCooldownTimer = boss.specialRollCooldown;
                boss.stateMachine.ChangeState(boss.attackState);
                return;
            }
        }

        if (boss.sensor == null || !boss.sensor.PlayerInSight)
        {
            boss.anim?.SetMoveSpeed(0f);
            boss.stateMachine.ChangeState(boss.searchState);
            return;
        }

        if (boss.agent != null && boss.agent.isOnNavMesh)
        {
            boss.agent.isStopped = false;
            boss.agent.SetDestination(boss.player.position);
            boss.agent.speed = boss.chaseSpeed;
        }

        boss.lastKnownPlayerPosition = boss.player.position;

        bool moving = boss.agent != null && boss.agent.isOnNavMesh && boss.agent.velocity.sqrMagnitude > 0.01f;

        boss.anim?.PlayAnimation("Locomotion");
        boss.anim?.SetMoveSpeed(moving ? boss.chaseSpeed : 0f);
    }

    private void TransitionToAttack()
    {
        boss.stateMachine.ChangeState(boss.attackState);
    }

    public void Exit()
    {
        boss.StopSinging();

        if (boss.agent != null)
        {
            boss.agent.speed = originalSpeed;
            boss.agent.updateRotation = originalUpdateRotation;
            boss.agent.updatePosition = true;
            boss.agent.isStopped = false;
        }

        boss.anim?.SetMoveSpeed(0f);
        boss.anim?.ForceUnlock();
    }

    private bool IsWithinAttackRange()
    {
        if (boss.player == null) return false;
        float distance = Vector3.Distance(boss.transform.position, boss.player.position);
        return distance <= boss.attackRange;
    }
}
