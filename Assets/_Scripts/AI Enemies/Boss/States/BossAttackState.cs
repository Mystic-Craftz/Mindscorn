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

        // --- CHANGE ---
        // Start the *looping* coroutine
        attackRoutine = boss.StartCoroutine(AttackSequenceLoop());
    }

    public void Update()
    {
        // If we are busy with an animation, wait.
        if (boss.lockStateTransition) return;

        if (boss.player == null)
        {
            boss.stateMachine.ChangeState(boss.searchState);
            return;
        }

        // Always face the player when in attack state and not busy
        Vector3 dir = boss.player.position - boss.transform.position;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.0001f)
        {
            var targetRot = Quaternion.LookRotation(dir);
            boss.transform.rotation = Quaternion.Slerp(boss.transform.rotation, targetRot, boss.rotationSpeed * Time.deltaTime);
        }

        // --- CHANGE ---
        // The coroutine now handles the attack loop.
        // Update's job is to transition *out* of the attack state
        // if the player is no longer in range.
        if (!ShouldContinueAttacking())
        {
            if (attackRoutine != null)
            {
                boss.StopCoroutine(attackRoutine);
                attackRoutine = null;
            }
            isPerformingAttack = false;

            // Decide where to go next
            if (boss.sensor != null && boss.sensor.PlayerInSight)
            {
                boss.stateMachine.ChangeState(boss.chaseState);
            }
            else
            {
                boss.stateMachine.ChangeState(boss.searchState);
            }
        }

    }

    private IEnumerator AttackSequenceLoop()
    {
        // --- CHANGE ---
        // This is now a loop that continues as long as the player
        // is in range and alive.
        while (ShouldContinueAttacking())
        {
            isPerformingAttack = true;
            boss.lockStateTransition = true; // Lock state during animation

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
                PauseAgentForAnimation();
                boss.TryPlayOneShot3D(boss.laughSound);
                yield return boss.anim.PlayAndWait(boss.afterSlash);
                ResumeAgentAfterAnimation(true);
            }

            // Brief pause between attacks
            yield return new WaitForSeconds(0.5f);

            // We are no longer busy, allow Update to check range again
            // or this loop to re-evaluate.
            isPerformingAttack = false;
            boss.lockStateTransition = false;

            // Yield one frame to allow Update to run and potentially
            // transition state if the player left range during the attack.
            yield return null;
        }

        // If the loop exits (ShouldContinueAttacking is false),
        // set flags and let Update handle the state transition.
        isPerformingAttack = false;
        boss.lockStateTransition = false;
        attackRoutine = null;
    }

    private IEnumerator SyncAgentToTransform()
    {
        var agent = boss.agent;
        if (agent == null) yield break;

        while (true)
        {
            agent.nextPosition = boss.transform.position;
            yield return null;
        }
    }

    private bool ShouldContinueAttacking()
    {
        if (boss.player == null) return false;
        float distSqr = (boss.player.position - boss.transform.position).sqrMagnitude;
        return distSqr <= attackRangeSqr;
    }

    private void StartAgentSync()
    {
        if (agentSyncCoroutine == null)
            agentSyncCoroutine = boss.StartCoroutine(SyncAgentToTransform());
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
        boss.agent.updatePosition = false;
        boss.agent.velocity = Vector3.zero;
        boss.agent.nextPosition = boss.transform.position;
    }

    private void ResumeAgentAfterAnimation(bool warpAgent = true)
    {
        if (boss.agent == null) return;
        if (warpAgent && boss.agent.isOnNavMesh)
        {
            boss.agent.Warp(boss.transform.position);
            boss.agent.nextPosition = boss.transform.position;
        }
        boss.agent.updatePosition = true;
    }

    public void Exit()
    {
        StopAgentSync();

        if (attackRoutine != null)
        {
            boss.StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        boss.lockStateTransition = false;
        isPerformingAttack = false;

        if (boss.agent != null)
        {
            boss.agent.isStopped = false;
            boss.agent.updateRotation = true;
            boss.agent.updatePosition = true;
        }

        boss.ResetDashFlags();
    }
}