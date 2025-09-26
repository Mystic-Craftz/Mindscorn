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
        boss.lockedInAfterSlash = false;

        attackRoutine = boss.StartCoroutine(AttackSequenceLoop());
    }

    public void Update()
    {
        if (isPerformingAttack || boss.lockedInAfterSlash) return;

        if (boss.player == null)
        {
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
    }

    private IEnumerator AttackSequenceLoop()
    {
        isPerformingAttack = true;

        while (true)
        {
            if (!ShouldContinueAttacking())
            {
                break;
            }

            string selectedClip = null;
            bool wasDashing = boss.queuedDash || boss.isDashing;
            boss.queuedDash = false;
            if (wasDashing)
            {
                selectedClip = boss.dashSlash;
                boss.isDashing = false;
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
                boss.lockedInAfterSlash = true;

                if (boss.agent != null)
                {
                    boss.agent.isStopped = true;
                    boss.agent.updatePosition = false;
                }

                yield return boss.anim.PlayAndWait(boss.afterSlash);

                boss.lockedInAfterSlash = false;

                if (boss.agent != null)
                {
                    boss.agent.updatePosition = true;
                }
            }

            if (!ShouldContinueAttacking())
            {
                break;
            }

            yield return new WaitForSeconds(0.1f);
        }

        isPerformingAttack = false;
        boss.lockedInAfterSlash = false;
        attackRoutine = null;

        DetermineNextState();
    }

    private bool ShouldContinueAttacking()
    {
        if (boss.player == null) return false;
        if (boss.sensor == null) return false;

        float distSqr = (boss.player.position - boss.transform.position).sqrMagnitude;
        bool inRange = distSqr <= attackRangeSqr * 1.2f;
        bool inSight = boss.sensor.PlayerInSight;

        return inRange && inSight;
    }

    private void DetermineNextState()
    {
        if (boss.player == null)
        {
            boss.stateMachine.ChangeState(boss.searchState);
            return;
        }

        float distSqr = (boss.player.position - boss.transform.position).sqrMagnitude;
        bool inSight = boss.sensor != null && boss.sensor.PlayerInSight;

        if (inSight)
        {
            if (distSqr <= attackRangeSqr * 1.1f)
            {
                attackRoutine = boss.StartCoroutine(AttackSequenceLoop());
            }
            else
            {
                boss.stateMachine.ChangeState(boss.chaseState);
            }
        }
        else
        {
            boss.OnPlayerLost();
        }
    }

    public void Exit()
    {
        if (attackRoutine != null)
        {
            boss.StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        isPerformingAttack = false;
        boss.lockedInAfterSlash = false;

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
