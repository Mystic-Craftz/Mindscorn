using System.Collections;
using UnityEngine;

public class BossStunState : IState
{
    private BossAI bossAI;
    private Coroutine stunRoutine;
    private bool routineRunning = false;

    public BossStunState(BossAI bossAI)
    {
        this.bossAI = bossAI;
    }

    public void Enter()
    {
        if (routineRunning) return;
        stunRoutine = bossAI.StartCoroutine(StunCoroutine());
    }

    public void Exit()
    {
        if (stunRoutine != null)
        {
            try { bossAI.StopCoroutine(stunRoutine); } catch { }
            stunRoutine = null;
        }

        routineRunning = false;
        bossAI.lockStateTransition = false;
        if (bossAI.agent != null) bossAI.agent.isStopped = false;
    }

    public void Update() { }

    private IEnumerator StunCoroutine()
    {
        routineRunning = true;
        bossAI.lockStateTransition = true;

        bossAI.health?.ClearStunAccumulation();

        if (bossAI.agent != null)
        {
            bossAI.agent.isStopped = true;
            bossAI.agent.ResetPath();
        }

        // Start hit animation (non-blocking) so visual lines up with hit SFX
        if (bossAI.anim != null)
        {
            bossAI.anim.SetMoveSpeed(0f);
            bossAI.anim.PlayAnimation(bossAI.hit, 0f);
        }

        // Play hit SFX and WAIT until it finishes
        if (!bossAI.hitSound.IsNull)
        {
            yield return bossAI.StartCoroutine(bossAI.PlayEventAndWait(bossAI.hitSound));
        }

        // After hit SFX finished -> switch to stunned animation
        if (bossAI.anim != null)
        {
            bossAI.anim.SetMoveSpeed(0f);
            bossAI.anim.PlayAnimation(bossAI.stunned, 0f);
        }

        // Breathing loop during stun
        float elapsed = 0f;
        float duration = Mathf.Max(0f, bossAI.stunDuration);

        float timeSinceLastBreath = 0f;
        float nextBreathInterval = Mathf.Max(0.01f, bossAI.breathIntervalMin);
        if (bossAI.breathIntervalMax > bossAI.breathIntervalMin)
            nextBreathInterval = Random.Range(bossAI.breathIntervalMin, bossAI.breathIntervalMax);

        while (elapsed < duration)
        {
            bossAI.anim?.SetMoveSpeed(0f);

            float dt = Time.deltaTime;
            elapsed += dt;
            timeSinceLastBreath += dt;

            if (bossAI.playBreathInStun && timeSinceLastBreath >= nextBreathInterval)
            {
                if (!bossAI.breathSound.IsNull)
                {
                    bossAI.TryPlayOneShot3D(bossAI.breathSound);
                }

                timeSinceLastBreath = 0f;
                if (bossAI.breathIntervalMax > bossAI.breathIntervalMin)
                    nextBreathInterval = Random.Range(bossAI.breathIntervalMin, bossAI.breathIntervalMax);
                else
                    nextBreathInterval = bossAI.breathIntervalMin;
            }

            yield return null;
        }

        // restore agent and state
        if (bossAI.agent != null)
            bossAI.agent.isStopped = false;

        bossAI.lockStateTransition = false;
        routineRunning = false;
        stunRoutine = null;

        if (bossAI.anim != null)
        {
            bossAI.anim.SetMoveSpeed(0f);
            bossAI.anim.PlayAnimation("Locomotion", 0.12f);
        }

        // switch to next state
        if (bossAI.player != null)
            bossAI.stateMachine.ChangeState(bossAI.chaseState);
        else if (bossAI.lastKnownPlayerPosition != Vector3.zero)
            bossAI.stateMachine.ChangeState(bossAI.searchState);
        else
            bossAI.stateMachine.ChangeState(bossAI.wanderState);
    }
}
