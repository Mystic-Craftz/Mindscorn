using System.Collections;
using UnityEngine;
using UnityEngine.AI;

static class WanderUtils
{
    public static readonly int areaMask = NavMesh.AllAreas;
}

public class WanderState : IState
{
    private MonsterAI monster;
    private NavMeshAgent agent;
    private AIAnimationController animator;

    private bool isWaiting = false;
    private Coroutine hissCoroutine;

    public WanderState(MonsterAI monsterAI)
    {
        monster = monsterAI;
        agent = monsterAI.agent;
        animator = monsterAI.aiAnimator;
    }

    public void Enter()
    {
        agent.stoppingDistance = 0f;
        agent.isStopped = false;
        animator.ForceUnlock();
        animator.PlayAnimation(monster.locomotionAnim);

        //  Start Wander Loop 
        monster.PlayWanderLoop();

        PickNewDestination();
    }

    public void Update()
    {
        animator.SetMoveSpeed(agent.velocity.magnitude);

        // keep loop position updated
        monster.UpdateStateLoopPosition(typeof(WanderState));

        if (isWaiting)
            return;

        if (!agent.pathPending && agent.remainingDistance <= 0.1f)
        {
            hissCoroutine = monster.StartCoroutine(HissThenWait());
        }
    }

    public void Exit()
    {
        if (hissCoroutine != null)
        {
            monster.StopCoroutine(hissCoroutine);
            hissCoroutine = null;
        }

        isWaiting = false;
        agent.isStopped = false;

        //Stop Wander Loop
        monster.StopWanderLoop();
    }

    private IEnumerator HissThenWait()
    {
        isWaiting = true;
        agent.isStopped = true;

        yield return animator.PlayAndWait(monster.hissAnim);

        if (monster.stateMachine.CurrentState != this)
            yield break;

        animator.PlayAnimation(monster.locomotionAnim);
        animator.SetMoveSpeed(0f);

        if (monster.stateMachine.CurrentState != this)
            yield break;

        float waitTime = Random.Range(monster.wanderIntervalMin, monster.wanderIntervalMax);
        yield return new WaitForSeconds(waitTime);

        if (monster.stateMachine.CurrentState != this)
            yield break;

        PickNewDestination();
        agent.isStopped = false;
        isWaiting = false;
        hissCoroutine = null;
    }

    private void PickNewDestination()
    {
        Vector3 center = monster.wanderCenter != null
            ? monster.wanderCenter.position
            : monster.transform.position;

        Vector3 randomPos = center + Random.insideUnitSphere * monster.wanderRadius;
        randomPos.y = center.y;

        if (NavMesh.SamplePosition(randomPos, out NavMeshHit hit, monster.wanderRadius, WanderUtils.areaMask))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            agent.SetDestination(center);
        }
    }
}
