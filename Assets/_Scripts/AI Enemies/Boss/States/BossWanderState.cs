using System;
using UnityEngine;
using UnityEngine.AI;

public class BossWanderState : IState
{
    private BossAI boss;
    private Vector3 wanderTarget;
    private float pickTimer = 0f;
    private float stuckTimer = 0f;
    private const float arriveThreshold = 0.6f;
    private const float stuckResetTime = 3.0f;

    public BossWanderState(BossAI boss)
    {
        this.boss = boss;
    }

    public void Enter()
    {
        if (boss == null) return;

        if (boss.agent != null && boss.agent.isOnNavMesh)
        {
            boss.agent.isStopped = false;
            boss.agent.speed = boss.wanderSpeed;
            boss.agent.angularSpeed = 120f;
            boss.agent.acceleration = 8f;
            boss.agent.autoBraking = true;
        }

        pickTimer = 0f;
        stuckTimer = 0f;
        PickNewWanderTarget(true);
    }

    public void Update()
    {
        if (boss == null || boss.agent == null || !boss.agent.isOnNavMesh)
            return;

        // If player exists and is too far, maybe teleport closer
        Transform playerT = boss.player;
        if (playerT == null)
        {
            // fallback
            var found = GameObject.FindGameObjectWithTag("Player");
            if (found) playerT = found.transform;
        }

        if (playerT != null)
        {
            float dist = Vector3.Distance(boss.transform.position, playerT.position);
            if (dist > boss.teleportDistanceThreshold &&
                Time.time - boss.lastTeleportTime > boss.teleportCooldown &&
                UnityEngine.Random.value < boss.teleportChance)
            {
                // attempt teleport near player 
                bool teleported = boss.TryTeleportNear(playerT.position, boss.teleportRadius);
                if (teleported)
                {
                    // after teleport, pick a nearby wander target to resume stalking
                    PickNewWanderTarget(true);
                    return;
                }
            }
        }

        // If agent has no path or reached destination pick a new one
        bool arrived = !boss.agent.pathPending && boss.agent.remainingDistance <= arriveThreshold;
        bool noPath = boss.agent.pathStatus == NavMeshPathStatus.PathInvalid || boss.agent.path == null || boss.agent.path.corners.Length == 0;

        // If stuck or blocked, attempt to repick
        if (noPath || arrived)
        {
            pickTimer += Time.deltaTime;
            if (pickTimer >= boss.wanderRepositionInterval)
            {
                PickNewWanderTarget(false);
                pickTimer = 0f;
            }
        }
        else
        {
            // If not stuck, pick a new target every so often so that it is not predictable
            pickTimer += Time.deltaTime;
            if (pickTimer >= boss.wanderRepositionInterval)
            {
                PickNewWanderTarget(false);
                pickTimer = 0f;
            }
        }

        // Simple stuck detection: if agent velocity is near zero while it should be moving
        if (boss.agent.velocity.sqrMagnitude < 0.1f && !arrived)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer > stuckResetTime)
            {
                // repick to escape stuck
                PickNewWanderTarget(false);
                stuckTimer = 0f;
            }
        }
        else
        {
            stuckTimer = 0f;
        }
    }

    public void Exit()
    {

        if (boss?.agent != null)
        {
            boss.agent.ResetPath();
        }
    }

    private void PickNewWanderTarget(bool immediate)
    {
        Vector3 center;

        // Prefer the live player position if available, otherwise last known, otherwise boss position.
        if (boss.player != null)
        {
            center = boss.player.position;
        }
        else if (boss.lastKnownPlayerPosition != Vector3.zero)
        {
            center = boss.lastKnownPlayerPosition;
        }
        else
        {
            center = boss.transform.position;
        }

        // Try several times to find a valid navmesh point
        const int attempts = 8;
        for (int i = 0; i < attempts; i++)
        {
            Vector3 candidate = SampleCandidate(center);
            NavMeshHit hit;
            if (NavMesh.SamplePosition(candidate, out hit, 2.0f, NavMesh.AllAreas))
            {
                wanderTarget = hit.position;
                SetAgentDestination(wanderTarget);
                return;
            }
        }

        // fallback: sample around boss itself
        NavMeshHit fallback;
        if (NavMesh.SamplePosition(boss.transform.position, out fallback, 2.0f, NavMesh.AllAreas))
        {
            wanderTarget = fallback.position;
            SetAgentDestination(wanderTarget);
        }
    }

    private Vector3 SampleCandidate(Vector3 center)
    {
        // Random distance between configured min/max
        float dist = UnityEngine.Random.Range(boss.wanderMinDistance, boss.wanderMaxDistance);

        // If we have a player, bias the sample to obstruct player
        if (boss.player != null && UnityEngine.Random.value < boss.obstructBias)
        {
            // try to position in front of the player's forward (so boss tends to cut the player's path)
            Vector3 forward = boss.player.forward;
            // small forward bias plus perpendicular random offset
            Vector3 perp = Vector3.Cross(forward, Vector3.up).normalized;
            float side = UnityEngine.Random.Range(-1f, 1f);
            Vector3 candidate = boss.player.position + forward * UnityEngine.Random.Range(-1f, dist * 0.8f)
                                               + perp * side * UnityEngine.Random.Range(1f, dist * 0.6f);
            candidate += UnityEngine.Random.insideUnitSphere * 0.5f;
            candidate.y = center.y;
            return candidate;
        }
        else
        {
            // plain random direction around center
            float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            Vector3 candidate = center + dir * dist;
            candidate.y = center.y;
            return candidate;
        }
    }

    private void SetAgentDestination(Vector3 dest)
    {
        if (boss.agent == null || !boss.agent.isOnNavMesh)
            return;

        boss.agent.speed = boss.wanderSpeed;
        boss.agent.SetDestination(dest);
    }
}
