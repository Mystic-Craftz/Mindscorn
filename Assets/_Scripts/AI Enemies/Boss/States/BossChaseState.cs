using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class BossChaseState : IState
{
    private BossAI boss;
    private float originalSpeed;
    private bool originalUpdateRotation;
    private const float reachEpsilon = 0.1f;
    private const float stoppingClampEpsilon = 0.05f;
    private const float attackEnterBuffer = 0.15f;

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
            originalUpdateRotation = boss.agent.updateRotation;
            boss.agent.updateRotation = true;
            boss.agent.updatePosition = true;
            boss.agent.isStopped = false;

            float desiredStop = boss.chaseStoppingDistance;
            if (boss.attackRange > 0f && desiredStop >= boss.attackRange)
            {
                desiredStop = Mathf.Max(0f, boss.attackRange - stoppingClampEpsilon);
            }
            boss.agent.stoppingDistance = desiredStop;

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

        if (boss.sensor == null || !boss.sensor.PlayerInSight)
        {
            boss.anim?.SetMoveSpeed(0f);
            boss.stateMachine.ChangeState(boss.searchState);
            return;
        }

        // ---------- roll for special attack while NOT in attack range ----------
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

        if (boss.agent != null && boss.agent.isOnNavMesh)
        {
            boss.agent.isStopped = false;
            boss.agent.SetDestination(boss.player.position);
            boss.agent.speed = boss.chaseSpeed;
        }

        boss.lastKnownPlayerPosition = boss.player.position;

        // Wait until we've reached the stopping distance before allowing the normal attack transition.
        bool reachedStoppingDistance = AgentReachedStoppingDistance();

        if (reachedStoppingDistance)
        {
            float dist = Vector3.Distance(boss.transform.position, boss.player.position);

            // Only transition to attack if we're comfortably inside the attack range (hysteresis)
            if (dist <= boss.attackRange - attackEnterBuffer)
            {
                Debug.Log($"Boss in attack range (enter)! Distance: {dist:F3}, Attack Range: {boss.attackRange:F3}");
                TransitionToAttack();
                return;
            }

            // If stoppingDistance accidentally >= attackRange, try to nudge it lower so the agent can approach
            if (boss.agent != null && boss.agent.isOnNavMesh &&
                boss.agent.remainingDistance <= boss.agent.stoppingDistance + reachEpsilon &&
                boss.agent.stoppingDistance >= boss.attackRange - 0.01f)
            {
                boss.agent.stoppingDistance = Mathf.Max(0f, boss.attackRange - stoppingClampEpsilon);
                boss.agent.isStopped = false;
                boss.agent.SetDestination(boss.player.position);
            }
        }

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

    private bool AgentReachedStoppingDistance()
    {
        if (boss.agent == null || boss.player == null) return false;

        if (!boss.agent.isOnNavMesh)
        {
            float dist = Vector3.Distance(boss.transform.position, boss.player.position);
            return dist <= (boss.agent != null ? boss.agent.stoppingDistance + reachEpsilon : boss.chaseStoppingDistance + reachEpsilon);
        }

        if (boss.agent.pathPending) return false;
        return boss.agent.remainingDistance <= boss.agent.stoppingDistance + reachEpsilon;
    }
}
