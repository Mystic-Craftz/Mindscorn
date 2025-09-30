using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(BossSensor))]
[RequireComponent(typeof(NavMeshAgent))]
public class BossAI : MonoBehaviour
{
    public enum BossStartState
    {
        Wander,
        Chase,
        Attack,
        Search,
        Stun
    }

    [Header("Debug Info")]
    public string currentStateName;

    [Header("Start State")]
    public BossStartState startingState = BossStartState.Wander;

    [Header("Registration Settings")]
    public bool disableAfterRegistration = false;
    [HideInInspector] public bool hasBeenActivated = false;

    [HideInInspector] public Transform player;
    [HideInInspector] public BossStateMachine stateMachine;
    [HideInInspector] public AIAnimationController anim;
    [HideInInspector] public BossHealth health;
    [HideInInspector] public BossSensor sensor;
    [HideInInspector] public NavMeshAgent agent;
    public CinemachineImpulseSource impulseSource;

    // States (construct these from their classes)
    [HideInInspector] public BossWanderState wanderState;
    [HideInInspector] public BossChaseState chaseState;
    [HideInInspector] public BossSearchState searchState;
    [HideInInspector] public BossAttackState attackState;
    [HideInInspector] public BossStunState stunState;

    // Wander State Settings 
    [Header("Wander State Settings")]
    public float wanderSpeed = 2f;
    public float wanderMinDistance = 3f;      // min distance from center (player/last known)
    public float wanderMaxDistance = 8f;      // max distance from center
    public float wanderRepositionInterval = 3.5f; // how often to pick a new wander point if necessary

    [Tooltip("If player is within this distance, bias the wander to cut the player's path")]
    [Range(0f, 1f)] public float obstructBias = 0.7f;

    [Tooltip("How long the boss will keep moving before pausing (random between min/max)")]
    public float wanderMoveDurationMin = 20f;
    public float wanderMoveDurationMax = 40f;

    [Tooltip("How long the boss will pause when it pauses (random between min/max)")]
    public float wanderStopDurationMin = 3f;
    public float wanderStopDurationMax = 7f;

    [Tooltip("If player is farther than this, boss will pick targets near player to close the distance")]
    public float playerFarDistance = 30f;

    [Tooltip("When closing, pick a point around the player at a distance between these values (so boss doesn't go exactly on player)")]
    public float stalkingCloseMin = 6f;
    public float stalkingCloseMax = 12f;


    //Chase

    [Header("Chase State Settings")]
    public float chaseSpeed = 3.5f;


    //Search
    [Header("Search State Settings")]
    public float searchDuration = 5f;

    [Tooltip("How long to pause at the last known location before actively searching around it")]
    public float investigationPause = 1f;

    [Tooltip("Radius to search around the last known position")]
    public float searchRadius = 5f;
    [HideInInspector] public Vector3 lastKnownPlayerPosition;


    //Attack
    [Header("Attack State Settings")]
    public float attackDamage = 10f;
    public float attackRange = 1.5f;
    public bool showGizmo = false;
    public Vector3 attackOffset = Vector3.zero;
    public float attackRadius = 0.5f;
    public LayerMask hitLayers;

    [Range(0f, 1f)]
    public float dashChance = 0.2f;
    public float dashSpeedMultiplier = 2f;
    public float rotationSpeed = 10f;           // how fast boss turns to face player during attack
    [HideInInspector] public bool isDashing = false;
    [HideInInspector] public bool playerWasInSight = false;
    [HideInInspector] public bool lockStateTransition = false;
    [HideInInspector] public bool queuedDash = false;


    //Stun
    [Header("Stun State Settings")]
    public float stunDuration = 3.0f;



    //Animation Strings 
    [HideInInspector] public string slash_1 = "Slash1";
    [HideInInspector] public string slash_2 = "Slash2";
    [HideInInspector] public string slashBoth = "SlashBoth";
    [HideInInspector] public string dashSlash = "DashingSlash";
    [HideInInspector] public string dashing = "Dashing";
    [HideInInspector] public string afterSlash = "AfterSlash";
    [HideInInspector] public string stunned = "Stunned";
    [HideInInspector] public string lookAround = "IdleLookAround";
    [HideInInspector] public string lifting = "Lifting";
    [HideInInspector] public string liftingIdle = "LiftingIdle";
    [HideInInspector] public string hit = "Hit";



    private void Awake()
    {
        health = GetComponent<BossHealth>();
        anim = GetComponentInChildren<AIAnimationController>();
        sensor = GetComponent<BossSensor>();
        agent = GetComponent<NavMeshAgent>();

        wanderState = new BossWanderState(this);
        chaseState = new BossChaseState(this);
        searchState = new BossSearchState(this);
        attackState = new BossAttackState(this);
        stunState = new BossStunState(this);

        IState initial = startingState switch
        {
            BossStartState.Wander => wanderState,
            BossStartState.Chase => chaseState,
            BossStartState.Attack => attackState,
            BossStartState.Search => searchState,
            BossStartState.Stun => stunState,
            _ => wanderState
        };

        stateMachine = new BossStateMachine(this, initial);
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

    public void SetActiveState(bool isActive)
    {
        gameObject.SetActive(isActive);
        hasBeenActivated = true;
    }

    private void Update()
    {
        stateMachine?.Update();
        currentStateName = stateMachine?.CurrentState != null
            ? stateMachine.CurrentState.GetType().Name
            : "None";
    }

    public void OnPlayerDetected(Transform target)
    {
        if (lockStateTransition)
            return;

        player = target;
        if (player != null) lastKnownPlayerPosition = player.position;
        stateMachine.ChangeState(chaseState);
    }

    public void OnPlayerLost()
    {
        if (lockStateTransition)
            return;

        if (player != null) lastKnownPlayerPosition = player.position;
        stateMachine.ChangeState(searchState);
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
        var cur = stateMachine?.CurrentState;
        if (cur == null) return;

        try { cur.Exit(); } catch (Exception ex) { Debug.LogWarning($"[BossAI] Reenter Exit exception: {ex}"); }
        try { cur.Enter(); } catch (Exception ex) { Debug.LogWarning($"[BossAI] Reenter Enter exception: {ex}"); }
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

    void OnDrawGizmosSelected()
    {
        if (!showGizmo) return;
        Vector3 origin = transform.position + attackOffset;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(origin, attackRadius);
    }


    public void StopAllStateSounds()
    {
        //Future boss audio stopper
    }
}
