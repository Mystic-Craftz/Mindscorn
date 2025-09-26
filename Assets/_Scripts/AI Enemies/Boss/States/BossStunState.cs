using System.Collections;
using UnityEngine;

public class BossStunState : IState
{
    private BossAI bossAI;
    private bool routineRunning = false;

    public BossStunState(BossAI bossAI)
    {
        this.bossAI = bossAI;
    }

    public void Enter()
    {
        if (bossAI != null && !routineRunning)
            bossAI.StartCoroutine(StunRoutine());
    }

    public void Exit() { }

    public void Update() { }

    private IEnumerator StunRoutine()
    {
        if (bossAI == null) yield break;
        routineRunning = true;

        bossAI.lockStateTransition = true;

        if (bossAI.agent != null) bossAI.agent.isStopped = true;

        var health = bossAI.health;
        if (health != null) health.ClearStunAccumulation();

        var animCtrl = bossAI.GetComponent<AIAnimationController>();

        if (animCtrl != null && !string.IsNullOrEmpty(bossAI.hit))
        {
            yield return bossAI.StartCoroutine(animCtrl.PlayAndWait(bossAI.hit));

            animCtrl.ForceLock();

            if (!string.IsNullOrEmpty(bossAI.stunned))
                animCtrl.PlayAnimation(bossAI.stunned, 0f);
        }
        else
        {
            var anim = bossAI.GetComponent<Animator>();
            if (anim != null && !string.IsNullOrEmpty(bossAI.hit))
            {
                anim.CrossFade(bossAI.hit, 0f);
                float timeout = 5f;
                float t = 0f;
                bool entered = false;
                while (t < timeout)
                {
                    var st = anim.GetCurrentAnimatorStateInfo(0);
                    if (st.IsName(bossAI.hit)) { entered = true; break; }
                    t += Time.deltaTime; yield return null;
                }

                if (entered)
                {
                    while (anim.GetCurrentAnimatorStateInfo(0).IsName(bossAI.hit) &&
                           anim.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
                    {
                        yield return null;
                    }
                }
                else
                {
                    Debug.LogWarning($"[BossStunState] Animator did not enter '{bossAI.hit}' within timeout.");
                }

                if (!string.IsNullOrEmpty(bossAI.stunned))
                    anim.CrossFade(bossAI.stunned, 0f);
            }
            else if (anim != null && !string.IsNullOrEmpty(bossAI.stunned))
            {
                anim.CrossFade(bossAI.stunned, 0f);
            }
        }

        float stunDuration = bossAI != null ? bossAI.stunDuration : 5f;
        float elapsed = 0f;
        while (elapsed < stunDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (animCtrl != null) animCtrl.ForceUnlock();

        bossAI.lockStateTransition = false;

        if (bossAI.agent != null) bossAI.agent.isStopped = false;

        if (bossAI.player != null)
            bossAI.stateMachine.ChangeState(bossAI.chaseState);
        else if (bossAI.lastKnownPlayerPosition != Vector3.zero)
            bossAI.stateMachine.ChangeState(bossAI.searchState);
        else
            bossAI.stateMachine.ChangeState(bossAI.wanderState);

        routineRunning = false;
    }
}
