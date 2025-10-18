using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class BossChaseState : IState
{
    private BossAI boss;
    private float originalSpeed;
    private bool originalUpdateRotation;
    private bool singingPlayed = false;
    private bool dashPreparationStarted = false;
    private bool dashTriggeredThisChase = false; // NEW: Track if dash was already triggered this chase

    public BossChaseState(BossAI boss)
    {
        this.boss = boss;
    }

    public void Enter()
    {
        boss.lockStateTransition = false;
        boss.isPreparingDash = false;
        boss.isInDashMode = false;
        dashPreparationStarted = false;
        dashTriggeredThisChase = false; // Reset dash trigger

        // Reset singing flag for this chase entry
        singingPlayed = false;
        boss.singingPlayedThisChase = false;

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

        // IMMEDIATELY check if we're already at stopping distance
        if (boss.player != null && IsWithinAttackRange())
        {
            // Debug.Log("Already in attack range, going directly to attack state");
            TransitionToAttack();
            return;
        }

        // Only check for dash if we're not already at attack range
        boss.queuedDash = Random.value < boss.dashChance;

        if (boss.queuedDash)
        {
            // Start dash preparation immediately - no singing in dash mode
            boss.isInDashMode = true;
            dashTriggeredThisChase = true;
            boss.StartCoroutine(PrepareForDash());
        }
        else
        {
            // Normal chase - play singing after delay
            boss.isInDashMode = false;
            boss.StartCoroutine(PlaySingingAfterDelay(1f));
            boss.anim?.ForceUnlock();
            boss.anim?.SetMoveSpeed(boss.agent != null ? boss.agent.speed : boss.chaseSpeed);
        }
    }

    private bool IsWithinAttackRange()
    {
        if (boss.player == null) return false;
        float distance = Vector3.Distance(boss.transform.position, boss.player.position);
        return distance <= boss.attackRange + 0.3f;
    }

    private IEnumerator PlaySingingAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (!singingPlayed && !boss.singingPlayedThisChase && !boss.isInDashMode)
        {
            boss.TryPlayOneShot3D(boss.singingSound);
            singingPlayed = true;
            boss.singingPlayedThisChase = true;
        }
    }

    private IEnumerator PrepareForDash()
    {
        // Check again if we reached attack range during the delay
        if (IsWithinAttackRange())
        {
            TransitionToAttack();
            yield break;
        }

        dashPreparationStarted = true;
        boss.isPreparingDash = true;

        // Stop movement during preparation
        if (boss.agent != null && boss.agent.isOnNavMesh)
        {
            boss.agent.isStopped = true;
            boss.agent.ResetPath();
        }

        // Play prepare for dash animation
        boss.anim?.ForceUnlock();
        boss.anim?.PlayAnimation(boss.prepareForDash, 0.1f);

        // Play laugh sound during preparation (only once)
        boss.TryPlayOneShot3D(boss.laughSound);

        // Wait for preparation to complete
        yield return new WaitForSeconds(boss.dashPreparationTime);

        // Check again if we reached attack range during preparation
        if (IsWithinAttackRange())
        {
            TransitionToAttack();
            yield break;
        }

        // Start actual dash
        boss.isPreparingDash = false;
        boss.isDashing = true;

        if (boss.agent != null && boss.agent.isOnNavMesh)
        {
            boss.agent.isStopped = false;
            boss.agent.speed = boss.chaseSpeed * boss.dashSpeedMultiplier;
        }

        boss.anim?.PlayAnimation(boss.dashing, 0.1f);
    }

    public void Update()
    {
        if (boss.player == null)
        {
            return;
        }

        // Check if we reached attack range - THIS IS THE MOST IMPORTANT CHECK
        if (IsWithinAttackRange())
        {
            TransitionToAttack();
            return;
        }

        // If preparing dash, don't process movement (but still check for attack range above)
        if (boss.isPreparingDash)
        {
            return;
        }

        // Check if player lost
        if (boss.sensor == null || !boss.sensor.PlayerInSight)
        {
            boss.anim?.SetMoveSpeed(0f);
            ResetDashStates();
            boss.stateMachine.ChangeState(boss.searchState);
            return;
        }

        // Continue chasing - only if not preparing dash and not in attack range
        if (boss.agent != null && boss.agent.isOnNavMesh && !boss.isPreparingDash)
        {
            boss.agent.isStopped = false;
            boss.agent.SetDestination(boss.player.position);

            // Update speed based on dash state
            if (boss.isDashing)
            {
                boss.agent.speed = boss.chaseSpeed * boss.dashSpeedMultiplier;
            }
            else
            {
                boss.agent.speed = boss.chaseSpeed;
            }
        }

        boss.lastKnownPlayerPosition = boss.player.position;

        // Rotate towards player
        Vector3 dir = boss.player.position - boss.transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            boss.transform.rotation = Quaternion.Slerp(boss.transform.rotation, targetRot, boss.rotationSpeed * Time.deltaTime);
        }

        // Update animation
        if (!boss.isPreparingDash)
        {
            bool moving = boss.agent != null && boss.agent.isOnNavMesh && boss.agent.velocity.sqrMagnitude > 0.01f;
            float animSpeed = moving ? (boss.isDashing ? boss.chaseSpeed * boss.dashSpeedMultiplier : boss.chaseSpeed) : 0f;
            boss.anim?.SetMoveSpeed(animSpeed);
        }
    }

    private void TransitionToAttack()
    {
        // Debug.Log("Transitioning to attack state from chase");

        // Stop any running coroutines
        boss.StopAllCoroutines();

        if (boss.agent != null && boss.agent.isOnNavMesh)
        {
            boss.agent.isStopped = true;
            boss.agent.ResetPath();
        }

        boss.anim?.SetMoveSpeed(0f);

        // Reset dash states
        ResetDashStates();

        // Force transition to attack
        boss.stateMachine.ChangeState(boss.attackState);
    }

    private void ResetDashStates()
    {
        boss.isDashing = false;
        boss.queuedDash = false;
        boss.isPreparingDash = false;
        boss.isInDashMode = false;
        dashTriggeredThisChase = false;
    }

    public void Exit()
    {
        // Stop any running coroutines
        if (boss != null)
        {
            boss.StopAllCoroutines();
        }

        // Reset states
        ResetDashStates();

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