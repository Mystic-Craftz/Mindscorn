using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;
using UnityEngine.AI;

public class Parasite : MonoBehaviour
{
    [SerializeField] private Animator parasiteAnimator;
    [SerializeField] private List<GameObject> bloodDecalPrefabs;

    [Header("Agent Settings")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private float speed = 1f;

    [Header("Detection / Combat")]
    [SerializeField] private float detectionRadius = 8f;
    [SerializeField] private float stopRadius = 10f;        // if player further than this, stop
    [SerializeField] private float attackRange = 1.5f;      // distance to start attack
    [SerializeField] private float jumpHeight = 1.2f;       // for attack arc
    [SerializeField] private float stoppedToRoamTimerMax = 2f;       // for going to roam state
    [SerializeField] private float jumpDuration = 0.6f;     // seconds for jump attack
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private float attackCooldown = 1.2f;
    [SerializeField] private LayerMask obstacleLayers;

    [Header("Dying Settings")]
    [SerializeField] private float dyingBackwardSpeed = 1f;

    [Header("Sounds")]
    [SerializeField] private EventReference speakSound;
    [SerializeField] private EventReference jumpSound;
    [SerializeField] private EventReference footstepSound;

    private const string GET_OUT = "GetOut";
    private const string JUMP = "Jump";
    private const string MOVE_MP = "MoveMP";
    private const string IS_GROUNDED = "IsGrounded";
    private const string MOVE_DEATH = "MoveDeath";
    private const string MID_AIR_DEATH = "MidAirDeath";
    private const string ATTACK_TRIGGER = "Attack"; // make sure this matches your animator trigger name

    [Header("Debug")]
    public string state = "";

    public enum ParasiteState
    {
        Inside,
        GettingOut,
        Moving,
        Roaming,
        Stopped,
        Attacking,
        Dying,
        Dead
    }

    private ParasiteState currentState = ParasiteState.Inside;

    private bool canMove = false;
    private bool isGrounded = false;
    private bool disableGroundCheck = false;
    private bool dyingWasMidAir = false;

    private bool canSpeakAgain = true;
    private bool canPlayJumpSoundAgain = true;
    private float lastAttackTime = -999f;
    private float stoppedToRoamTimer = 0f;

    private float bloodDecalSpawnDelay = 0.2f; // delay after landing to spawn blood decal

    private Transform player;

    private Vector3 roamPoint = Vector3.zero;
    private Coroutine traversingLinkCoroutine;

    private void Start()
    {
        player = PlayerController.Instance.transform; // keep using your singleton
        agent.speed = speed;
        agent.updateRotation = true;
        agent.autoBraking = true;
        agent.autoTraverseOffMeshLink = false; // we handle off-mesh links manually
    }

    private void Update()
    {
        switch (currentState)
        {
            case ParasiteState.Inside:
                agent.enabled = false;
                // do nothing
                break;

            case ParasiteState.GettingOut:
                // small forward movement handled by animation events (CanMoveNow)
                agent.enabled = true;
                if (canMove) { Vector3 forwardMove = transform.forward * .3f * Time.deltaTime; agent.Move(forwardMove); }
                break;

            case ParasiteState.Moving:
                HandleMovingState();
                break;

            case ParasiteState.Roaming:
                HandleRoamingState();
                break;

            case ParasiteState.Stopped:
                if (stoppedToRoamTimer >= stoppedToRoamTimerMax)
                {
                    currentState = ParasiteState.Roaming;
                    stoppedToRoamTimer = 0f;
                    currentState = ParasiteState.Roaming;
                }
                else
                {
                    stoppedToRoamTimer += Time.deltaTime;
                }
                // idle on table (or stop animation). Detect re-entry into radius to resume moving
                if (IsPlayerWithin(detectionRadius) || Time.time - lastAttackTime < attackCooldown)
                {
                    // resume movement
                    currentState = ParasiteState.Moving;
                    agent.isStopped = false;
                    parasiteAnimator.SetFloat(MOVE_MP, Mathf.Clamp(agent.velocity.magnitude / Mathf.Max(agent.speed, 0.0001f), 0f, 10f) * 3);
                    RotateTowards(player.position);
                }
                break;

            case ParasiteState.Attacking:
                // attack coroutine handles movement, nothing to do here every frame
                break;

            case ParasiteState.Dying:
                // if mid-air dying, move backwards a bit so body looks pushed back
                if (dyingWasMidAir)
                {
                    // Use NavMeshAgent.Move to nudge the agent
                    Vector3 backward = -transform.forward * dyingBackwardSpeed * Time.deltaTime;
                    agent.Move(backward);
                }
                break;

            case ParasiteState.Dead:
                // do nothing
                break;
        }

        GroundedCheck();
        state = currentState.ToString();
    }

    #region State Helpers

    private void HandleMovingState()
    {
        // Handle OffMeshLink traversal
        if (agent.isOnOffMeshLink)
        {
            if (traversingLinkCoroutine == null)
                StartCoroutine(TraverseOffMeshLink());
            return;
        }
        // If player is too far, stop and switch to Stopped
        if (!IsPlayerWithin(stopRadius) || Time.time - lastAttackTime < attackCooldown)
        {
            // stop moving
            agent.isStopped = true;
            parasiteAnimator.SetFloat(MOVE_MP, 0f);
            currentState = ParasiteState.Stopped;
            return;
        }

        // If player within detection radius, set destination
        if (IsPlayerWithin(detectionRadius))
        {
            Vector3 targetPos = player.position;
            agent.SetDestination(targetPos);
            // animate with a multiplier proportional to agent velocity & current set speed
            parasiteAnimator.SetFloat(MOVE_MP, Mathf.Clamp(agent.velocity.magnitude / Mathf.Max(agent.speed, 0.0001f), 0f, 10f) * 3);



            // If within attack range, start attack
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            if (distanceToPlayer <= attackRange && Time.time - lastAttackTime >= attackCooldown)
            {
                StartCoroutine(PerformAttack());
            }
            else
            {
                if (bloodDecalSpawnDelay <= 0f)
                {
                    SpawnBloodDecal();
                    bloodDecalSpawnDelay = 0.2f;
                }
                else
                {
                    bloodDecalSpawnDelay -= Time.deltaTime;
                }
            }
            RotateTowards(player.position);

        }
        else
        {
            // player left detection, stop
            agent.isStopped = true;
            parasiteAnimator.SetFloat(MOVE_MP, 0f);
            currentState = ParasiteState.Stopped;
        }
    }

    private void HandleRoamingState()
    {
        if (roamPoint == Vector3.zero)
        {
            FindRandomPoint();
            agent.isStopped = true;
            parasiteAnimator.SetFloat(MOVE_MP, 0f);
            agent.speed = 0;
        }
        else
        {
            agent.SetDestination(roamPoint);
            parasiteAnimator.SetFloat(MOVE_MP, Mathf.Clamp(agent.velocity.magnitude / Mathf.Max(agent.speed, 0.0001f), 0f, 10f) * 3);
            agent.isStopped = false;
            agent.speed = speed;

            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                roamPoint = Vector3.zero;
                currentState = ParasiteState.Stopped;
            }
        }

        if (IsPlayerWithin(detectionRadius) || Time.time - lastAttackTime < attackCooldown)
        {
            // resume movement
            currentState = ParasiteState.Moving;
            agent.isStopped = false;
            parasiteAnimator.SetFloat(MOVE_MP, Mathf.Clamp(agent.velocity.magnitude / Mathf.Max(agent.speed, 0.0001f), 0f, 10f) * 3);
            RotateTowards(player.position);
        }
    }

    private void FindRandomPoint()
    {
        Vector3 randomDirection = Random.insideUnitSphere * detectionRadius;
        randomDirection += transform.position;
        NavMeshHit hit;
        Vector3 finalPosition = Vector3.zero;
        if (NavMesh.SamplePosition(randomDirection, out hit, detectionRadius, 1))
        {
            NavMeshPath path = new NavMeshPath();
            agent.CalculatePath(hit.position, path);

            switch (path.status)
            {
                case NavMeshPathStatus.PathComplete:
                    finalPosition = hit.position;
                    break;
                default:
                    roamPoint = Vector3.zero;
                    break;
            }
        }
        roamPoint = finalPosition;
    }

    private void SpawnBloodDecal()
    {
        if (bloodDecalPrefabs == null || bloodDecalPrefabs.Count == 0) return;

        int index = Random.Range(0, bloodDecalPrefabs.Count);
        GameObject decalPrefab = bloodDecalPrefabs[index];

        RaycastHit hit;

        Quaternion rotation = Quaternion.Euler(90f, 0f, transform.eulerAngles.y);
        GameObject bloodDecal = Instantiate(decalPrefab, transform.position, rotation);

        Destroy(bloodDecal, 10f);

    }

    private void RotateTowards(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0; // keep only horizontal rotation
        if (direction.magnitude < 0.1f) return; // avoid zero direction

        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
    }

    private bool IsPlayerWithin(float radius)
    {
        if (player == null) return false;
        float sqr = (player.position - transform.position).sqrMagnitude;
        if (sqr <= radius * radius)
        {
            // Check for obstacles between parasite and player
            Vector3 direction = (player.position - transform.position).normalized;
            float distance = Mathf.Sqrt(sqr);
            if (!Physics.Raycast(transform.position + Vector3.up, direction, distance, obstacleLayers))
            {
                return true;
            }
        }
        return false;
    }

    #endregion

    #region Ground / Animation events

    private void GroundedCheck()
    {
        if (disableGroundCheck) return;
        RaycastHit hit;
        // small offset down so we don't detect inside ground
        float checkDistance = 0.25f;
        isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out hit, checkDistance + 0.1f);
        parasiteAnimator.SetBool(IS_GROUNDED, isGrounded);
    }

    public void CanMoveNow()
    {
        canMove = true;
    }

    // Called from animation event when finish getting out movement/animation
    public void FinishGettingOut()
    {
        currentState = ParasiteState.Moving;
        parasiteAnimator.SetBool(IS_GROUNDED, true);
        parasiteAnimator.SetFloat(MOVE_MP, speed);
        agent.speed = speed;
        agent.isStopped = false;
    }

    public void GetOut()
    {
        currentState = ParasiteState.GettingOut;
        parasiteAnimator.SetTrigger(GET_OUT);
    }

    #endregion

    #region OffMeshLink traversal (jump down from table)

    private IEnumerator TraverseOffMeshLink()
    {
        // Called when agent hits off-mesh link while in GettingOut state
        disableGroundCheck = true; // disable ground checks during jump
        isGrounded = false;
        OffMeshLinkData linkData = agent.currentOffMeshLinkData;
        Vector3 startPos = agent.transform.position;
        Vector3 endPos = linkData.endPos; // small offset if needed

        float duration = 0.6f;
        float t = 0f;

        // Play jump animation
        parasiteAnimator.SetBool(JUMP, true);
        agent.isStopped = true;

        while (t < duration)
        {
            if (currentState == ParasiteState.Dying) yield break; // abort if dying mid-air
            t += Time.deltaTime;
            float normalized = t / duration;
            // simple parabolic arc
            float height = Mathf.Sin(normalized * Mathf.PI) * jumpHeight - .5f;

            height = Mathf.Clamp(height, 0f, jumpHeight); // don't go below ground
            agent.transform.position = Vector3.Lerp(startPos, endPos, normalized) + Vector3.up * height;
            yield return null;
        }

        parasiteAnimator.SetBool(JUMP, false);
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer <= attackRange)
        {
            lastAttackTime = Time.time;
        }
        // finish
        agent.CompleteOffMeshLink();
        agent.Warp(transform.position);
        agent.isStopped = false;
        agent.autoTraverseOffMeshLink = true;
        // continue moving (state already set by FinishGettingOut or Move)
        traversingLinkCoroutine = null;
        disableGroundCheck = false;
        yield break;
    }

    #endregion

    #region Attack

    private IEnumerator PerformAttack()
    {
        disableGroundCheck = true; // disable ground checks during jump
        isGrounded = false;
        currentState = ParasiteState.Attacking;
        agent.isStopped = true;

        Vector3 start = transform.position;
        Vector3 targetPos = player != null ? player.position : start;
        // aim slightly at player's center
        targetPos.y = targetPos.y + 0.5f;
        // Offset target position to be slightly behind the player
        Vector3 playerForward = player != null ? player.forward : Vector3.forward;
        float behindDistance = 0.5f; // adjust as needed
        targetPos -= playerForward * behindDistance;

        // Trigger attack animation (assumes animation contains a "jump/attack" animation that visually leaps)
        parasiteAnimator.SetBool(JUMP, true);

        float t = 0f;
        float duration = jumpDuration;
        while (t < duration)
        {
            if (currentState == ParasiteState.Dying) yield break; // abort if dying mid-air
            t += Time.deltaTime;
            float normalized = t / duration;
            // parabolic movement toward target
            Vector3 horizontal = Vector3.Lerp(start, targetPos, normalized);
            float height = Mathf.Sin(normalized * Mathf.PI) * jumpHeight;
            height = Mathf.Clamp(height, 0f, jumpHeight); // don't go below ground
            transform.position = new Vector3(horizontal.x, horizontal.y + height, horizontal.z);
            yield return null;
        }
        parasiteAnimator.SetBool(JUMP, false);

        // after landing, check collision manually with player
        TryDealAttackDamage();

        lastAttackTime = Time.time;

        // small cooldown / resume moving
        agent.Warp(transform.position); // keep navmesh agent in sync with transform
        agent.isStopped = false;
        currentState = ParasiteState.Moving;
        disableGroundCheck = false;
        yield break;
    }

    private void TryDealAttackDamage()
    {
        // check if player is within attackRange (or use colliders in your attack animation)
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= 0.95f)
        {
            var p = PlayerController.Instance;
            if (p != null)
            {
                PlayerHealth.Instance.TakeDamage(attackDamage);
            }
        }
    }

    #endregion

    #region Dying / Dead

    // Call this method from your damage system when parasite should die.
    // If it gets killed mid-air, set wasMidAir = true.
    public void Damage()
    {
        if (currentState == ParasiteState.Dead || currentState == ParasiteState.Dying) return;

        currentState = ParasiteState.Dying;
        dyingWasMidAir = isGrounded ? false : true;
        agent.isStopped = true;
        disableGroundCheck = true; // stop ground checks

        if (dyingWasMidAir)
        {
            parasiteAnimator.Play(MID_AIR_DEATH);
        }
        else
        {
            parasiteAnimator.Play(MOVE_DEATH);
        }

        // Optionally disable colliders here so player can't further interact
        // Collider[] cols = GetComponentsInChildren<Collider>();
        // foreach (var c in cols) c.enabled = false;
    }

    // Animation event at end of death animation -> call this to finalize
    public void OnDeathFinished()
    {
        currentState = ParasiteState.Dead;
        // disable agent and optionally make rigidbody reactable
        if (agent != null) agent.enabled = false;
        // maybe destroy after some time:
        // Destroy(gameObject, 6f);
    }

    #endregion

    public void PlaySpeakSound()
    {
        if (canSpeakAgain)
        {
            AudioManager.Instance.PlayOneShot(speakSound, transform.position);
            canSpeakAgain = false;
            StartCoroutine(ResetCanSpeakAgainAfterDelay(.5f));
        }
    }

    private IEnumerator ResetCanSpeakAgainAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        canSpeakAgain = true;
    }

    public void PlayJumpSound()
    {
        if (canPlayJumpSoundAgain)
        {
            AudioManager.Instance.PlayOneShot(jumpSound, transform.position);
            canPlayJumpSoundAgain = false;
            StartCoroutine(ResetCanPlayJumpSoundAgainAfterDelay(.5f));
        }
    }

    private IEnumerator ResetCanPlayJumpSoundAgainAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        canPlayJumpSoundAgain = true;
    }

    public void PlayFootstepSound()
    {
        AudioManager.Instance.PlayOneShot(footstepSound, transform.position);
    }

    #region Utility / Debug

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

    #endregion
}
