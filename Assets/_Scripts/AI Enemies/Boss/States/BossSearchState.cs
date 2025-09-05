using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class BossSearchState : IState
{
    private BossAI boss;
    private Coroutine searchRoutine;
    private const float moveThresholdSqr = 0.01f;

    public BossSearchState(BossAI boss)
    {
        this.boss = boss;
    }

    public void Enter()
    {
        if (boss.agent != null && boss.agent.isOnNavMesh)
        {
            boss.agent.isStopped = false;
            boss.agent.speed = Mathf.Min(boss.wanderSpeed * 1.1f, boss.chaseSpeed);
            boss.agent.SetDestination(boss.lastKnownPlayerPosition);
        }

        searchRoutine = boss.StartCoroutine(SearchRoutine());
    }

    public void Update()
    {
        // if player is seen, switch to chase
        if (boss.sensor != null && boss.sensor.PlayerInSight && boss.player != null)
        {
            boss.anim?.SetMoveSpeed(1f);
            boss.stateMachine.ChangeState(boss.chaseState);
        }
        else
        {
            // move to last known position
            if (boss.agent != null)
            {
                bool moving = boss.agent.isOnNavMesh && boss.agent.velocity.sqrMagnitude > moveThresholdSqr;
                boss.anim?.SetMoveSpeed(moving ? 1f : 0f);
            }
        }
    }

    public void Exit()
    {
        // stop coroutine 
        if (searchRoutine != null)
        {
            boss.StopCoroutine(searchRoutine);
            searchRoutine = null;
        }

        // stop movement and animation
        if (boss.agent != null && boss.agent.isOnNavMesh)
        {
            boss.agent.ResetPath();
            boss.agent.isStopped = true;
        }

        boss.anim?.SetMoveSpeed(0f);
    }

    private IEnumerator SearchRoutine()
    {
        float timer = boss.searchDuration;

        //  Move to last known position
        if (boss.agent != null && boss.agent.isOnNavMesh)
        {
            boss.agent.SetDestination(boss.lastKnownPlayerPosition);
        }

        // Wait until we reach last-known position (or until player is found)
        while (true)
        {
            if (boss.sensor != null && boss.sensor.PlayerInSight && boss.player != null)
            {
                boss.stateMachine.ChangeState(boss.chaseState);
                yield break;
            }

            if (boss.agent == null || !boss.agent.isOnNavMesh) break;

            bool reached = !boss.agent.pathPending && boss.agent.remainingDistance <= Mathf.Max(0.2f, boss.agent.stoppingDistance);
            if (reached) break;

            bool moving = boss.agent.velocity.sqrMagnitude > 0.01f;
            boss.anim?.SetMoveSpeed(moving ? 1f : 0f);

            yield return null;
        }

        //Wait for investigationPause
        if (boss.agent != null && boss.agent.isOnNavMesh)
        {
            boss.agent.isStopped = true;
        }
        boss.anim?.SetMoveSpeed(0f);

        float pauseTimer = boss.investigationPause;
        while (pauseTimer > 0f)
        {
            if (boss.sensor != null && boss.sensor.PlayerInSight && boss.player != null)
            {
                boss.stateMachine.ChangeState(boss.chaseState);
                yield break;
            }
            pauseTimer -= Time.deltaTime;
            yield return null;
        }

        // Move to random position within search radius until player is found
        while (timer > 0f)
        {
            if (boss.sensor != null && boss.sensor.PlayerInSight && boss.player != null)
            {
                boss.stateMachine.ChangeState(boss.chaseState);
                yield break;
            }

            Vector3 randomOffset = Random.insideUnitSphere * boss.searchRadius;
            randomOffset.y = 0f;
            Vector3 candidate = boss.lastKnownPlayerPosition + randomOffset;

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
            {
                if (boss.agent != null && boss.agent.isOnNavMesh)
                {
                    boss.agent.isStopped = false;
                    boss.agent.SetDestination(hit.position);
                }

                while (true)
                {
                    if (boss.sensor != null && boss.sensor.PlayerInSight && boss.player != null)
                    {
                        boss.stateMachine.ChangeState(boss.chaseState);
                        yield break;
                    }

                    if (boss.agent == null || !boss.agent.isOnNavMesh) break;

                    bool reached = !boss.agent.pathPending && boss.agent.remainingDistance <= Mathf.Max(0.2f, boss.agent.stoppingDistance);

                    bool moving = boss.agent.velocity.sqrMagnitude > 0.01f;
                    boss.anim?.SetMoveSpeed(moving ? 1f : 0f);

                    if (reached) break;
                    timer -= Time.deltaTime;
                    if (timer <= 0f) break;
                    yield return null;
                }

                if (boss.agent != null && boss.agent.isOnNavMesh)
                {
                    boss.agent.isStopped = true;
                    boss.anim?.SetMoveSpeed(0f);
                }

                float smallPause = 0.8f; // short look-around at each waypoint
                while (smallPause > 0f)
                {
                    if (boss.sensor != null && boss.sensor.PlayerInSight && boss.player != null)
                    {
                        boss.stateMachine.ChangeState(boss.chaseState);
                        yield break;
                    }
                    smallPause -= Time.deltaTime;
                    timer -= Time.deltaTime;
                    if (timer <= 0f) break;
                    yield return null;
                }
            }
            else
            {
                // no valid navmesh sample, skip and try another point
                timer -= 0.1f;
                yield return null;
            }
        }

        //  Return to wander
        boss.anim?.SetMoveSpeed(0f);
        boss.stateMachine.ChangeState(boss.wanderState);
    }
}
