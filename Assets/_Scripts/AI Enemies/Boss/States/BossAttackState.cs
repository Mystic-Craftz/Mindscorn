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
        if (boss.agent != null)
        {
            // Stop the agent from moving/applying its internal position to transform
            boss.agent.ResetPath();
            boss.agent.isStopped = true;
            boss.agent.updateRotation = false;
            boss.agent.updatePosition = false;
            boss.agent.velocity = Vector3.zero;
            // Keep internal agent position equal to the transform right away
            boss.agent.nextPosition = boss.transform.position;
        }

        // Start syncing agent internal position to the transform each frame
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
        // While we're performing an attack or locked, the Sync coroutine handles sync.
        // When not attacking, handle facing / range checks as before.
        if (!isPerformingAttack && !boss.lockStateTransition)
        {
            if (boss.player == null)
            {
                boss.stateMachine.ChangeState(boss.searchState);
                return;
            }

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
                // Play the attack sound at the start of the attack animation 
                boss.TryPlayOneShot3D(boss.attackSound);

                // Because updatePosition is false and the Sync coroutine is running,
                // there will be no teleport when the animation finishes.
                yield return boss.anim.PlayAndWait(selectedClip);
            }

            // Handle afterSlash animation (pause agent, play afterSlash, then resume)
            if (!string.IsNullOrEmpty(boss.afterSlash))
            {
                boss.lockStateTransition = true;

                // Ensure agent is stopped & synced while afterSlash runs
                PauseAgentForAnimation();

                // Play laugh as soon as afterSlash starts
                boss.TryPlayOneShot3D(boss.laughSound);

                // Play the afterSlash animation and wait (during this PlayAndWait the sync coroutine keeps nextPosition in sync)
                yield return boss.anim.PlayAndWait(boss.afterSlash);

                boss.lockStateTransition = false;

                // Resume agent movement and allow it to navigate again.
                // We pass 'true' so the agent starts moving toward the player immediately (if player exists).
                ResumeAgentAfterAnimation(true);
            }

            if (!ShouldContinueAttacking())
            {
                break;
            }

            yield return new WaitForSeconds(0.1f);
        }

        isPerformingAttack = false;
        boss.lockStateTransition = false;
        attackRoutine = null;
        DetermineNextState();
    }


    // Coroutine that keeps the agent's internal position (nextPosition) equal to the visible transform
    // to avoid internal drift / teleportation when re-enabling agent updates.
    private IEnumerator SyncAgentToTransform()
    {
        var agent = boss.agent;
        if (agent == null) yield break;

        while (true)
        {
            // keep agent internal pos equal to transform each frame
            agent.nextPosition = boss.transform.position;
            agent.velocity = Vector3.zero;

            // safety: if there's a large mismatch, warp (rare)
            float sqr = (agent.nextPosition - boss.transform.position).sqrMagnitude;
            if (sqr > 4f) // > 2 meters squared threshold
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


    // Pause the NavMeshAgent properly for animation: clear path, stop applying position,
    // zero velocity and set internal pos to transform so there's no drift.  
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


    // Resume the NavMeshAgent after animation finishes.
    // If allowMoveImmediately is true, agent.isStopped = false and we'll set a destination to the player (if available). 
    private void ResumeAgentAfterAnimation(bool allowMoveImmediately)
    {
        if (boss.agent == null) return;

        // final sync before enabling transforms
        boss.agent.nextPosition = boss.transform.position;
        boss.agent.velocity = Vector3.zero;

        boss.agent.updatePosition = true;
        boss.agent.updateRotation = true;

        if (allowMoveImmediately)
        {
            boss.agent.isStopped = false;
            if (boss.player != null)
            {
                boss.agent.SetDestination(boss.player.position);
            }
        }
        else
        {
            boss.agent.isStopped = true;
        }
    }

    private bool ShouldContinueAttacking()
    {
        if (boss.player == null)
        {
            return false;
        }
        if (boss.sensor == null)
        {
            return false;
        }

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
        boss.lockStateTransition = false;

        // stop the sync coroutine
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
        boss.queuedDash = false;
        boss.isDashing = false;
    }
}
