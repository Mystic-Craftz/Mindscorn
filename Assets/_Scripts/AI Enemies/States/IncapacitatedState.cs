using UnityEngine;
using UnityEngine.AI;

public class IncapacitatedState : IState
{
    private readonly MonsterAI ai;
    private readonly AISensor sensor;
    private readonly NavMeshAgent agent;
    private readonly AIAnimationController anim;

    public IncapacitatedState(MonsterAI monsterAI)
    {
        ai = monsterAI;
        sensor = ai.aiSensor;
        agent = ai.agent;
        anim = ai.aiAnimator;
    }

    public void Enter()
    {
        ai.isIncapacitated = true;
        ai.incapacitatedDetectionCount = 0;

        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.updatePosition = false;
            agent.updateRotation = false;
        }

        sensor.viewRadius = ai.incapacitatedDetectionRadius;

        string lieClip = ai.crouchAnim;
        if (!ai.isStartingIncapacitated)
        {
            if (ai.lastHitWasHard)
            {
                bool fromFront = Vector3.Dot(ai.transform.forward, ai.lastHitDirection) < 0f;
                lieClip = fromFront ? ai.lieBackAnim : ai.lieFrontAnim;
            }
            else
            {
                lieClip = ai.lieFrontAnim;
            }
        }
        anim.PlayAnimation(lieClip);
    }

    public void Update() { }

    public void Exit() { }
}