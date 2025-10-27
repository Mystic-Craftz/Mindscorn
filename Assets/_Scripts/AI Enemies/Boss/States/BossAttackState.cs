using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class BossAttackState : IState
{
    private BossAI boss;
    private bool isPerformingAttack = false;
    private Coroutine attackRoutine = null;
    private Coroutine agentSyncCoroutine = null;

    // How far outside attackRange we allow before leaving attack (prevents jitter)
    private const float attackExitBuffer = 0.25f; // meters

    public BossAttackState(BossAI boss)
    {
        this.boss = boss;
    }

    public void Enter()
    {
        Debug.Log("Entering Attack State.");

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

        if (!ShouldContinueAttacking())
        {
            if (attackRoutine != null)
            {
                boss.StopCoroutine(attackRoutine);
                attackRoutine = null;
            }
            isPerformingAttack = false;
            boss.lockStateTransition = false;

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
        bool specialMode = boss.pendingSpecialAttack;

        while (specialMode || ShouldContinueAttacking())
        {
            isPerformingAttack = true;
            boss.lockStateTransition = true;

            //  ----------  special attack sequence (unchanged) ----------
            if (specialMode)
            {
                boss.TryPlayOneShot2D(boss.itBeginsSound);
                PauseAgentForAnimation();
                if (!string.IsNullOrEmpty(boss.lifting)) yield return boss.anim.PlayAndWait(boss.lifting);
                if (!string.IsNullOrEmpty(boss.liftingIdle)) boss.anim.PlayAnimation(boss.liftingIdle);

                float idleClipLen = boss.anim.GetClipLength(boss.liftingIdle);
                float waitTime = Mathf.Max(idleClipLen, boss.liftingIdleDuration);

                bool applied = false;
                if (!applied)
                {
                    applied = true;
                    NeonDimensionController.Instance.PlayGlitch(boss.specialGlitchIntensity);
                    PlayerHealth.Instance.TakeDamage(boss.specialAttackDamage);
                }

                float elapsed = 0f;
                while (elapsed < waitTime)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                ResumeAgentAfterAnimation(true);

                boss.pendingSpecialAttack = false;
                specialMode = false;

                boss.canRollForSpecial = false;
                boss.specialRollCooldownTimer = boss.specialPostAttackCooldown;

                isPerformingAttack = false;
                boss.lockStateTransition = false;

                yield return null;
                continue;
            }

            // ---------- Normal attack selection (unchanged) ----------
            string selectedClip = null;
            int pick = Random.Range(0, 3);
            switch (pick)
            {
                case 0: selectedClip = boss.slash_1; break;
                case 1: selectedClip = boss.slash_2; break;
                default: selectedClip = boss.slashBoth; break;
            }

            if (boss.player != null)
            {
                Vector3 lookDir = boss.player.position - boss.transform.position;
                lookDir.y = 0f;
                if (lookDir.sqrMagnitude > 0.0001f)
                {
                    boss.transform.rotation = Quaternion.LookRotation(lookDir);
                }
            }

            if (!string.IsNullOrEmpty(selectedClip))
            {
                boss.TryPlayOneShot3D(boss.attackSound);
                yield return boss.anim.PlayAndWait(selectedClip);
            }

            if (!string.IsNullOrEmpty(boss.afterSlash))
            {
                PauseAgentForAnimation();
                boss.TryPlayOneShot3D(boss.laughSound);
                yield return boss.anim.PlayAndWait(boss.afterSlash);
                ResumeAgentAfterAnimation(true);
            }

            yield return new WaitForSeconds(0.5f);

            isPerformingAttack = false;
            boss.lockStateTransition = false;

            yield return null;
        }

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
        float dist = Vector3.Distance(boss.player.position, boss.transform.position);

        // Use an exit buffer so we only stop attacking once the player is clearly outside the attackRange + buffer
        return dist <= (boss.attackRange + attackExitBuffer);
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
    }
}
