using System;
using System.Collections;
using FMODUnity;
using UnityEngine;
using UnityEngine.AI;

public class Rat : MonoBehaviour
{
    [SerializeField] private float initialSpeed = 3f;
    [SerializeField] private float crawlingSpeed = 1f;
    [SerializeField] private float runningAwaySpeed = 1f;
    [SerializeField] private Animator animator;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private LayerMask doorLayerMask;
    [SerializeField] private float waitToChangeIdleTimerMax = 5f;
    [SerializeField] private float waitInRoamTimerMax = 2f;
    [SerializeField] private float goNearPlayerTimerMin = 2f;
    [SerializeField] private float goNearPlayerTimerMax = 5f;
    [SerializeField] private float viewRadius = 10f;
    [SerializeField] private float runDistance = 10f;
    [SerializeField] private float distanceFromPlayerToRunAwayWhenShooting = 10f;
    [SerializeField] private float distanceFromPlayerToRunAway = 10f;
    [SerializeField] private Vector3 doorDetectionCube;
    [SerializeField] private Vector3 doorDetectionCubeOffset;
    [SerializeField] private bool doesRandomlyGoToPlayer = false;
    [SerializeField] private EventReference ratNoises;

    private const string ROAM = "Roam";
    private const string DEATH = "Death";
    private const string CRAWL_UNDER_DOOR = "CrawlUnderDoor";
    private const string DEAD = "Dead";
    private const string IDLE_1 = "Idle1";
    private const string IDLE_2 = "Idle2";
    private const string DEFAULT_POSE = "default_pose";
    private bool canPlaySound = true;

    private enum AIState
    {
        Idle,
        Roam,
        Dead,
        RunningAway,
        GoingNearPlayer
    }

    private AIState currentState = AIState.Idle;

    private float waitToChangeIdleTimer = 0f;
    private float waitInRoamTimer = 0f;
    private float goNearPlayerTimer = 0f;
    private bool isPlayingFirstIdleAnimation = false;

    private Vector3 targetLocation = Vector3.zero;
    Transform playerTransform;

    private void Start()
    {
        PlayerWeapons.Instance.OnShoot += OnPlayerShoot;
        if (doesRandomlyGoToPlayer) goNearPlayerTimer = UnityEngine.Random.Range(goNearPlayerTimerMin, goNearPlayerTimerMax);
        playerTransform = PlayerController.Instance.transform;
    }

    private void FixedUpdate()
    {
        switch (currentState)
        {
            case AIState.Idle:
                Idle();
                break;
            case AIState.Roam:
                Roam();
                break;
            case AIState.Dead:
                Dead();
                break;
            case AIState.RunningAway:
                RunningAway();
                break;
            case AIState.GoingNearPlayer:
                GoingNearPlayer();
                break;
        }

        PlayerDistanceCheck();

        GoToPlayer();
    }

    private void Idle()
    {
        if (isPlayingFirstIdleAnimation)
        {
            animator.Play(IDLE_1);
        }
        else
        {
            animator.Play(IDLE_2);
        }

        agent.speed = 0f;


        if (waitToChangeIdleTimer >= waitToChangeIdleTimerMax)
        {
            waitToChangeIdleTimer = 0f;
            if (UnityEngine.Random.Range(0f, 1f) > 0.8f)
            {
                currentState = AIState.Roam;
            }
            if (UnityEngine.Random.Range(0f, 1f) > 0.2f)
            {
                isPlayingFirstIdleAnimation = !isPlayingFirstIdleAnimation;
            }
            PlaySound();
        }
        else
        {
            waitToChangeIdleTimer += Time.fixedDeltaTime;
        }
    }

    private void Roam()
    {
        if (targetLocation == Vector3.zero)
        {
            FindRandomPoint();
            PlaySound();
        }
        else
        {
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                agent.speed = 0f;
                animator.Play(UnityEngine.Random.Range(0, 1) > 0.5f ? IDLE_1 : IDLE_2);
                if (waitInRoamTimer >= waitInRoamTimerMax)
                {
                    waitInRoamTimer = 0f;
                    if (UnityEngine.Random.Range(0f, 1f) > 0.7f)
                    {
                        currentState = AIState.Idle;
                    }
                    PlaySound();
                    targetLocation = Vector3.zero;
                }
                else
                {
                    waitInRoamTimer += Time.fixedDeltaTime;
                }
            }
            else
            {
                agent.speed = initialSpeed;
                PlayRoamingAnimations();
                animator.SetFloat("runMP", 1f);
            }
        }
    }

    private void PlayRoamingAnimations()
    {
        Collider[] colliders = Physics.OverlapBox(transform.position + doorDetectionCubeOffset, doorDetectionCube, Quaternion.identity, doorLayerMask);

        if (colliders.Length > 0)
        {
            animator.Play(CRAWL_UNDER_DOOR);
            agent.speed = crawlingSpeed;
        }
        else
        {
            animator.Play(ROAM);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position + doorDetectionCubeOffset, doorDetectionCube);
    }

    private void FindRandomPoint()
    {
        Vector3 randomDirection = UnityEngine.Random.insideUnitSphere * viewRadius;
        randomDirection += transform.position;
        NavMeshHit hit;
        Vector3 finalPosition = Vector3.zero;
        if (NavMesh.SamplePosition(randomDirection, out hit, viewRadius, 1))
        {
            finalPosition = hit.position;
        }
        targetLocation = finalPosition;
        agent.SetDestination(targetLocation);
    }

    public void TakeDamage(float damage)
    {
        animator.CrossFade(DEATH, 0f);
        currentState = AIState.Dead;
        targetLocation = Vector3.zero;
        agent.speed = 0f;
        PlaySound();
    }

    private void OnPlayerShoot(object sender, EventArgs e)
    {
        if (currentState == AIState.Dead || currentState == AIState.RunningAway) return;
        if (Vector3.Distance(transform.position, playerTransform.position) < distanceFromPlayerToRunAwayWhenShooting)
        {
            currentState = AIState.RunningAway;
            Vector3 dirToAI = transform.position - playerTransform.position;
            Vector3 randomOffset = new Vector3(UnityEngine.Random.Range(-2f, 2f), 0, UnityEngine.Random.Range(-2f, 2f));
            Vector3 runTo = transform.position + (dirToAI.normalized * runDistance) + randomOffset;
            PlaySound();
            if (NavMesh.SamplePosition(runTo, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
        }
    }

    private void Dead() { }

    private void RunningAway()
    {
        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            currentState = AIState.Idle;
        }
        PlayRoamingAnimations();
        agent.speed = runningAwaySpeed;
        animator.SetFloat("runMP", 2f);
    }

    private void PlayerDistanceCheck()
    {
        if (currentState == AIState.Dead || currentState == AIState.RunningAway) return;
        if (Vector3.Distance(transform.position, playerTransform.position) < distanceFromPlayerToRunAway)
        {
            currentState = AIState.RunningAway;
            PlaySound();

            Vector3 bestPoint = transform.position;
            float bestDistance = Vector3.Distance(transform.position, playerTransform.position);

            int attempts = 12; // Number of directions to try

            for (int i = 0; i < attempts; i++)
            {
                // Spread directions evenly in a circle
                float angle = (360f / attempts) * i;
                Vector3 dir = Quaternion.Euler(0, angle, 0) * Vector3.forward;

                // Add a little randomness so it’s not always exactly the same
                dir += new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), 0, UnityEngine.Random.Range(-0.5f, 0.5f));

                Vector3 candidate = transform.position + dir.normalized * runDistance;

                // Check if this candidate point is valid on the NavMesh
                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                {
                    float distToPlayer = Vector3.Distance(hit.position, playerTransform.position);

                    // Choose the point farthest from the player
                    if (distToPlayer > bestDistance)
                    {
                        bestDistance = distToPlayer;
                        bestPoint = hit.position;
                    }
                }
            }

            // Move AI to the chosen best escape point
            agent.SetDestination(bestPoint);
            targetLocation = bestPoint;
        }
    }

    private void GoingNearPlayer()
    {
        agent.destination = playerTransform.position;
        agent.speed = runningAwaySpeed;
        animator.SetFloat("runMP", 2f);
        PlayRoamingAnimations();
        if (agent.remainingDistance <= agent.stoppingDistance + distanceFromPlayerToRunAway + 1)
        {
            currentState = AIState.Idle;
            PlaySound();
            goNearPlayerTimer = UnityEngine.Random.Range(goNearPlayerTimerMin, goNearPlayerTimerMax);
        }
    }

    private void GoToPlayer()
    {
        if (currentState == AIState.Dead || !doesRandomlyGoToPlayer) return;

        if (Vector3.Distance(transform.position, playerTransform.position) > distanceFromPlayerToRunAwayWhenShooting)
        {
            goNearPlayerTimer -= Time.fixedDeltaTime;
            if (goNearPlayerTimer <= 0f)
            {
                currentState = AIState.GoingNearPlayer;
            }
        }
    }

    private void PlaySound()
    {
        if (canPlaySound)
        {
            AudioManager.Instance.PlayOneShot(ratNoises, transform.position);
            canPlaySound = false;
            StartCoroutine(ResetCanPlaySoundAfterDelay(3f));
        }
    }

    private IEnumerator ResetCanPlaySoundAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        canPlaySound = true;
    }
}
