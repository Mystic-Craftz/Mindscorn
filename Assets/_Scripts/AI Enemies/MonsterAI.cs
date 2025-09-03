using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using FMODUnity;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public enum MonsterStartState
{
    Idle,
    Wander,
    Chase,
    Attack,
    Incapacitated,
    Hiss,
    Die,
    Hit,
    Stun
}

[RequireComponent(typeof(NavMeshAgent))]
public class MonsterAI : MonoBehaviour, ISaveable
{

    [Header("AIManager Stuff")]
    [Tooltip("Set manually in Inspector. Positive unique integer per instance.")]
    [SerializeField] private int monsterID = -1;

    [Tooltip("Choose the monster type in Inspector.")]
    [SerializeField] private MonsterType monsterType = MonsterType.None;

    // read-only accessors for AIManager
    public int MonsterID => monsterID;
    public MonsterType MonsterType => monsterType;

    [Header("Registration Settings")]
    [Tooltip("Disable immediately after registration")]
    public bool disableAfterRegistration = false;


    // Components & references
    public NavMeshAgent agent;
    public AIAnimationController aiAnimator;
    public AISensor aiSensor;
    public Transform playerTransform;
    public AIHealth aiHealth;
    public CinemachineImpulseSource impulseSource;

    // State machine & states
    public StateMachine stateMachine;
    public IState idleState;
    public IState wanderState;
    public IState chaseState;
    public IState attackState;
    public IState incapacitatedState;
    public IState hissState;
    public IState dieState;
    public IState hitState;
    public IState stunState;

    // Properties for States
    [Header("Start State")]
    public MonsterStartState startingState = MonsterStartState.Wander;


    [Header("Idle Behavior Stuff")]
    public Transform eatingSpot;


    [Header("Attack Behavior Stuff")]
    public float attackDamage = 10f;
    public float attackRange = 1.5f;
    public float attackCooldown = 1.0f;
    public float rotationSpeed = 10f;  // how fast they turn
    public float attackCooldownTimer;
    [HideInInspector] public bool immediateAttack = true;
    [HideInInspector] public bool playerWasInSight = false;


    [Header("Hiss Behavior Stuff")]
    [HideInInspector] public IState nextStateAfterHiss;
    [HideInInspector] public bool hasHissedAfterHit = false;



    [Header("Wander Settings")]
    public Transform wanderCenter;
    public float wanderRadius = 10f;         // how far from center they can go
    public float wanderIntervalMin = 3f;     // min seconds between picks
    public float wanderIntervalMax = 8f;     // max seconds between picks


    [Header("Power Surge Settings")]
    [Range(0f, 1f)]
    public float powerSurgeChance = 0.2f;
    public int normalHitsToInterrupt = 2;
    [HideInInspector] public int surgeNormalHitsCount = 0;
    [HideInInspector] public bool isPowerSurging = false;
    [HideInInspector] public float powerSurgeSpeed = 3f;


    [Header("Attack Hit Setup")]
    public bool showGizmo = false;
    public Vector3 attackOffset = Vector3.zero;
    public float attackRadius = 0.5f;
    public LayerMask hitLayers;


    [Header("Hit Reaction Settings")]

    [Tooltip("Backward push (units) to apply on a HARD hit.")]
    public float hardHitForce = 3f;

    [Tooltip("Optional small push on a normal hit (usually 0).")]
    public float normalHitForce = 0f;

    [Tooltip("How long (seconds) to stay in HitState at minimum, even if the animation finishes.")]
    public float onGroundTime = 1.5f;
    public float hardKnockbackDistance = 3f;
    public float hardKnockbackDuration = 0.1f;
    [HideInInspector] public bool isTrembling = false;


    [HideInInspector] public Vector3 lastHitDirection;
    [HideInInspector] public bool lastHitWasHard;
    [HideInInspector] public bool isProcessingHit = false;
    [HideInInspector] public IState queuedStateAfterHit = null;


    [Header("Stun Settings")]
    public float stunTime = 3.0f;
    public float currentStunTimer = 0f;
    [HideInInspector] public bool lastHitWasStun = false;
    [HideInInspector] public IState nextStateAfterStun = null;


    [Header("Incapacitation Settings")]
    [Tooltip("How close the player must come to resurrect")]
    public float incapacitatedDetectionRadius = 5f;

    [Tooltip("Health range applied on each resurrection")]
    public int resurrectionMinHealth = 100;
    public int resurrectionMaxHealth = 250;

    [Range(0f, 1f)]
    public float resurrectionChance = 0.3f;
    public int incapacitatedDetectionCount = 0;
    [HideInInspector] public bool isIncapacitated = false;
    [HideInInspector] public float originalSensorRadius;
    [HideInInspector] public bool isResurrecting = false;
    [HideInInspector] public IState queuedStateAfterResurrection = null;
    [HideInInspector] public bool isStartingIncapacitated = false;


    [Header("FMOD Stuff")]
    [SerializeField] private EventReference attackSound;
    [SerializeField] private EventReference hissSound;
    [SerializeField] private EventReference chaseSound;
    [SerializeField] private EventReference hitSound;
    [SerializeField] private EventReference deathSound;
    [SerializeField] private EventReference tremblingSound;
    [SerializeField] private EventReference walkingSound;
    [SerializeField] private EventReference ChaseSoundRef => chaseSound;
    [SerializeField] private EventReference WalkingSoundRef => walkingSound;
    [SerializeField] private EventReference footstepSound;
    [SerializeField] private EventReference sprintFootstepSound;
    public EventReference stunSound;
    public EventReference idleGoreSound;
    private Dictionary<Type, int> stateSoundIds = new Dictionary<Type, int>();
    private int nextSoundId = 0;
    [SerializeField] private float footstepMovementThreshold = 0.01f; // ~0.1 units/sec
    private class WalkFootstepState { }
    private class SprintFootstepState { }

    private enum FootstepState { None, Walk, Sprint }
    private FootstepState currentFootstepState = FootstepState.None;



    [Header("Hitboxes")]
    [SerializeField] private List<Collider> pathBlockingHitboxes = new List<Collider>();
    [SerializeField] private bool autoCollectChildColliders = true;
    private readonly Dictionary<Collider, bool> originalHitboxEnabled = new Dictionary<Collider, bool>();


    //All Animations Strings
    [HideInInspector] public string hitNormalFrontAnim = "HitNormalFront";
    [HideInInspector] public string hitNormalBackAnim = "HitNormalBack";
    [HideInInspector] public string hitHardFrontAnim = "HitHardFront";
    [HideInInspector] public string hitHardBackAnim = "HitHardBack";
    [HideInInspector] public string getUpBackAnim = "GettingUpFront";
    [HideInInspector] public string getUpFrontAnim = "GettingUpBack";
    [HideInInspector] public string lieFrontAnim = "DeadFront";
    [HideInInspector] public string lieBackAnim = "DeadBack";
    [HideInInspector] public string deadAnim = "Dead";
    [HideInInspector] public string trembleFrontAnim = "TrembleFront";
    [HideInInspector] public string trembleBackAnim = "TrembleBack";
    [HideInInspector] public string stunAnim = "Stuned";
    [HideInInspector] public string crouchAnim = "Crouch";
    [HideInInspector] public string gettingUpAnim = "GettingUp";
    [HideInInspector] public string eatingAnim = "Eating";
    [HideInInspector] public string gettingDownAnim = "GettingDown";
    [HideInInspector] public string attackAnim = "Attack";
    [HideInInspector] public string locomotionAnim = "Locomotion";
    [HideInInspector] public string hissAnim = "Hissing";

    private bool hasBeenActivated = false;



    [Header("For Debugging")]
    public string currentStateName;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        aiAnimator = GetComponent<AIAnimationController>();
        aiSensor = GetComponent<AISensor>();
        aiHealth = GetComponent<AIHealth>();

        if (playerTransform == null)
            playerTransform = GameObject.FindWithTag("Player")?.transform;

        originalSensorRadius = aiSensor.viewRadius;

        // Instantiate all state objects first
        idleState = new IdleState(this);
        wanderState = new WanderState(this);
        chaseState = new ChaseState(this);
        attackState = new AttackState(this);
        incapacitatedState = new IncapacitatedState(this);
        hissState = new HissState(this);
        dieState = new DieState(this);
        hitState = new HitState(this);
        stunState = new StunState(this);

        // Select the initial state
        IState initialState = startingState switch
        {
            MonsterStartState.Idle => idleState,
            MonsterStartState.Wander => wanderState,
            MonsterStartState.Chase => chaseState,
            MonsterStartState.Attack => attackState,
            MonsterStartState.Incapacitated => incapacitatedState,
            MonsterStartState.Hiss => hissState,
            MonsterStartState.Die => dieState,
            MonsterStartState.Hit => hitState,
            MonsterStartState.Stun => stunState,
            _ => idleState
        };

        if (startingState == MonsterStartState.Incapacitated)
            isStartingIncapacitated = true;

        stateMachine = new StateMachine(this, initialState);

        GetStateSoundId(typeof(WanderState));
        GetStateSoundId(typeof(ChaseState));

        if (autoCollectChildColliders)
        {
            CollectChildColliders(includeInactive: false);
        }
        else
        {
            CacheOriginalHitboxStates();
        }
    }

    void Start()
    {
        AIManager.Register(this);

        if (disableAfterRegistration && gameObject.activeSelf && !hasBeenActivated)
        {
            gameObject.SetActive(false);
        }
    }

    void OnDestroy()
    {
        AIManager.Unregister(this);
    }


    void OnEnable()
    {
        ReenterCurrentState();
    }

    void OnDisable()
    {
        StopAllStateSounds();
    }

    private void ReenterCurrentState()
    {
        var cur = stateMachine.CurrentState;
        if (cur == null) return;

        try
        {
            cur.Exit();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[MonsterAI] Reenter Exit exception: {ex}");
        }

        try
        {
            cur.Enter();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[MonsterAI] Reenter Enter exception: {ex}");
        }
    }

    void Update()
    {

        if (aiHealth.currentHealth <= 0f
            && stateMachine.CurrentState != dieState
            && stateMachine.CurrentState != incapacitatedState
            && !isResurrecting && !isProcessingHit)
        {
            stateMachine.ChangeState(dieState);
            return;
        }

        stateMachine.Update();

        currentStateName = stateMachine.CurrentState != null
            ? stateMachine.CurrentState.GetType().Name
            : "None";


        ManageFootsteps();
    }


    public void SetActiveState(bool isActive)
    {
        gameObject.SetActive(isActive);
        hasBeenActivated = true;
    }

    public void OnPlayerDetected(Transform player)
    {
        if (isResurrecting) return;

        if (stateMachine.CurrentState == dieState)
            return;

        // —— special start incapacitated path ——
        if (isStartingIncapacitated && stateMachine.CurrentState == incapacitatedState)
        {
            StartCoroutine(StartPhaseWakeUp());
            return;
        }

        // —— Resurrection path ——
        if (isIncapacitated && stateMachine.CurrentState == incapacitatedState)
        {
            incapacitatedDetectionCount++;

            if (incapacitatedDetectionCount > 1
                && UnityEngine.Random.value <= resurrectionChance)
            {
                StartCoroutine(DoResurrectSequence());
            }
            return;
        }

        // —— Normal detection path ——
        if (stateMachine.CurrentState == chaseState) return;
        playerTransform = player;
        if (!playerWasInSight)
        {
            immediateAttack = true;
            playerWasInSight = true;
        }

        if ((stateMachine.CurrentState == stunState && isProcessingHit)
            || aiHealth.currentHealth <= 0f)
            return;

        if (stateMachine.CurrentState == hitState && isProcessingHit)
        {
            queuedStateAfterHit = chaseState;
            return;
        }
        nextStateAfterHiss = chaseState;
        if (aiAnimator.CurrentAnimation == eatingAnim)
            StartCoroutine(JumpUpThenHiss());
        else
            stateMachine.ChangeState(hissState);
    }


    private IEnumerator JumpUpThenHiss()
    {
        aiAnimator.PlayAnimation(gettingUpAnim);
        yield return new WaitForSeconds(aiAnimator.GetClipLength(gettingUpAnim));

        stateMachine.ChangeState(hissState);
    }


    private IEnumerator StartPhaseWakeUp()
    {
        aiAnimator.PlayAnimation(gettingUpAnim);
        yield return new WaitForSeconds(aiAnimator.GetClipLength(gettingUpAnim));


        isStartingIncapacitated = false;
        isIncapacitated = false;
        startingState = MonsterStartState.Wander;
        aiSensor.viewRadius = originalSensorRadius;
        SetPathBlockingHitboxesEnabled(true);

        if (agent.isOnNavMesh)
        {
            agent.updatePosition = true;
            agent.updateRotation = true;
            agent.isStopped = false;
        }
        if (aiSensor.PlayerInSight)
        {
            nextStateAfterHiss = chaseState;
            stateMachine.ChangeState(hissState);
        }
        else
        {
            nextStateAfterHiss = wanderState;
            stateMachine.ChangeState(hissState);
        }
    }


    public void OnPlayerLost()
    {
        if (isResurrecting) return;

        if (stateMachine.CurrentState == dieState)
            return;

        // ── Resurrection path ──
        if (isIncapacitated && stateMachine.CurrentState == incapacitatedState)
            return;

        // ── Normal detection path ──
        immediateAttack = true;
        playerWasInSight = false;
        hasHissedAfterHit = false;

        IState desired = (startingState == MonsterStartState.Idle)
                    ? idleState
                    : wanderState;

        if (stateMachine.CurrentState == hitState && isProcessingHit)
        {
            queuedStateAfterHit = desired;
            return;
        }

        nextStateAfterHiss = desired;
        stateMachine.ChangeState(hissState);
    }

    //this method is called in animation event
    public void OnAttackHit()
    {
        Vector3 origin = transform.position + attackOffset;
        Collider[] hits = Physics.OverlapSphere(origin, attackRadius, hitLayers);

        foreach (var hit in hits)
        {
            if (hit.GetComponent<CharacterController>() != null)
            {
                PlayerHealth.Instance.TakeDamage(attackDamage);
                impulseSource?.GenerateImpulse();
                return;
            }
        }
    }


    // Compute the direction from monster to attacker (so we know front vs back)
    public void RegisterHit(Vector3 origin, bool isHard)
    {
        Vector3 dir = (transform.position - origin).normalized;
        lastHitDirection = dir;
        lastHitWasHard = isHard;
    }

    private IEnumerator DoResurrectSequence()
    {
        isResurrecting = true;

        // restore health & flags
        aiHealth.currentHealth = UnityEngine.Random.Range(resurrectionMinHealth, resurrectionMaxHealth);
        isIncapacitated = false;
        aiSensor.viewRadius = originalSensorRadius;

        // tremble + wait
        string trembleClip = lastHitWasHard
            ? (Vector3.Dot(transform.forward, lastHitDirection) < 0f ? trembleBackAnim : trembleFrontAnim)
            : trembleFrontAnim;
        aiAnimator.PlayAnimation(trembleClip);
        PlayTremblingSound();
        yield return new WaitForSeconds(onGroundTime);

        // get‑up + wait
        string getUpClip = lastHitWasHard
            ? (Vector3.Dot(transform.forward, lastHitDirection) < 0f ? getUpFrontAnim : getUpBackAnim)
            : getUpBackAnim;
        yield return aiAnimator.PlayAndWait(getUpClip, 0.1f);

        SetPathBlockingHitboxesEnabled(true);

        // // unfreeze NavMeshAgent
        if (agent.isOnNavMesh)
        {
            agent.updatePosition = true;
            agent.updateRotation = true;
            agent.isStopped = false;
        }

        // unlock ChangeState
        isResurrecting = false;

        if (aiSensor.PlayerInSight)
        {
            nextStateAfterHiss = chaseState;
            stateMachine.ChangeState(hissState);
        }
        else
        {
            IState desired = (startingState == MonsterStartState.Idle)
                     ? idleState
                     : wanderState;

            nextStateAfterHiss = desired;
            stateMachine.ChangeState(hissState);
        }

    }



    void OnDrawGizmosSelected()
    {
        if (!showGizmo) return;
        Vector3 origin = transform.position + attackOffset;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(origin, attackRadius);
    }


    //FMOD stuff
    public void PlayAttackSound()
    {
        int soundId = GetStateSoundId(typeof(AttackState));
        AudioManager.Instance.PlayStateSound(attackSound, transform.position, soundId);
    }

    public void PlayHissSound()
    {
        int soundId = GetStateSoundId(typeof(HissState));
        AudioManager.Instance.PlayStateSound(hissSound, transform.position, soundId);
    }

    public void PlayDieSound()
    {
        int soundId = GetStateSoundId(typeof(DieState));
        AudioManager.Instance.PlayStateSound(deathSound, transform.position, soundId);
    }

    public void PlayHitSound()
    {
        int soundId = GetStateSoundId(typeof(HitState));
        AudioManager.Instance.PlayStateSound(hitSound, transform.position, soundId);
    }

    public void PlayTremblingSound()
    {
        int soundId = GetStateSoundId(typeof(HitState));
        AudioManager.Instance.PlayStateSound(tremblingSound, transform.position, soundId);
    }

    public void PlayChaseLoop()
    {
        if (AudioManager.Instance == null || chaseSound.IsNull) return;

        StopWanderLoop(); // prevent overlap
        int id = GetStateSoundId(typeof(ChaseState));
        AudioManager.Instance.PlayStateSound(chaseSound, transform.position, id);
    }

    public void StopChaseLoop()
    {
        if (AudioManager.Instance == null) return;

        int id = GetStateSoundId(typeof(ChaseState));
        AudioManager.Instance.StopStateSound(id);
    }

    public void PlayWanderLoop()
    {
        if (AudioManager.Instance == null || walkingSound.IsNull) return;

        int id = GetStateSoundId(typeof(WanderState));
        AudioManager.Instance.PlayStateSound(walkingSound, transform.position, id);
    }

    public void StopWanderLoop()
    {
        if (AudioManager.Instance == null) return;

        int id = GetStateSoundId(typeof(WanderState));
        AudioManager.Instance.StopStateSound(id);
    }


    public void UpdateStateLoopPosition(Type stateType)
    {
        int id = GetStateSoundId(stateType);
        AudioManager.Instance.UpdateStateSoundPosition(id, transform.position);
    }


    public int GetStateSoundId(Type stateType)
    {
        if (!stateSoundIds.ContainsKey(stateType))
        {
            stateSoundIds[stateType] = (GetInstanceID() * 397) ^ stateType.GetHashCode();
        }
        return stateSoundIds[stateType];
    }


    public void StopAllStateSounds()
    {
        foreach (var soundId in stateSoundIds.Values)
        {
            AudioManager.Instance.StopStateSound(soundId);
        }
    }

    private void ManageFootsteps()
    {
        if (agent == null || AudioManager.Instance == null || stateMachine == null)
        {
            StopFootsteps();
            return;
        }

        bool moving = false;
        try
        {
            moving = agent.isOnNavMesh && !agent.isStopped && agent.velocity.sqrMagnitude > footstepMovementThreshold;
        }
        catch { moving = false; }

        if (!moving)
        {
            StopFootsteps();
            return;
        }

        // Decide sprint vs normal footsteps: sprint when in ChaseState and currently surged
        bool wantSprint = (stateMachine.CurrentState != null
                           && stateMachine.CurrentState.GetType() == typeof(ChaseState)
                           && isPowerSurging);

        if (wantSprint)
        {
            if (currentFootstepState != FootstepState.Sprint)
            {
                StopFootsteps();
                if (!sprintFootstepSound.IsNull)
                {
                    int id = GetStateSoundId(typeof(SprintFootstepState));
                    AudioManager.Instance.PlayStateSound(sprintFootstepSound, transform.position, id);
                }
                currentFootstepState = FootstepState.Sprint;
            }
            else
            {
                // keep 3D position updated
                UpdateStateLoopPosition(typeof(SprintFootstepState));
            }
        }
        else
        {
            if (currentFootstepState != FootstepState.Walk)
            {
                StopFootsteps();
                if (!footstepSound.IsNull)
                {
                    int id = GetStateSoundId(typeof(WalkFootstepState));
                    AudioManager.Instance.PlayStateSound(footstepSound, transform.position, id);
                }
                currentFootstepState = FootstepState.Walk;
            }
            else
            {
                UpdateStateLoopPosition(typeof(WalkFootstepState));
            }
        }
    }

    private void StopFootsteps()
    {
        if (AudioManager.Instance == null) { currentFootstepState = FootstepState.None; return; }

        if (currentFootstepState == FootstepState.Walk)
        {
            AudioManager.Instance.StopStateSound(GetStateSoundId(typeof(WalkFootstepState)));
        }
        else if (currentFootstepState == FootstepState.Sprint)
        {
            AudioManager.Instance.StopStateSound(GetStateSoundId(typeof(SprintFootstepState)));
        }

        currentFootstepState = FootstepState.None;
    }


    public void CollectChildColliders(bool includeInactive = false)
    {
        var cols = GetComponentsInChildren<Collider>(includeInactive);
        pathBlockingHitboxes = new List<Collider>(cols);
        CacheOriginalHitboxStates();
    }


    public void CacheOriginalHitboxStates()
    {
        originalHitboxEnabled.Clear();
        if (pathBlockingHitboxes == null) return;
        foreach (var c in pathBlockingHitboxes)
        {
            if (c == null) continue;
            if (!originalHitboxEnabled.ContainsKey(c))
                originalHitboxEnabled[c] = c.enabled;
        }
    }

    public void RegisterPathBlockingHitbox(Collider c)
    {
        if (c == null) return;
        if (pathBlockingHitboxes == null) pathBlockingHitboxes = new List<Collider>();
        if (!pathBlockingHitboxes.Contains(c)) pathBlockingHitboxes.Add(c);
        if (!originalHitboxEnabled.ContainsKey(c)) originalHitboxEnabled[c] = c.enabled;
    }


    public void UnregisterPathBlockingHitbox(Collider c)
    {
        if (c == null || pathBlockingHitboxes == null) return;
        pathBlockingHitboxes.Remove(c);
        originalHitboxEnabled.Remove(c);
    }


    public void SetPathBlockingHitboxesEnabled(bool enabled)
    {
        if (pathBlockingHitboxes == null) return;

        // remove destroyed/null colliders to keep list clean
        pathBlockingHitboxes.RemoveAll(x => x == null);

        if (enabled)
        {
            foreach (var c in pathBlockingHitboxes)
            {
                if (c == null) continue;
                if (originalHitboxEnabled.TryGetValue(c, out var orig))
                    c.enabled = orig;
                else
                    c.enabled = true; // fallback
            }
        }
        else
        {
            // disable all so they won't block the player
            foreach (var c in pathBlockingHitboxes)
            {
                if (c == null) continue;
                c.enabled = false;
            }
        }
    }

    public string GetUniqueIdentifier()
    {
        return monsterType.ToString() + "_" + monsterID;


    }

    public object CaptureState()
    {
        return GetCurrentStateData();
    }

    public void RestoreState(object state)
    {
        string json = state as string;
        if (!string.IsNullOrEmpty(json))
        {
            AIStateData data = JsonUtility.FromJson<AIStateData>(json);
            RestoreStateFromData(data);
        }
    }

    // Add this method to restore state from saved data
    public void RestoreStateFromData(AIStateData data)
    {
        if (data.monsterID != monsterID) return;

        AIManager.Register(this);

        IState targetState = GetStateFromTypeName(data.currentState);

        if (targetState == incapacitatedState)
        {
            SetPathBlockingHitboxesEnabled(false);
        }

        if (disableAfterRegistration)
        {
            gameObject.SetActive(data.hasBeenActivated);
        }
        else
        {
            gameObject.SetActive(data.isActive);
        }

        // Restore transform
        transform.SetPositionAndRotation(new Vector3(data.x, data.y, data.z), Quaternion.Euler(0f, data.rotY, 0f));

        agent.Warp(new Vector3(data.x, data.y, data.z));
        // if (gameObject.activeSelf)
        // {
        //     if (NavMesh.SamplePosition(new Vector3(data.x, data.y, data.z), out NavMeshHit hit, 1f, NavMesh.AllAreas))
        //     {
        //     }
        // }

        if (data.isActive)
        {
            agent.updatePosition = data.agentUpdatePosition;
            agent.updateRotation = data.agentUpdateRotation;
            agent.isStopped = data.agentIsStopped;
        }



        // Restore health
        aiHealth.currentHealth = data.currentHealth;

        // Restore state machine related data
        isPowerSurging = data.isPowerSurging;
        surgeNormalHitsCount = data.surgeNormalHitsCount;
        playerWasInSight = data.playerWasInSight;
        immediateAttack = data.immediateAttack;
        attackCooldownTimer = data.attackCooldownTimer;
        hasHissedAfterHit = data.hasHissedAfterHit;
        hasBeenActivated = data.hasBeenActivated;
        isIncapacitated = data.isIncapacitated;
        isStartingIncapacitated = data.isStartingIncapacitated;
        incapacitatedDetectionCount = data.incapacitatedDetectionCount;
        isResurrecting = data.isResurrecting;
        isProcessingHit = data.isProcessingHit;
        resurrectionChance = data.resurrectionChance;
        lastHitWasHard = data.lastHitWasHard;
        lastHitWasStun = data.lastHitWasStun;
        lastHitDirection = new Vector3(data.lastHitDirectionx, data.lastHitDirectiony, data.lastHitDirectionz);
        currentStunTimer = data.currentStunTimer;

        // Restore queued states
        if (!string.IsNullOrEmpty(data.queuedStateAfterHitType))
        {
            queuedStateAfterHit = GetStateFromTypeName(data.queuedStateAfterHitType);
        }

        if (!string.IsNullOrEmpty(data.nextStateAfterHissType))
        {
            nextStateAfterHiss = GetStateFromTypeName(data.nextStateAfterHissType);
        }

        if (!string.IsNullOrEmpty(data.nextStateAfterStunType))
        {
            nextStateAfterStun = GetStateFromTypeName(data.nextStateAfterStunType);
        }

        // Change to the saved state
        if (targetState != null)
        {
            stateMachine.ChangeState(targetState);
            if (targetState == wanderState && gameObject.activeSelf) agent.SetDestination(transform.position);
        }
    }

    // Helper method to get state from type name
    private IState GetStateFromTypeName(string typeName)
    {
        switch (typeName)
        {
            case "IdleState": return idleState;
            case "WanderState": return wanderState;
            case "ChaseState": return chaseState;
            case "AttackState": return attackState;
            case "IncapacitatedState": return incapacitatedState;
            case "HissState": return hissState;
            case "DieState": return dieState;
            case "HitState": return hitState;
            case "StunState": return stunState;
            default: return null;
        }
    }

    // Add this class to hold AI state data
    public class AIStateData
    {
        public float health;
        public int monsterID;
        public float x;
        public float y;
        public float z;
        public float rotY;
        public bool isActive;
        public float currentHealth;
        public string currentState;
        public bool isPowerSurging;
        public int surgeNormalHitsCount;
        public bool playerWasInSight;
        public bool immediateAttack;
        public bool agentUpdatePosition;
        public bool agentUpdateRotation;
        public bool agentIsStopped;
        public bool hasBeenActivated;
        public float attackCooldownTimer;
        public bool hasHissedAfterHit;
        public bool isIncapacitated;
        public bool isStartingIncapacitated;
        public int incapacitatedDetectionCount;
        public bool isResurrecting;
        public bool isProcessingHit;
        public bool lastHitWasHard;
        public bool lastHitWasStun;
        public float lastHitDirectionx;
        public float lastHitDirectiony;
        public float lastHitDirectionz;
        public float currentStunTimer;
        public float resurrectionChance;
        public string queuedStateAfterHitType;
        public string nextStateAfterHissType;
        public string nextStateAfterStunType;
    }

    public AIStateData GetCurrentStateData()
    {
        return new AIStateData
        {
            health = aiHealth.currentHealth,
            monsterID = monsterID,
            x = transform.position.x,
            y = transform.position.y,
            z = transform.position.z,
            isActive = gameObject.activeSelf,
            agentIsStopped = gameObject.activeSelf ? agent.isStopped : true,
            agentUpdatePosition = agent.updatePosition,
            agentUpdateRotation = agent.updateRotation,
            rotY = transform.rotation.eulerAngles.y,
            currentHealth = aiHealth.currentHealth,
            currentState = stateMachine.CurrentState != null ? stateMachine.CurrentState.GetType().Name : "None",
            isPowerSurging = isPowerSurging,
            surgeNormalHitsCount = surgeNormalHitsCount,
            playerWasInSight = playerWasInSight,
            immediateAttack = immediateAttack,
            attackCooldownTimer = attackCooldownTimer,
            hasHissedAfterHit = hasHissedAfterHit,
            isIncapacitated = isIncapacitated,
            isStartingIncapacitated = isStartingIncapacitated,
            incapacitatedDetectionCount = incapacitatedDetectionCount,
            isResurrecting = isResurrecting,
            isProcessingHit = isProcessingHit,
            lastHitWasHard = lastHitWasHard,
            hasBeenActivated = hasBeenActivated,
            lastHitWasStun = lastHitWasStun,
            lastHitDirectionx = lastHitDirection.x,
            lastHitDirectiony = lastHitDirection.y,
            lastHitDirectionz = lastHitDirection.z,
            resurrectionChance = resurrectionChance,
            currentStunTimer = currentStunTimer,
            queuedStateAfterHitType = queuedStateAfterHit != null ? queuedStateAfterHit.GetType().Name : "",
            nextStateAfterHissType = nextStateAfterHiss != null ? nextStateAfterHiss.GetType().Name : "",
            nextStateAfterStunType = nextStateAfterStun != null ? nextStateAfterStun.GetType().Name : ""
        };
    }
}
