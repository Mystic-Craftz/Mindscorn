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

        //  play hit and wait
        if (bossAI.anim != null)
            yield return bossAI.StartCoroutine(bossAI.anim.PlayAndWait(bossAI.hit));

        if (bossAI.anim != null)
        {
            bossAI.anim.SetMoveSpeed(0f);
            bossAI.anim.PlayAnimation(bossAI.stunned, 0f);
        }

        float elapsed = 0f;
        float duration = Mathf.Max(0f, bossAI.stunDuration);
        while (elapsed < duration)
        {
            bossAI.anim?.SetMoveSpeed(0f);
            elapsed += Time.deltaTime;
            yield return null;
        }


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
