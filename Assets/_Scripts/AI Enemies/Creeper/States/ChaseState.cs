using System;
using FMOD;
using UnityEngine;
using UnityEngine.AI;

public class ChaseState : IState
{
    private readonly MonsterAI monsterAI;
    private readonly Transform player;
    private readonly NavMeshAgent agent;
    private float originalSpeed;
    private bool lastSurgeState;

    public Type StateType => typeof(ChaseState);

    public ChaseState(MonsterAI ai)
    {
        monsterAI = ai;
        player = ai.playerTransform;
        agent = ai.agent;
    }

    public void Enter()
    {
        // Agent setup
        originalSpeed = agent.speed;
        agent.stoppingDistance = 1.5f;
        if (agent.isOnNavMesh) agent.isStopped = false;

        // Decide surge and apply speed if surged
        monsterAI.isPowerSurging = UnityEngine.Random.value < monsterAI.powerSurgeChance;
        if (monsterAI.isPowerSurging)
        {
            monsterAI.surgeNormalHitsCount = 0;
            agent.speed = monsterAI.powerSurgeSpeed;
        }

        // Anim
        monsterAI.aiAnimator.ForceUnlock();
        monsterAI.aiAnimator.PlayAnimation(monsterAI.locomotionAnim, 0.1f);
        monsterAI.aiAnimator.SetMoveSpeed(agent.speed);


        if (monsterAI.isPowerSurging)
        {
            // surge => chase loop
            monsterAI.StopWanderLoop();
            monsterAI.PlayChaseLoop();
        }
        else
        {
            // normal chase => wander loop
            monsterAI.StopChaseLoop();
            monsterAI.PlayWanderLoop();
        }

        lastSurgeState = monsterAI.isPowerSurging;
    }

    public void Update()
    {
        if (player == null) return;

        // keep the correct loop following the monster
        if (lastSurgeState)
            monsterAI.UpdateStateLoopPosition(typeof(ChaseState));
        else
            monsterAI.UpdateStateLoopPosition(typeof(WanderState));

        // if surge flips mid-chase, swap loops so only one plays
        if (monsterAI.isPowerSurging != lastSurgeState)
        {
            if (monsterAI.isPowerSurging)
            {
                monsterAI.StopWanderLoop();
                monsterAI.PlayChaseLoop();
            }
            else
            {
                monsterAI.StopChaseLoop();
                monsterAI.PlayWanderLoop();
            }
            lastSurgeState = monsterAI.isPowerSurging;
        }

        // movement / transition
        float distance = Vector3.Distance(monsterAI.transform.position, player.position);
        if (distance > agent.stoppingDistance)
        {
            if (agent.isOnNavMesh) agent.SetDestination(player.position);
            monsterAI.aiAnimator.SetMoveSpeed(agent.speed);
        }
        else
        {
            if (agent.isOnNavMesh) agent.isStopped = true;
            monsterAI.aiAnimator.SetMoveSpeed(0f);

            if (monsterAI.isPowerSurging)
                monsterAI.immediateAttack = true;

            monsterAI.stateMachine.ChangeState(monsterAI.attackState);
        }
    }

    public void Exit()
    {
        // restore movement & flags
        agent.speed = originalSpeed;
        monsterAI.isPowerSurging = false;
        monsterAI.aiAnimator.SetMoveSpeed(0f);


        monsterAI.StopChaseLoop();
        monsterAI.StopWanderLoop();
    }
}
