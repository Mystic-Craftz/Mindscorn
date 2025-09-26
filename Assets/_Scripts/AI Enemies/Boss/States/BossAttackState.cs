using System.Collections;
using UnityEngine;

public class BossAttackState : IState
{
    private BossAI boss;
    private float attackRangeSqr;
    private bool isPerformingAttack = false;
    private Coroutine attackRoutine = null;

    public BossAttackState(BossAI boss)
    {
        this.boss = boss;
        attackRangeSqr = boss.attackRange * boss.attackRange;
    }

    public void Enter()
    {
        Debug.Log("Entered Attack State");

        if (boss.agent != null)
        {
            boss.agent.isStopped = true;
            boss.agent.updateRotation = false;
            boss.agent.updatePosition = false;
        }

        boss.StopAllStateSounds();

        if (attackRoutine != null)
        {
            boss.StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        isPerformingAttack = false;
        boss.lockStateTransition = false;

        attackRoutine = boss.StartCoroutine(AttackSequenceLoop());
    }

    public void Update()
    {
        if (isPerformingAttack || boss.lockStateTransition) return;

        if (boss.player == null)
        {
            Debug.Log("Player is null in Attack State, transitioning to Search");
            boss.stateMachine.ChangeState(boss.searchState);
            return;
        }

        // Face the player even when not actively attacking
        Vector3 dir = boss.player.position - boss.transform.position;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.0001f)
        {
            var targetRot = Quaternion.LookRotation(dir);
            boss.transform.rotation = Quaternion.Slerp(boss.transform.rotation, targetRot, boss.rotationSpeed * Time.deltaTime);
        }

        // Additional check for player leaving attack range during non-attack moments
        if (!isPerformingAttack && !ShouldContinueAttacking())
        {
            Debug.Log("Player left attack range during attack state update");
            DetermineNextState();
        }
    }

    private IEnumerator AttackSequenceLoop()
    {
        isPerformingAttack = true;
        Debug.Log("Starting attack sequence");

        while (true)
        {
            if (!ShouldContinueAttacking())
            {
                Debug.Log("Breaking attack sequence - should not continue attacking");
                break;
            }

            string selectedClip = null;
            bool wasDashing = boss.queuedDash || boss.isDashing;
            boss.queuedDash = false;
            if (wasDashing)
            {
                selectedClip = boss.dashSlash;
                boss.isDashing = false;
                Debug.Log("Performing dash attack");
            }
            else
            {
                int pick = Random.Range(0, 3);
                switch (pick)
                {
                    case 0: selectedClip = boss.slash_1; break;
                    case 1: selectedClip = boss.slash_2; break;
                    default: selectedClip = boss.slashBoth; break;
                }
                Debug.Log($"Performing regular attack: {selectedClip}");
            }

            if (boss.player != null)
            {
                Vector3 lookDir = boss.player.position - boss.transform.position;
                lookDir.y = 0f;
                if (lookDir.sqrMagnitude > 0.0001f)
                {
                    boss.transform.rotation = Quaternion.Slerp(boss.transform.rotation, Quaternion.LookRotation(lookDir), 0.6f);
                }
            }

            if (!string.IsNullOrEmpty(selectedClip))
            {
                yield return boss.anim.PlayAndWait(selectedClip);
            }

            if (!string.IsNullOrEmpty(boss.afterSlash))
            {
                boss.lockStateTransition = true;

                if (boss.agent != null)
                {
                    boss.agent.isStopped = true;
                    boss.agent.updatePosition = false;
                }

                yield return boss.anim.PlayAndWait(boss.afterSlash);

                boss.lockStateTransition = false;

                if (boss.agent != null)
                {
                    boss.agent.updatePosition = true;
                }
            }

            if (!ShouldContinueAttacking())
            {
                Debug.Log("Breaking attack sequence after attack - should not continue");
                break;
            }

            yield return new WaitForSeconds(0.1f);
        }

        isPerformingAttack = false;
        boss.lockStateTransition = false;
        attackRoutine = null;

        Debug.Log("Attack sequence finished, determining next state");
        DetermineNextState();
    }

    private bool ShouldContinueAttacking()
    {
        if (boss.player == null)
        {
            Debug.Log("ShouldContinueAttacking: Player is null");
            return false;
        }
        if (boss.sensor == null)
        {
            Debug.Log("ShouldContinueAttacking: Sensor is null");
            return false;
        }

        float distSqr = (boss.player.position - boss.transform.position).sqrMagnitude;
        bool inRange = distSqr <= attackRangeSqr * 1.2f;
        bool inSight = boss.sensor.PlayerInSight;

        Debug.Log($"ShouldContinueAttacking: InRange={inRange}, InSight={inSight}, DistSqr={distSqr}, AttackRangeSqr={attackRangeSqr}");

        return inRange && inSight;
    }

    private void DetermineNextState()
    {
        Debug.Log("Determining next state from Attack State");

        if (boss.player == null)
        {
            Debug.Log("Player is null, going to Search State");
            boss.stateMachine.ChangeState(boss.searchState);
            return;
        }

        float distSqr = (boss.player.position - boss.transform.position).sqrMagnitude;
        bool inSight = boss.sensor != null && boss.sensor.PlayerInSight;

        Debug.Log($"DetermineNextState: DistSqr={distSqr}, InSight={inSight}");

        if (inSight)
        {
            if (distSqr <= attackRangeSqr * 1.1f)
            {
                Debug.Log("Player still in range, continuing attack sequence");
                attackRoutine = boss.StartCoroutine(AttackSequenceLoop());
            }
            else
            {
                Debug.Log("Player out of range, going to Chase State");
                boss.stateMachine.ChangeState(boss.chaseState);
            }
        }
        else
        {
            Debug.Log("Player lost sight, calling OnPlayerLost");
            boss.OnPlayerLost();
        }
    }

    public void Exit()
    {
        Debug.Log("Exiting Attack State");

        if (attackRoutine != null)
        {
            boss.StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        isPerformingAttack = false;
        boss.lockStateTransition = false;

        if (boss.agent != null)
        {
            boss.agent.isStopped = false;
            boss.agent.updateRotation = true;
            boss.agent.updatePosition = true;
        }

        boss.anim?.ForceUnlock();
        boss.queuedDash = false;
        boss.isDashing = false;
    }
}