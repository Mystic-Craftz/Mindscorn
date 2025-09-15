using System;
using UnityEngine;
using UnityEngine.AI;

// Summary: Patrol-like wander — move then pause, close toward player (no teleport), set anim speed 0 while stopped.
public class BossWanderState : IState
{
    private BossAI boss;
    private Vector3 wanderTarget;
    private const float arriveThreshold = 0.6f;

    private bool isMoving = true;
    private float moveTimer = 0f;
    private float stopTimer = 0f;
    private float currentMoveDuration = 0f;
    private float currentStopDuration = 0f;

    private float stuckTimer = 0f;
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
            boss.agent.autoBraking = false;
        }

        moveTimer = stopTimer = 0f;
        stuckTimer = 0f;
        isMoving = true;
        PickNewMoveStopDurations();
        PickNewWanderTargetImmediate();

        boss.anim?.SetMoveSpeed(boss.wanderSpeed);
    }

    public void Update()
    {
        if (boss == null || boss.agent == null || !boss.agent.isOnNavMesh)
            return;

        Transform playerT = boss.player;
        if (playerT == null)
        {
            var found = GameObject.FindGameObjectWithTag("Player");
            if (found) playerT = found.transform;
        }

        if (isMoving)
        {
            moveTimer += Time.deltaTime;

            bool arrived = !boss.agent.pathPending && boss.agent.remainingDistance <= arriveThreshold;
            bool noPath = boss.agent.pathStatus == NavMeshPathStatus.PathInvalid
                          || boss.agent.path == null
                          || boss.agent.path.corners.Length == 0;

            if (noPath || arrived)
            {
                if (moveTimer >= Mathf.Max(0.25f, boss.wanderRepositionInterval))
                {
                    PickNewWanderTarget(false, playerT);
                    moveTimer = 0f;
                }
            }

            if (moveTimer >= currentMoveDuration)
            {
                EnterStop();
                return;
            }

            if (boss.agent.velocity.sqrMagnitude < 0.05f && !arrived)
            {
                stuckTimer += Time.deltaTime;
                if (stuckTimer > stuckResetTime)
                {
                    PickNewWanderTarget(false, playerT);
                    stuckTimer = 0f;
                }
            }
            else
            {
                stuckTimer = 0f;
            }

            boss.anim?.SetMoveSpeed(boss.wanderSpeed);
        }
        else
        {
            stopTimer += Time.deltaTime;
            boss.anim?.SetMoveSpeed(0f);

            if (stopTimer >= currentStopDuration)
            {
                ExitStopAndMove(playerT);
                return;
            }
        }
    }

    public void Exit()
    {
        if (boss?.agent != null)
        {
            boss.agent.ResetPath();
            boss.agent.isStopped = false;
            boss.agent.autoBraking = true;
        }
        boss.anim?.SetMoveSpeed(0f);
    }

    //  switch to stopped mode 
    private void EnterStop()
    {
        isMoving = false;
        stopTimer = 0f;
        boss.agent.isStopped = true;
        currentStopDuration = UnityEngine.Random.Range(boss.wanderStopDurationMin, boss.wanderStopDurationMax);
        boss.anim?.SetMoveSpeed(0f);
    }

    // switch to moving mode
    private void ExitStopAndMove(Transform playerT)
    {
        isMoving = true;
        moveTimer = 0f;
        boss.agent.isStopped = false;
        currentMoveDuration = UnityEngine.Random.Range(boss.wanderMoveDurationMin, boss.wanderMoveDurationMax);
        PickNewWanderTarget(false, playerT);
        boss.anim?.SetMoveSpeed(boss.wanderSpeed);
    }

    // Pick random durations for the next move and stop phases.
    private void PickNewMoveStopDurations()
    {
        currentMoveDuration = UnityEngine.Random.Range(boss.wanderMoveDurationMin, boss.wanderMoveDurationMax);
        currentStopDuration = UnityEngine.Random.Range(boss.wanderStopDurationMin, boss.wanderStopDurationMax);
    }

    // Immediately choose the first wander target.
    private void PickNewWanderTargetImmediate()
    {
        PickNewWanderTarget(true, boss.player);
    }

    // Choose a new wander target, if player is far pick a point near the player to close distance.
    private void PickNewWanderTarget(bool immediate, Transform playerT)
    {
        Vector3 center;

        if (playerT != null)
        {
            float distToPlayer = Vector3.Distance(boss.transform.position, playerT.position);
            if (distToPlayer > boss.playerFarDistance)
            {
                float closeDist = UnityEngine.Random.Range(boss.stalkingCloseMin, boss.stalkingCloseMax);
                float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Vector3 candidate = playerT.position + dir * closeDist + UnityEngine.Random.insideUnitSphere * 0.5f;
                candidate.y = playerT.position.y;

                NavMeshHit hit;
                if (NavMesh.SamplePosition(candidate, out hit, 2.0f, NavMesh.AllAreas))
                {
                    wanderTarget = hit.position;
                    SetAgentDestination(wanderTarget);
                    return;
                }

                center = playerT.position;
            }
            else
            {
                center = playerT.position;
            }
        }
        else if (boss.lastKnownPlayerPosition != Vector3.zero)
        {
            center = boss.lastKnownPlayerPosition;
        }
        else
        {
            center = boss.transform.position;
        }

        int attempts = immediate ? 12 : 6;
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

        NavMeshHit fallback;
        if (NavMesh.SamplePosition(boss.transform.position, out fallback, 2.0f, NavMesh.AllAreas))
        {
            wanderTarget = fallback.position;
            SetAgentDestination(wanderTarget);
        }
    }

    //  produce a random point around center, biased to cut the player's path sometimes.
    private Vector3 SampleCandidate(Vector3 center)
    {
        float dist = UnityEngine.Random.Range(boss.wanderMinDistance, boss.wanderMaxDistance);

        if (boss.player != null && UnityEngine.Random.value < boss.obstructBias)
        {
            Vector3 forward = boss.player.forward;
            Vector3 perp = Vector3.Cross(forward, Vector3.up).normalized;
            float side = UnityEngine.Random.Range(-1f, 1f);
            Vector3 candidate = boss.player.position
                                + forward * UnityEngine.Random.Range(-1f, dist * 0.8f)
                                + perp * side * UnityEngine.Random.Range(1f, dist * 0.6f);
            candidate += UnityEngine.Random.insideUnitSphere * 0.5f;
            candidate.y = center.y;
            return candidate;
        }
        else
        {
            float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            Vector3 candidate = center + dir * dist;
            candidate += UnityEngine.Random.insideUnitSphere * 0.5f;
            candidate.y = center.y;
            return candidate;
        }
    }

    // Set destination on the NavMeshAgent.
    private void SetAgentDestination(Vector3 dest)
    {
        if (boss.agent == null || !boss.agent.isOnNavMesh)
            return;

        boss.agent.speed = boss.wanderSpeed;
        boss.agent.SetDestination(dest);
    }
}
