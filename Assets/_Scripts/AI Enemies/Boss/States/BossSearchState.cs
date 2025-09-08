using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class BossSearchState : IState
{
    private BossAI boss;
    private Coroutine searchRoutine;
    private const float moveThresholdSqr = 0.01f;
    private float originalStoppingDistance = -1f;


    public BossSearchState(BossAI boss)
    {
        this.boss = boss;
    }

    public void Enter()
    {
        // Stop any previous routine just in case
        if (searchRoutine != null)
        {
            boss.StopCoroutine(searchRoutine);
            searchRoutine = null;
        }

        if (boss.agent == null || !boss.agent.isOnNavMesh)
        {
            Debug.Log("[BossSearchState] Agent missing or not on navmesh - switching to wander.");
            boss.stateMachine.ChangeState(boss.wanderState);
            return;
        }

        // store and reduce stopping distance so "reached" is strict
        originalStoppingDistance = boss.agent.stoppingDistance;
        boss.agent.stoppingDistance = Mathf.Min(0.25f, originalStoppingDistance);

        boss.agent.isStopped = false;
        boss.agent.updatePosition = true;
        boss.agent.updateRotation = true;
        boss.agent.speed = Mathf.Min(boss.wanderSpeed * 1.1f, boss.chaseSpeed);

        // try to snap the last known position to the navmesh
        Vector3 targetPos = boss.lastKnownPlayerPosition;
        if (NavMesh.SamplePosition(boss.lastKnownPlayerPosition, out NavMeshHit lastHit, 2.0f, NavMesh.AllAreas))
        {
            targetPos = lastHit.position;
        }

        boss.agent.ResetPath();
        boss.agent.SetDestination(targetPos);

        searchRoutine = boss.StartCoroutine(SearchRoutine());
    }

    public void Update()
    {
        // if player is seen, switch to chase
        if (boss.sensor != null && boss.sensor.PlayerInSight && boss.player != null)
        {
            boss.anim?.SetMoveSpeed(1f);
            boss.stateMachine.ChangeState(boss.chaseState);
            return;
        }

        // Update move animation from agent velocity
        if (boss.agent != null && boss.agent.isOnNavMesh)
        {
            bool moving = boss.agent.velocity.sqrMagnitude > moveThresholdSqr;
            boss.anim?.SetMoveSpeed(moving ? 1f : 0f);
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

        // restore stopping distance
        if (boss.agent != null && boss.agent.isOnNavMesh && originalStoppingDistance >= 0f)
        {
            boss.agent.stoppingDistance = originalStoppingDistance;
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

        // Wait until path resolves, or bail if no valid path
        if (boss.agent.pathPending)
        {
            float pathWait = 0.5f;
            while (boss.agent.pathPending && pathWait > 0f)
            {
                if (boss.sensor != null && boss.sensor.PlayerInSight && boss.player != null)
                {
                    boss.stateMachine.ChangeState(boss.chaseState);
                    yield break;
                }
                pathWait -= Time.deltaTime;
                yield return null;
            }
        }

        if (!boss.agent.hasPath && boss.agent.remainingDistance <= boss.agent.stoppingDistance)
        {
            Debug.Log("[BossSearchState] No path or already at last-known.");
        }
        else
        {
            // Wait until we reach last-known position or see player
            while (true)
            {
                if (boss.sensor != null && boss.sensor.PlayerInSight && boss.player != null)
                {
                    boss.stateMachine.ChangeState(boss.chaseState);
                    yield break;
                }

                if (boss.agent == null || !boss.agent.isOnNavMesh) break;

                bool reached = !boss.agent.pathPending
                               && boss.agent.remainingDistance <= Mathf.Max(0.2f, boss.agent.stoppingDistance);

                bool moving = boss.agent.velocity.sqrMagnitude > 0.01f;
                boss.anim?.SetMoveSpeed(moving ? 1f : 0f);

                if (reached) break;
                yield return null;
            }
        }

        // Wait for investigation pause period
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

        // Start Searching
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


            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
            {
                if (boss.agent != null && boss.agent.isOnNavMesh)
                {
                    boss.agent.isStopped = false;
                    boss.agent.ResetPath();
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


                float smallPause = boss.investigationPause;
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

                timer -= 0.1f;
                yield return null;
            }
        }


        boss.anim?.SetMoveSpeed(0f);
        boss.stateMachine.ChangeState(boss.wanderState);
    }
}
