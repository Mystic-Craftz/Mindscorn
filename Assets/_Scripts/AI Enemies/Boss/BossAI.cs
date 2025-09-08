using System;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(BossSensor))]
[RequireComponent(typeof(AIAnimationController))]
[RequireComponent(typeof(NavMeshAgent))]
public class BossAI : MonoBehaviour
{
    public enum BossStartState
    {
        Wander,
        Chase,
        Attack,
        Search
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
    [HideInInspector] public BossSensor sensor;
    [HideInInspector] public NavMeshAgent agent;

    // States (construct these from their classes)
    [HideInInspector] public BossWanderState wanderState;
    [HideInInspector] public BossChaseState chaseState;
    [HideInInspector] public BossSearchState searchState;
    [HideInInspector] public BossAttackState attackState;

    // Wander State Settings 
    [Header("Wander State Settings")]
    public float wanderSpeed = 2f;
    public float wanderMinDistance = 3f;      // min distance from center (player/last known)
    public float wanderMaxDistance = 8f;      // max distance from center
    public float wanderRepositionInterval = 3.5f; // how often to pick a new wander point 

    [Tooltip("If player is within this distance, bias the wander to cut the player's path")]
    [Range(0f, 1f)] public float obstructBias = 0.7f;


    //Teleport 
    [Header("Teleportation")]

    [Tooltip("If boss is farther than this from the player, teleport chance is checked.")]
    public float teleportDistanceThreshold = 35f;
    public float teleportRadius = 5f;         // radius around player to teleport into
    [Range(0f, 1f)] public float teleportChance = 0.6f;
    public float teleportCooldown = 10f;
    [HideInInspector] public float lastTeleportTime = -999f;


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
    public float attackRange = 2f;


    //Animation Strings 


    private void Awake()
    {
        anim = GetComponent<AIAnimationController>();
        sensor = GetComponent<BossSensor>();
        agent = GetComponent<NavMeshAgent>();

        wanderState = new BossWanderState(this);
        chaseState = new BossChaseState(this);
        searchState = new BossSearchState(this);
        attackState = new BossAttackState(this);

        IState initial = startingState switch
        {
            BossStartState.Wander => wanderState,
            BossStartState.Chase => chaseState,
            BossStartState.Attack => attackState,
            BossStartState.Search => searchState,
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
        player = target;
        if (player != null) lastKnownPlayerPosition = player.position;
        stateMachine.ChangeState(chaseState);
    }

    public void OnPlayerLost()
    {
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



    // Teleport methods  
    public bool TryTeleportNear(Vector3 center, float radius)
    {
        if (agent == null || !agent.isOnNavMesh)
            return false;

        const int attempts = 6;
        for (int i = 0; i < attempts; i++)
        {
            Vector3 randomOffset = UnityEngine.Random.insideUnitSphere * radius;
            randomOffset.y = 0f;
            Vector3 tryPos = center + randomOffset;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(tryPos, out hit, 2.0f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
                lastTeleportTime = Time.time;
                agent.ResetPath();
                return true;
            }
        }
        return false;
    }


    public void StopAllStateSounds()
    {
        //Future boss audio stopper
    }
}
