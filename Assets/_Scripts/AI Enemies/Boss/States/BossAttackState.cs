using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class BossAttackState : IState
{
    private BossAI boss;
    private float attackRangeSqr;
    private bool isPerformingAttack = false;
    private Coroutine attackRoutine = null;
    private Coroutine agentSyncCoroutine = null;

    public BossAttackState(BossAI boss)
    {
        this.boss = boss;
        attackRangeSqr = boss.attackRange * boss.attackRange;
    }

    public void Enter()
    {
        Debug.Log($"Entering Attack State. nextAttackIsDash: {boss.nextAttackIsDash}, isDashing: {boss.isDashing}");

        if (boss.agent != null)
        {
            boss.agent.ResetPath();
            boss.agent.isStopped = true;
            boss.agent.updateRotation = false;
            boss.agent.updatePosition = false;
            boss.agent.velocity = Vector3.zero;
            boss.agent.nextPosition = boss.transform.position;
        }

        StartAgentSync();
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
        if (!isPerformingAttack && !boss.lockStateTransition)
        {
            if (boss.player == null)
            {
                boss.stateMachine.ChangeState(boss.searchState);
                return;
            }

            // Face the player
            Vector3 dir = boss.player.position - boss.transform.position;
            dir.y = 0;
            if (dir.sqrMagnitude > 0.0001f)
            {
                var targetRot = Quaternion.LookRotation(dir);
                boss.transform.rotation = Quaternion.Slerp(boss.transform.rotation, targetRot, boss.rotationSpeed * Time.deltaTime);
            }

            if (!ShouldContinueAttacking())
            {
                DetermineNextState();
            }
        }
    }

    private IEnumerator AttackSequenceLoop()
    {
        isPerformingAttack = true;

        while (ShouldContinueAttacking())
        {
            string selectedClip = null;
            bool useDashSlash = boss.nextAttackIsDash;

            Debug.Log($"Choosing attack. useDashSlash: {useDashSlash}, nextAttackIsDash: {boss.nextAttackIsDash}");

            if (useDashSlash)
            {
                selectedClip = boss.dashSlash;
                Debug.Log("Selected DASH SLASH attack!");
                // Clear dash flags AFTER we confirm we're using the dash slash
                boss.ResetDashFlags();
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
                Debug.Log($"Selected normal attack: {selectedClip}");
            }

            // Face player before attacking
            if (boss.player != null)
            {
                Vector3 lookDir = boss.player.position - boss.transform.position;
                lookDir.y = 0f;
                if (lookDir.sqrMagnitude > 0.0001f)
                {
                    boss.transform.rotation = Quaternion.LookRotation(lookDir);
                }
            }

            // Play the attack
            if (!string.IsNullOrEmpty(selectedClip))
            {
                boss.TryPlayOneShot3D(boss.attackSound);
                yield return boss.anim.PlayAndWait(selectedClip);
            }

            // Play after-slash animation if available
            if (!string.IsNullOrEmpty(boss.afterSlash) && !useDashSlash)
            {
                boss.lockStateTransition = true;
                PauseAgentForAnimation();
                boss.TryPlayOneShot3D(boss.laughSound);
                yield return boss.anim.PlayAndWait(boss.afterSlash);
                boss.lockStateTransition = false;
                ResumeAgentAfterAnimation(true);
            }

            // Brief pause between attacks
            yield return new WaitForSeconds(0.5f);

            if (!ShouldContinueAttacking()) break;
        }

        isPerformingAttack = false;
        boss.lockStateTransition = false;
        attackRoutine = null;
        DetermineNextState();
    }

    private IEnumerator SyncAgentToTransform()
    {
        var agent = boss.agent;
        if (agent == null) yield break;

        while (true)
        {
            agent.nextPosition = boss.transform.position;
            agent.velocity = Vector3.zero;

            float sqr = (agent.nextPosition - boss.transform.position).sqrMagnitude;
            if (sqr > 4f)
            {
                agent.Warp(boss.transform.position);
                agent.ResetPath();
                agent.nextPosition = boss.transform.position;
            }

            yield return null;
        }
    }

    private void StartAgentSync()
    {
        if (boss.agent == null) return;
        if (agentSyncCoroutine == null)
        {
            agentSyncCoroutine = boss.StartCoroutine(SyncAgentToTransform());
        }
    }

    private void StopAgentSync()
    {
        if (agentSyncCoroutine != null)
        {
            boss.StopCoroutine(agentSyncCoroutine);
            agentSyncCoroutine = null;
        }
    }

    private void PauseAgentForAnimation()
    {
        if (boss.agent == null) return;

        boss.agent.ResetPath();
        boss.agent.isStopped = true;
        boss.agent.updatePosition = false;
        boss.agent.updateRotation = false;
        boss.agent.velocity = Vector3.zero;
        boss.agent.nextPosition = boss.transform.position;
    }

    private void ResumeAgentAfterAnimation(bool allowMoveImmediately)
    {
        if (boss.agent == null) return;

        boss.agent.nextPosition = boss.transform.position;
        boss.agent.velocity = Vector3.zero;

        boss.agent.updatePosition = true;
        boss.agent.updateRotation = true;

        if (allowMoveImmediately)
        {
            boss.agent.isStopped = false;
            if (boss.player != null) boss.agent.SetDestination(boss.player.position);
        }
        else
        {
            boss.agent.isStopped = true;
        }
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
                // Continue attacking
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
        Debug.Log("Exiting Attack State");

        if (attackRoutine != null)
        {
            boss.StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        isPerformingAttack = false;
        boss.lockStateTransition = false;

        StopAgentSync();

        if (boss.agent != null)
        {
            boss.agent.nextPosition = boss.transform.position;
            boss.agent.velocity = Vector3.zero;
            boss.agent.isStopped = false;
            boss.agent.updateRotation = true;
            boss.agent.updatePosition = true;
        }

        boss.anim?.ForceUnlock();
    }
}