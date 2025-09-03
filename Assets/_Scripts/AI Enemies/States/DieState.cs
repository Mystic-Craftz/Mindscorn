using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class DieState : IState
{
    private readonly MonsterAI monsterAI;
    private readonly NavMeshAgent agent;
    private readonly AIAnimationController aiAnimator;

    public Type StateType => typeof(DieState);

    public DieState(MonsterAI monsterAI)
    {
        this.monsterAI = monsterAI;
        this.agent = monsterAI.agent;
        this.aiAnimator = monsterAI.aiAnimator;
    }

    public void Enter()
    {
        monsterAI.isTrembling = false;

        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.updatePosition = false;
            agent.updateRotation = false;
        }

        monsterAI.isProcessingHit = false;
        monsterAI.StartCoroutine(DoDieAnimation());
        monsterAI.SetPathBlockingHitboxesEnabled(false);

    }

    public void Update() { }

    public void Exit()
    {
        int soundId = monsterAI.GetStateSoundId(typeof(DieState));
        AudioManager.Instance.StopStateSound(soundId);
    }

    private IEnumerator DoDieAnimation()
    {
        bool fromFront = Vector3.Dot(monsterAI.transform.forward, monsterAI.lastHitDirection) < 0f;
        string clip;

        if (monsterAI.lastHitWasHard)
        {
            clip = fromFront
                ? monsterAI.lieBackAnim
                : monsterAI.lieFrontAnim;
        }
        else
        {
            clip = monsterAI.deadAnim;
        }

        monsterAI.PlayDieSound();
        yield return aiAnimator.PlayAndWait(clip, 0.1f);
        monsterAI.stateMachine.ChangeState(monsterAI.incapacitatedState);
    }
}
