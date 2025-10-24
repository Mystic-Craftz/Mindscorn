using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class BossChaseState : IState
{
    private BossAI boss;
    private float originalSpeed;
    private bool originalUpdateRotation;
    private bool dashPreparationStarted = false;
    private bool dashTriggeredThisChase = false;
    private Coroutine dashRoutine;

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
        dashTriggeredThisChase = false;
        boss.singingPlayedThisChase = false;

        if (boss.agent != null)
        {
            originalUpdateRotation = boss.agent.updateRotation;
            boss.agent.updateRotation = false;
            boss.agent.updatePosition = true;
            boss.agent.isStopped = false;
            boss.agent.stoppingDistance = boss.chaseStoppingDistance;
            originalSpeed = boss.agent.speed;
            boss.agent.speed = boss.chaseSpeed;
        }

        boss.ResetDashFlags();

        if (boss.player != null && !IsWithinAttackRange())
        {
            bool shouldDash = Random.value < boss.dashChance;
            if (shouldDash)
            {
                StartDashSequence();
            }
            else
            {
                boss.isInDashMode = false;
                boss.StartCoroutine(PlaySingingAfterDelay(1f));
                boss.anim?.ForceUnlock();
                boss.anim?.PlayAnimation("Locomotion");
                boss.anim?.SetMoveSpeed(boss.chaseSpeed);
            }
        }
        else if (boss.player != null && IsWithinAttackRange())
        {
            TransitionToAttack();
        }
    }

    private bool IsWithinAttackRange()
    {
        if (boss.player == null) return false;
        float distance = Vector3.Distance(boss.transform.position, boss.player.position);
        return distance <= boss.attackRange;
    }

    private IEnumerator PlaySingingAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (!boss.isInDashMode)
        {
            boss.TryPlaySingingOnce();
        }
    }

    private void StartDashSequence()
    {
        Debug.Log("Starting dash sequence!");
        boss.isInDashMode = true;
        dashTriggeredThisChase = true;

        boss.TriggerDashForNextAttack();

        if (dashRoutine != null)
        {
            boss.StopCoroutine(dashRoutine);
        }
        dashRoutine = boss.StartCoroutine(DashRoutine());
    }

    private IEnumerator DashRoutine()
    {
        Debug.Log("Dash routine started");

        dashPreparationStarted = true;
        boss.isPreparingDash = true;

        if (boss.agent != null && boss.agent.isOnNavMesh)
        {
            boss.agent.isStopped = true;
            boss.agent.ResetPath();
        }

        boss.anim?.ForceUnlock();
        boss.anim?.PlayAnimation(boss.prepareForDash, 0.1f);

        boss.TryPlayLaughOnce();

        float prepTime = boss.anim?.GetClipLength(boss.prepareForDash) ?? boss.dashPreparationTime;
        yield return new WaitForSeconds(prepTime);

        if (IsWithinAttackRange())
        {
            Debug.Log("Player in range after dash preparation, transitioning to attack");
            TransitionToAttack();
            yield break;
        }

        Debug.Log("Starting dash movement");
        boss.isPreparingDash = false;
        boss.isDashing = true;

        if (boss.agent != null && boss.agent.isOnNavMesh)
        {
            boss.agent.isStopped = false;
            boss.agent.speed = boss.chaseSpeed * boss.dashSpeedMultiplier;

            if (boss.player != null)
            {
                boss.agent.SetDestination(boss.player.position);
            }
        }

        boss.anim?.PlayAnimation(boss.dashing, 0.1f);

        float dashTime = 2f;
        float timer = 0f;

        while (timer < dashTime && boss.player != null && !IsWithinAttackRange())
        {
            timer += Time.deltaTime;

            if (boss.agent != null && boss.agent.isOnNavMesh && boss.player != null)
            {
                boss.agent.SetDestination(boss.player.position);
                boss.agent.speed = boss.chaseSpeed * boss.dashSpeedMultiplier;
            }

            yield return null;
        }

        if (IsWithinAttackRange())
        {
            Debug.Log("Reached player after dash, transitioning to attack");
            TransitionToAttack();
        }
        else
        {
            Debug.Log("Dash completed but player not in range, continuing chase");
            boss.isDashing = false;
            if (boss.agent != null)
            {
                boss.agent.speed = boss.chaseSpeed;
            }
            boss.anim?.PlayAnimation("Locomotion");
            boss.anim?.SetMoveSpeed(boss.chaseSpeed);
        }
    }

    public void Update()
    {
        if (boss.player == null) return;

        LookAtPlayer();

        if (IsWithinAttackRange())
        {
            TransitionToAttack();
            return;
        }

        if (boss.isPreparingDash) return; // Still wait for dash prep to finish

        if (boss.isDashing)
        {
            if (boss.agent != null)
            {
                bool move = boss.agent.isOnNavMesh && boss.agent.velocity.sqrMagnitude > 0.01f;
                float dashAnimSpeed = move ? boss.chaseSpeed * boss.dashSpeedMultiplier : 0f;
                boss.anim?.SetMoveSpeed(dashAnimSpeed);
            }
            return; // Return to let the DashRoutine manage movement
        }

        if (boss.sensor == null || !boss.sensor.PlayerInSight)
        {
            boss.anim?.SetMoveSpeed(0f);
            ResetDashStates();
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

    private void LookAtPlayer()
    {
        if (boss.player == null) return;

        Vector3 dir = boss.player.position - boss.transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            boss.transform.rotation = Quaternion.Slerp(boss.transform.rotation, targetRot, boss.rotationSpeed * Time.deltaTime);
        }
    }

    private void TransitionToAttack()
    {
        Debug.Log($"Transitioning to attack. nextAttackIsDash: {boss.nextAttackIsDash}, isDashing: {boss.isDashing}");

        if (dashRoutine != null)
        {
            boss.StopCoroutine(dashRoutine);
            dashRoutine = null;
        }

        boss.stateMachine.ChangeState(boss.attackState);
    }

    private void ResetDashStates()
    {
        if (dashRoutine != null)
        {
            boss.StopCoroutine(dashRoutine);
            dashRoutine = null;
        }

        boss.isDashing = false;
        boss.queuedDash = false;
        boss.isPreparingDash = false;
        boss.isInDashMode = false;
        dashTriggeredThisChase = false;
    }

    public void Exit()
    {
        boss.StopSinging();

        if (dashRoutine != null)
        {
            boss.StopCoroutine(dashRoutine);
            dashRoutine = null;
        }

        ResetDashStates();

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
}