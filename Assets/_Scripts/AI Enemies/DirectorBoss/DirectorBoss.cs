using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using FMODUnity;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations.Rigging;

public class DirectorBoss : MonoBehaviour
{
    private const string IS_WALKING = "IsWalking";
    private const string IS_DASHING = "IsDashing";
    private const string IS_STUNNED = "IsStunned";
    private const string STUN = "Stun";
    private const string PUNCH_1 = "Punch1";
    private const string PUNCH_2 = "Punch2";
    private const string FAKE_DEATH = "Death";
    private const string REAL_DEATH = "RealDeath";
    private const string GETTING_UP = "GettingUp";
    private const string HEADSHOT_1 = "Headshot1";
    private const string HEADSHOT_2 = "Headshot2";
    private const string OPENING_DOOR = "OpeningDoor";
    private const string HOLDING_HEAD = "HoldingHead";
    private const string PUNCH_MP = "PunchMP";
    private const string MOVE_MP = "MoveMP";

    [Header("Main Components")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator animator;
    [SerializeField] private Rig headRig;
    [SerializeField] private List<GameObject> limb;
    [SerializeField] private List<GameObject> limbThrowable;
    [SerializeField] private List<GameObject> limbBloods;
    [SerializeField] private CinemachineImpulseSource impulseSource;

    [Header("Agent Settings")]
    [SerializeField] private float chaseSpeed = 3.5f;
    [SerializeField] private float normalWalkSpeed = 3.5f;
    [SerializeField] private float punchingMoveSpeed = 1.2f;
    [SerializeField] private float dashSpeed = 6f;

    [Header("Director Settings")]
    [SerializeField] private float maxHealth = 4000f;
    [SerializeField] private float distanceToPunchFrom = 1.5f;
    [SerializeField, Range(0, 1)] private float dashChance = 0.3f;
    [SerializeField] private float distanceFromPlayerToDash = 5f;
    [SerializeField] private float dashCapsuleRadius = 0.5f;
    [SerializeField] private float capsuleBottomOffset = 0.2f;
    [SerializeField] private float capsuleTopOffset = 1.6f;
    [SerializeField] private float attackDamage = 20f;
    [SerializeField] private float dashDamage = 20f;
    [SerializeField] private LayerMask dashLayerMask;

    [Header("Timers")]
    [SerializeField] private float idleTimerMax = 2f;
    private float idleTimer = 0f;
    [SerializeField] private float stunTimerMax = 2f;
    private float stunTimer = 0f;
    [SerializeField] private float dashInWalkingTimerMax = 2f;
    private float dashInWalkingTimer = 0f;
    [SerializeField] private float dashDuration = 0.6f;
    private float dashTimer = 0f;

    [Header("Audios")]
    [SerializeField] private EventReference breathing;
    [SerializeField] private EventReference angryBreathing;
    [SerializeField] private EventReference gettingHit;
    [SerializeField] private EventReference punching;
    [SerializeField] private EventReference gettingUp;
    [SerializeField] private EventReference fakeDeath;
    [SerializeField] private EventReference openingDoor;
    [SerializeField] private EventReference preparingToDash;
    [SerializeField] private EventReference realDeath;
    [SerializeField] private EventReference throwingLimb;
    [SerializeField] private EventReference bodyDropping;
    [SerializeField] private EventReference footsteps;
    [SerializeField] private EventReference goreSound;

    [Header("Debug")]
    public string currentRunningState;
    public string currentHealthString;
    [SerializeField] private bool debug = true;

    public enum DirectorState
    {
        Chasing,
        OpeningDoor,
        Idle,
        Moving,
        PreparingToDash,
        Dashing,
        Stunned,
        Punching,
        ThrowingLimb,
        FakeDeath,
        GettingUp,
        RealDeath
    }

    private DirectorState currentState;
    private Transform player;
    private Transform mainCam;
    private float currentHealth;
    private Vector3 directionToDashTowards;
    private bool isInvulnerable = false;
    private int limbIndex = 0;
    private bool canBeStunned = true;
    private bool canDash = true;
    private bool hasFakeDeathHappened = false;
    private bool isDead = false;
    private bool canBreath = true;
    private bool canGenerateImpulse = true;
    private bool didHitPlayer = false;

    private bool canThrowLimbNow = false;

    //* Unity methods
    private void Start()
    {
        player = PlayerController.Instance.transform;
        mainCam = Camera.main.transform;
        currentState = DirectorState.Idle;
        currentHealth = maxHealth;
    }

    private void Update()
    {
        if (!isDead)
        {
            if (currentHealth <= 0f && !isDead) SwitchToRealDeathState();
            switch (currentState)
            {
                case DirectorState.Chasing:
                    break;

                case DirectorState.Idle:
                    IdleState();
                    break;

                case DirectorState.OpeningDoor:
                    OpeningDoorState();
                    break;

                case DirectorState.Moving:
                    MovingState();
                    break;

                case DirectorState.PreparingToDash:
                    PreparingToDashState();
                    break;

                case DirectorState.Dashing:
                    DashingState();
                    break;

                case DirectorState.Stunned:
                    StunnedState();
                    break;

                case DirectorState.Punching:
                    PunchingState();
                    break;

                case DirectorState.ThrowingLimb:
                    ThrowingLimbState();
                    break;

                case DirectorState.FakeDeath:
                    FakeDeathState();
                    break;

                case DirectorState.GettingUp:
                    GettingUpState();
                    break;

                case DirectorState.RealDeath:
                    RealDeathState();
                    break;

                default:
                    break;
            }
        }

        // if (debug)
        // {
        //     float moveDistance = dashSpeed * Time.deltaTime;
        //     Vector3 capsulePoint1 = transform.position + Vector3.up * capsuleBottomOffset;
        //     Vector3 capsulePoint2 = transform.position + Vector3.up * capsuleTopOffset;
        //     RaycastHit hit;

        //     if (Physics.CapsuleCast(capsulePoint1, capsulePoint2, dashCapsuleRadius, transform.forward, out hit, moveDistance + 0.5f, dashLayerMask))
        //     {
        //         Collider c = hit.collider;
        //         Debug.Log("Hit: " + c.name);
        //     }
        // }

        if (debug)
        {
            currentRunningState = currentState.ToString();
            currentHealthString = currentHealth.ToString();
        }
    }

    //* States

    private void IdleState()
    {
        if (canThrowLimbNow)
        {
            SwitchToLimbThrowingState();
            canThrowLimbNow = false;
            return;
        }

        MakeVulnerable();
        animator.SetBool(IS_WALKING, false);
        animator.SetBool(IS_DASHING, false);
        animator.SetBool(IS_STUNNED, false);
        agent.isStopped = true;
        agent.speed = normalWalkSpeed;
        dashInWalkingTimer = 0f;

        if (idleTimer >= idleTimerMax)
        {
            idleTimer = 0f;
            if (didHitPlayer)
            {
                DOTween.To(() => animator.GetLayerWeight(2), x => animator.SetLayerWeight(2, x), 0f, .25f).SetEase(Ease.OutQuad);
            }
            SwitchToMovingState();
        }
        else
        {
            idleTimer += Time.deltaTime;
        }
    }

    private void OpeningDoorState() { }

    private void MovingState()
    {
        if (canThrowLimbNow)
        {
            SwitchToLimbThrowingState();
            canThrowLimbNow = false;
            return;
        }

        MakeVulnerable();
        agent.SetDestination(player.position);
        agent.isStopped = false;
        animator.SetBool(IS_WALKING, true);
        animator.SetBool(IS_STUNNED, false);
        agent.speed = normalWalkSpeed;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer <= distanceToPunchFrom)
        {
            SwitchToPunchingState();
        }

        if (dashInWalkingTimer >= dashInWalkingTimerMax)
        {
            dashInWalkingTimer = 0f;
            if (distanceToPlayer >= distanceFromPlayerToDash)
            {
                SwitchToPreparingToDashState();
            }
        }
        else
        {
            dashInWalkingTimer += Time.deltaTime;
        }
    }

    private void PreparingToDashState()
    {
        MakeVulnerable();
        agent.isStopped = true;
        agent.speed = 0f;
        animator.SetBool(IS_STUNNED, false);
        animator.SetBool(IS_WALKING, false);
        animator.SetBool(IS_DASHING, true);
        didHitPlayer = false;
    }

    private void DashingState()
    {
        MakeVulnerable();
        dashTimer += Time.deltaTime;
        animator.SetBool(IS_STUNNED, false);
        animator.SetBool(IS_WALKING, false);
        animator.SetBool(IS_DASHING, true);
        agent.isStopped = true;
        Vector3 moveDirection = transform.forward;
        float moveDistance = dashSpeed * Time.deltaTime;
        Vector3 capsulePoint1 = transform.position + Vector3.up * capsuleBottomOffset;
        Vector3 capsulePoint2 = transform.position + Vector3.up * capsuleTopOffset;
        GenerateImpulse();
        RaycastHit hit;
        if (Physics.CapsuleCast(capsulePoint1, capsulePoint2, dashCapsuleRadius, transform.forward, out hit, moveDistance + 0.5f, dashLayerMask))
        {
            Collider c = hit.collider;
            if (debug)
                Debug.Log("Dash Hit: " + c.name);
            if (c != null)
            {
                PlayerHealth playerHealth = c.GetComponentInParent<PlayerHealth>();
                if (playerHealth != null)
                {
                    PlayerHealth.Instance.TakeDamage(dashDamage);
                    GenerateImpulse();
                    didHitPlayer = true;
                    SwitchToIdleState();
                    return;
                }
                else
                {
                    didHitPlayer = false;
                    SwitchToStunnedState();
                    return;
                }
            }
        }

        if (currentState == DirectorState.Dashing)
            agent.Move(moveDirection * moveDistance);
        FaceDirection(player.position, .5f);

        if (dashTimer >= dashDuration)
        {
            didHitPlayer = false;
            SwitchToIdleState();
        }
    }

    private void StunnedState()
    {
        if (canThrowLimbNow)
        {
            SwitchToLimbThrowingState();
            canThrowLimbNow = false;
            return;
        }

        MakeVulnerable();
        animator.SetBool(IS_WALKING, false);
        animator.SetBool(IS_DASHING, false);
        animator.SetBool(IS_STUNNED, true);
        agent.isStopped = true;
        agent.speed = 0;
        dashInWalkingTimer = 0f;
        didHitPlayer = false;

        if (stunTimer >= stunTimerMax)
        {
            stunTimer = 0f;
            animator.SetBool(IS_STUNNED, false);
            DOTween.To(() => animator.GetLayerWeight(2), x => animator.SetLayerWeight(2, x), 0f, .25f).SetEase(Ease.OutQuad);
            // DOTween.To(() => headRig.weight, x => headRig.weight = x, 1f, stunTimerMax).SetEase(Ease.OutQuad);
            SwitchToMovingState();
        }
        else
        {
            stunTimer += Time.deltaTime;
        }
    }

    private void PunchingState()
    {
        MakeVulnerable();
        FaceDirection(player.position, 10f);
        dashInWalkingTimer = 0f;
        agent.Move(transform.forward * punchingMoveSpeed * Time.deltaTime);
    }

    private void ThrowingLimbState()
    {
        animator.SetBool(IS_STUNNED, false);
        animator.SetBool(IS_WALKING, false);
        animator.SetBool(IS_DASHING, false);
        FaceDirection(player.position, 5f);
        dashInWalkingTimer = 0f;
        agent.isStopped = true;
        agent.speed = 0;
        didHitPlayer = false;
    }

    private void FakeDeathState() { }

    private void GettingUpState() { }

    private void RealDeathState() { }
    //* Misc
    public void Damage(float amount, GameObject hitBox, bool isStun = false)
    {
        if (isDead) return;

        // if (debug)
        //     Debug.Log("Damage On " + hitBox.name);
        //? spine.006 is head hitbox
        if (hitBox.name == "spine.006")
        {
            amount *= 1.2f;
            animator.SetLayerWeight(1, 1);
            int randomHeadshot = Random.Range(0, 2);
            if (randomHeadshot == 0)
                animator.CrossFade(HEADSHOT_1, 0, 1);
            else
                animator.CrossFade(HEADSHOT_2, 0, 1);
        }

        if (!isInvulnerable)
        {
            currentHealth -= amount;
            PlayGettingHit();
        }
        else currentHealth -= 1;

        if (isStun && !isInvulnerable && currentState != DirectorState.Stunned && currentState != DirectorState.PreparingToDash && currentState != DirectorState.ThrowingLimb)
        {
            SwitchToStunnedState();
        }

        //* Switching on different health states
        if (currentHealth <= 0)
        {
            //? Real Death here
            if (!isDead)
                SwitchToRealDeathState();
        }
        else if (currentHealth <= 1000 && currentHealth > 0)
        {
            //? Fake Death here
            if (!hasFakeDeathHappened)
                SwitchToFakeDeathState();
        }
        else if (currentHealth <= 1500 && currentHealth > 1000)
        {
            //? Throw Limb here

            if (limbIndex == 3)
                canThrowLimbNow = true;
        }
        else if (currentHealth <= 2000 && currentHealth > 1500)
        {
            //? Throw Limb here

            if (limbIndex == 2)
                canThrowLimbNow = true;
        }
        else if (currentHealth <= 2500 && currentHealth > 2000)
        {
            //? Throw Limb here

            if (limbIndex == 1)
                canThrowLimbNow = true;
        }
        else if (currentHealth <= 3000 && currentHealth > 2500)
        {
            //? Throw Limb here

            if (limbIndex == 0)
                canThrowLimbNow = true;
        }
    }

    private void SwitchToIdleState()
    {
        currentState = DirectorState.Idle;
        idleTimer = 0f;
        if (didHitPlayer)
        {
            animator.SetLayerWeight(2, 1);
            animator.CrossFade(HOLDING_HEAD, 0.5f, 2);
        }
    }

    private void SwitchToPunchingState()
    {
        currentState = DirectorState.Punching;
        int randomPunch = Random.Range(0, 2);
        if (randomPunch == 0)
            animator.SetTrigger(PUNCH_1);
        else
            animator.SetTrigger(PUNCH_2);
    }

    private void SwitchToMovingState()
    {
        currentState = DirectorState.Moving;
        agent.isStopped = false;
        dashInWalkingTimer = 0f;
        bool shouldDash = Random.value < dashChance;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (shouldDash && distanceToPlayer >= distanceFromPlayerToDash)
        {
            SwitchToPreparingToDashState();
        }
        else
        {
            currentState = DirectorState.Moving;
            animator.SetBool(IS_WALKING, true);
            animator.SetBool(IS_DASHING, false);
            agent.speed = normalWalkSpeed;
            agent.isStopped = false;
        }
    }

    public void SwitchToPreparingToDashState()
    {
        if (!canDash) return;

        Debug.DrawRay(transform.position + Vector3.up * 0.5f, player.position - transform.position + Vector3.up * 0.5f, Color.red);

        float moveDistance = dashSpeed * Time.deltaTime;
        Vector3 capsulePoint1 = transform.position + Vector3.up * capsuleBottomOffset;
        Vector3 capsulePoint2 = transform.position + Vector3.up * capsuleTopOffset;

        RaycastHit hit;
        RaycastHit hit2;

        bool didRaycast = Physics.Raycast(transform.position + Vector3.up * 0.5f, player.position - transform.position + Vector3.up * 0.5f, out hit, distanceFromPlayerToDash, dashLayerMask);
        bool didCapsuleCast = Physics.CapsuleCast(capsulePoint1, capsulePoint2, dashCapsuleRadius, transform.forward, out hit2, moveDistance + 0.5f, dashLayerMask);

        if (!didRaycast && !didCapsuleCast)
        {
            GenerateImpulse();
            currentState = DirectorState.PreparingToDash;
            animator.SetBool(IS_DASHING, true);
            animator.SetBool(IS_WALKING, false);
            animator.SetBool(IS_STUNNED, false);
            Vector3 direction = (player.position - transform.position).normalized;
            Vector3 lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z)).eulerAngles;
            transform.DORotate(lookRotation, 0.2f);
            agent.speed = 0;
            agent.isStopped = true;
        }
        else
        {
            SwitchToMovingState();
        }
    }

    private void SwitchToStunnedState(bool shouldSetOnCooldown = true)
    {
        if (!canBeStunned) return;

        currentState = DirectorState.Stunned;
        animator.SetBool(IS_STUNNED, true);
        animator.SetBool(IS_DASHING, false);
        animator.SetBool(IS_WALKING, false);
        animator.SetLayerWeight(2, 1);
        animator.CrossFade(STUN, 0.25f, 2);
        agent.isStopped = true;
        DOTween.To(() => headRig.weight, x => headRig.weight = x, 0f, stunTimerMax).SetEase(Ease.OutQuad);
        dashTimer = 0f;
        dashInWalkingTimer = 0f;

        if (shouldSetOnCooldown)
        {
            canBeStunned = false;

            StartCoroutine(ResetCanBeStunnedEnable(stunTimerMax * 2));
        }
    }

    private IEnumerator ResetCanBeStunnedEnable(float time = 2f)
    {
        yield return new WaitForSeconds(time);
        canBeStunned = true;
    }

    private IEnumerator ResetCanDashEnable(float time = 2f)
    {
        yield return new WaitForSeconds(time);
        canDash = true;
    }

    private void SwitchToLimbThrowingState()
    {
        if (limbIndex > 3) return;
        var currentLimbThrowable = limbThrowable[limbIndex];
        ThrowableLimb throwable = currentLimbThrowable.GetComponent<ThrowableLimb>();
        currentState = DirectorState.ThrowingLimb;
        animator.SetBool(IS_WALKING, false);
        animator.SetBool(IS_DASHING, false);
        animator.SetBool(IS_STUNNED, false);
        agent.isStopped = true;
        agent.speed = 0;
        dashInWalkingTimer = 0f;
        animator.SetLayerWeight(2, 1);
        animator.Play(throwable.animationString, 2);
    }

    private void SwitchToFakeDeathState()
    {
        currentState = DirectorState.FakeDeath;
        animator.SetLayerWeight(2, 1);
        animator.Play(FAKE_DEATH, 2);
        agent.isStopped = true;
        agent.speed = 0;
        dashInWalkingTimer = 0f;
        hasFakeDeathHappened = true;
        MakeInvulnerable();
        StartCoroutine(GetUpAfterFakeDeath());
    }

    private IEnumerator GetUpAfterFakeDeath()
    {
        yield return new WaitForSeconds(6f);
        SwitchToGettingUpState();
    }

    private void SwitchToGettingUpState()
    {
        currentState = DirectorState.GettingUp;
        animator.SetLayerWeight(2, 1);
        animator.Play(GETTING_UP, 2);
        agent.isStopped = true;
        agent.speed = 0;
        dashInWalkingTimer = 0f;
    }

    private void SwitchToRealDeathState()
    {
        currentState = DirectorState.RealDeath;
        animator.SetLayerWeight(2, 1);
        animator.Play(REAL_DEATH, 2);
        agent.isStopped = true;
        agent.speed = 0;
        dashInWalkingTimer = 0f;
        isDead = true;

        foreach (Collider collider in GetComponentsInChildren<Collider>())
        {
            collider.enabled = false;
        }

        foreach (Rigidbody rigidBody in GetComponentsInChildren<Rigidbody>())
        {
            rigidBody.isKinematic = true;
        }

        foreach (GameObject limbBlood in limbBloods)
        {
            limbBlood.SetActive(false);
        }
    }

    public void SwitchToDoorOpeningState()
    {
        gameObject.SetActive(true);
        currentState = DirectorState.OpeningDoor;
        animator.CrossFade(OPENING_DOOR, 0f, 0);
        agent.isStopped = true;
        agent.speed = 0;
        dashInWalkingTimer = 0f;
        GenerateImpulse();
        MakeInvulnerable();
    }

    private void FaceDirection(Vector3 position, float rotateSpeed = 5f)
    {
        Vector3 direction = (position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotateSpeed);
    }

    public void GenerateImpulse()
    {
        if (canGenerateImpulse)
        {
            impulseSource.GenerateImpulse();
            canGenerateImpulse = false;
            StartCoroutine(ResetGenerateImpulse());
        }
    }

    private IEnumerator ResetGenerateImpulse()
    {
        yield return new WaitForSeconds(.5f);
        canGenerateImpulse = true;
    }

    //* Animation Events
    public void HeadshotAnmationComplete()
    {
        animator.SetLayerWeight(1, 0);
    }

    public void PunchStarting()
    {
        animator.SetFloat(PUNCH_MP, 0f);
        StartCoroutine(PunchResume());
    }

    private IEnumerator PunchResume()
    {
        yield return new WaitForSeconds(0.15f);
        animator.SetFloat(PUNCH_MP, 1f);
    }

    public void FinishPunching()
    {
        SwitchToIdleState();
    }

    public void PunchDamage()
    {
        float moveDistance = dashSpeed * Time.deltaTime;
        Vector3 capsulePoint1 = transform.position + Vector3.up * capsuleBottomOffset;
        Vector3 capsulePoint2 = transform.position + Vector3.up * capsuleTopOffset;
        RaycastHit hit;
        if (Physics.CapsuleCast(capsulePoint1, capsulePoint2, dashCapsuleRadius, transform.forward, out hit, moveDistance + 1f, dashLayerMask))
        {
            Collider c = hit.collider;
            if (c != null)
            {
                PlayerHealth playerHealth = c.GetComponentInParent<PlayerHealth>();
                if (playerHealth != null)
                {
                    PlayerHealth.Instance.TakeDamage(attackDamage);
                    GenerateImpulse();
                    didHitPlayer = true;
                }
            }
        }
    }

    public void MakeInvulnerable()
    {
        isInvulnerable = true;
    }

    public void MakeVulnerable()
    {
        isInvulnerable = false;
    }

    public void LimbThrowableActivate()
    {
        var currentLimb = limb[limbIndex];
        var currentThrowable = limbThrowable[limbIndex];
        var limbBlood = limbBloods[limbIndex];

        limbBlood.SetActive(true);

        currentLimb.SetActive(false);
        currentThrowable.SetActive(true);
    }

    public void ThrowLimbAnimEvent()
    {
        var currentThrowable = limbThrowable[limbIndex];
        ThrowableLimb throwable = currentThrowable.GetComponent<ThrowableLimb>();
        currentThrowable.transform.SetParent(null, true);
        throwable.ThrowLimb(mainCam, GenerateImpulse);
    }

    public void FinishLimbThrowing()
    {
        animator.SetLayerWeight(2, 0);
        didHitPlayer = true;
        SwitchToIdleState();
        limbIndex++;
        canThrowLimbNow = false;
    }

    public void SwitchToDashingState()
    {
        if (!canDash) return;

        currentState = DirectorState.Dashing;
        // directionToDashTowards = (player.position - transform.position).normalized;
        agent.isStopped = true;
        dashTimer = 0f;
        dashInWalkingTimer = 0f;

        canDash = false;

        StartCoroutine(ResetCanDashEnable(dashDuration * 3));
    }

    public void FinishGettingUp()
    {
        DOTween.To(() => animator.GetLayerWeight(2), x => animator.SetLayerWeight(2, x), 0f, .25f).SetEase(Ease.OutQuad);
        SwitchToMovingState();
        MakeVulnerable();
        stunTimerMax /= 2f;
    }

    public void FinishOpeningDoor()
    {
        SwitchToMovingState();
        MakeVulnerable();
    }

    //* Audio Events

    public void PlayBreathing()
    {
        if (isDead) return;
        if (animator.GetLayerWeight(2) <= 0 && canBreath)
        {
            AudioManager.Instance.PlayOneShot(hasFakeDeathHappened ? angryBreathing : breathing, transform.position);
            canBreath = false;

            StartCoroutine(ResetCanBreathEnable(.7f));
        }
    }

    public void PlayOtherLayerBreathing() => AudioManager.Instance.PlayOneShot(angryBreathing, transform.position);

    private IEnumerator ResetCanBreathEnable(float time)
    {
        yield return new WaitForSeconds(time);
        canBreath = true;
    }

    public void PlayGettingUp() => AudioManager.Instance.PlayOneShot(gettingUp, transform.position);
    public void PlayGettingHit() => AudioManager.Instance.PlayOneShot(gettingHit, transform.position);
    public void PlayPunching()
    {
        if (isDead) return;
        if (animator.GetLayerWeight(2) <= 0.5f || currentState == DirectorState.ThrowingLimb)
            AudioManager.Instance.PlayOneShot(punching, transform.position);
    }
    public void PlayFakeDeath() => AudioManager.Instance.PlayOneShot(fakeDeath, transform.position);
    public void PlayOpeningDoor() => AudioManager.Instance.PlayOneShot(openingDoor, transform.position);
    public void PlayPreparingToDash()
    {
        if (isDead) return;
        if (animator.GetLayerWeight(2) <= 0.5f)
            AudioManager.Instance.PlayOneShot(preparingToDash, transform.position);
    }
    public void PlayRealDeath() => AudioManager.Instance.PlayOneShot(realDeath, transform.position);
    public void PlayThrowingLimb() => AudioManager.Instance.PlayOneShot(throwingLimb, transform.position);
    public void PlayBodyDropping() => AudioManager.Instance.PlayOneShot(bodyDropping, transform.position);
    public void PlayFootsteps()
    {
        if (isDead) return;
        if (agent.speed > 0 || currentState == DirectorState.Dashing)
            AudioManager.Instance.PlayOneShot(footsteps, transform.position);
    }

    public void PlayGoreSound() => AudioManager.Instance.PlayOneShot(goreSound, transform.position);

    public void PlayOtherLayerFootsteps()
    {
        if (isDead) return;
        AudioManager.Instance.PlayOneShot(footsteps, transform.position);
    }

    //* Gizmos
    private void DrawApproxCapsule(Vector3 bottomCenter, Vector3 topCenter, float radius)
    {
        if (debug == false) return;
        Vector3 right = transform.right;
        Vector3 forward = transform.forward;
        Vector3 b_r = bottomCenter + right * radius;
        Vector3 b_l = bottomCenter - right * radius;
        Vector3 b_f = bottomCenter + forward * radius;
        Vector3 b_b = bottomCenter - forward * radius;
        Vector3 t_r = topCenter + right * radius;
        Vector3 t_l = topCenter - right * radius;
        Vector3 t_f = topCenter + forward * radius;
        Vector3 t_b = topCenter - forward * radius;
        Gizmos.DrawWireSphere(bottomCenter, radius);
        Gizmos.DrawWireSphere(topCenter, radius);
        Gizmos.DrawLine(b_r, t_r);
        Gizmos.DrawLine(b_l, t_l);
        Gizmos.DrawLine(b_f, t_f);
        Gizmos.DrawLine(b_b, t_b);
        Gizmos.DrawLine(b_r, b_f);
        Gizmos.DrawLine(b_f, b_l);
        Gizmos.DrawLine(b_l, b_b);
        Gizmos.DrawLine(b_b, b_r);
        Gizmos.DrawLine(t_r, t_f);
        Gizmos.DrawLine(t_f, t_l);
        Gizmos.DrawLine(t_l, t_b);
        Gizmos.DrawLine(t_b, t_r);
    }

    private void OnDrawGizmosSelected()
    {
        if (debug == false) return;
        Gizmos.color = Color.magenta;
        Vector3 dir = directionToDashTowards;
        if (dir == Vector3.zero) dir = transform.forward;
        Vector3 bottom = transform.position + Vector3.up * capsuleBottomOffset;
        Vector3 top = transform.position + Vector3.up * capsuleTopOffset;
        DrawApproxCapsule(bottom, top, dashCapsuleRadius);
        Vector3 projected = transform.position + dir.normalized * dashSpeed * dashDuration;
        Vector3 pBottom = projected + Vector3.up * capsuleBottomOffset;
        Vector3 pTop = projected + Vector3.up * capsuleTopOffset;
        DrawApproxCapsule(pBottom, pTop, dashCapsuleRadius);
        Gizmos.DrawLine(bottom, pBottom);
        Gizmos.DrawLine(top, pTop);
    }
}
