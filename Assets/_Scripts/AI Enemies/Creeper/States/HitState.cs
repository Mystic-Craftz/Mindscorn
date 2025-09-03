using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class HitState : IState
{
    private readonly MonsterAI monsterAI;
    private readonly NavMeshAgent agent;
    private readonly AIAnimationController aiAnimator;
    private readonly AIHealth aiHealth;
    private float originalAgentSpeed;
    private Coroutine _hitCoroutine;
    public Type StateType => typeof(HitState);

    public HitState(MonsterAI monsterAI)
    {
        this.monsterAI = monsterAI;
        agent = monsterAI.agent;
        aiAnimator = monsterAI.aiAnimator;
        aiHealth = monsterAI.aiHealth;
    }

    public void Enter()
    {
        monsterAI.StopAllStateSounds();
        aiAnimator.ForceUnlock();
        originalAgentSpeed = agent.speed;
        agent.speed = 0f;
        agent.isStopped = true;

        monsterAI.isProcessingHit = true;
        _hitCoroutine = monsterAI.StartCoroutine(DoHitSequence());
    }

    public void Update() { }

    public void Exit()
    {
        int soundId = monsterAI.GetStateSoundId(typeof(HitState));
        AudioManager.Instance.StopStateSound(soundId);

        if (_hitCoroutine != null)
            monsterAI.StopCoroutine(_hitCoroutine);

        agent.isStopped = false;
        agent.speed = originalAgentSpeed;
        monsterAI.isProcessingHit = false;
    }

    private IEnumerator DoHitSequence()
    {
        try
        {
            // helper that now respects isTrembling
            bool CheckDeath(bool forceNormalDeath = false)
            {
                if (aiHealth.currentHealth <= 0f)
                {
                    monsterAI.StopAllCoroutines();
                    monsterAI.queuedStateAfterHit = null;
                    monsterAI.queuedStateAfterResurrection = null;
                    monsterAI.isProcessingHit = false;

                    if (forceNormalDeath)
                        monsterAI.lastHitWasHard = false;
                    else if (monsterAI.isTrembling)
                        monsterAI.lastHitWasHard = true;

                    monsterAI.stateMachine.ChangeState(monsterAI.dieState);
                    return true;
                }
                return false;
            }

            // Cache & pick clips
            Vector3 forward = monsterAI.transform.forward;
            Vector3 hitDir = monsterAI.lastHitDirection;
            bool fromFront = Vector3.Dot(forward, hitDir) < 0f;
            bool isHard = monsterAI.lastHitWasHard;

            string normalClip = fromFront
                ? monsterAI.hitNormalFrontAnim
                : monsterAI.hitNormalBackAnim;
            string hardClip = fromFront
                ? monsterAI.hitHardFrontAnim
                : monsterAI.hitHardBackAnim;
            string lieClip = fromFront
                ? monsterAI.lieBackAnim
                : monsterAI.lieFrontAnim;
            string trembleClip = fromFront
                ? monsterAI.trembleBackAnim
                : monsterAI.trembleFrontAnim;
            string getUpClip = fromFront
                ? monsterAI.getUpFrontAnim
                : monsterAI.getUpBackAnim;

            // HARD‐HIT path
            if (isHard)
            {
                if (monsterAI.hardKnockbackDistance > 0f)
                {
                    monsterAI.StartCoroutine(
                        ApplyHardKnockback(
                            hitDir,
                            monsterAI.hardKnockbackDistance,
                            monsterAI.hardKnockbackDuration
                        )
                    );
                }

                // 1) Impact
                monsterAI.PlayHitSound();
                yield return aiAnimator.PlayAndWait(hardClip, 0.1f);
                if (CheckDeath()) yield break;

                // 2) Fall-down
                yield return aiAnimator.PlayAndWait(lieClip, 0.1f);
                if (CheckDeath()) yield break;

                // 3) Tremble
                yield return new WaitForSeconds(1f);
                monsterAI.isTrembling = true;
                aiAnimator.PlayAnimation(trembleClip);
                monsterAI.PlayTremblingSound();
                yield return new WaitForSeconds(monsterAI.onGroundTime);
                if (CheckDeath()) yield break;
                monsterAI.isTrembling = false;

                // 4) Get-up
                yield return aiAnimator.PlayAndWait(getUpClip, 0.1f);
                if (CheckDeath(forceNormalDeath: true)) yield break;
            }
            else
            {
                // NORMAL‐HIT
                monsterAI.PlayHitSound();
                yield return aiAnimator.PlayAndWait(normalClip, 0.1f);
                if (CheckDeath()) yield break;
            }

            // survived the hit
            monsterAI.isProcessingHit = false;

            if (monsterAI.lastHitWasStun)
            {
                monsterAI.lastHitWasStun = false;
                monsterAI.stateMachine.ChangeState(monsterAI.stunState);
                yield break;
            }

            if (monsterAI.queuedStateAfterHit != null)
            {
                var next = monsterAI.queuedStateAfterHit;
                monsterAI.queuedStateAfterHit = null;
                monsterAI.stateMachine.ChangeState(next);
                yield break;
            }
        }
        finally
        {
            // ** guaranteed exit from hitState if nothing else ran**
            if (monsterAI.stateMachine.CurrentState == monsterAI.hitState)
            {
                var next = monsterAI.queuedStateAfterHit ?? monsterAI.chaseState;
                monsterAI.queuedStateAfterHit = null;
                monsterAI.stateMachine.ChangeState(next);
            }
        }
    }

    private IEnumerator ApplyHardKnockback(Vector3 direction, float distance, float duration)
    {
        float elapsed = 0f;
        Vector3 start = monsterAI.transform.position;
        Vector3 target = start + direction.normalized * distance;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            Vector3 next = Vector3.Lerp(start, target, t);
            agent.Move(next - monsterAI.transform.position);
            elapsed += Time.deltaTime;
            yield return null;
        }
        agent.Move(target - monsterAI.transform.position);
    }
}
