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

    [Header("Boss Settings")]
    public float wanderSpeed = 2f;
    public float attackRange = 2f;


    [Header("Chase State Settings")]
    public float chaseSpeed = 3.5f;


    [Header("Search State Settings")]
    public float searchDuration = 5f;

    [Tooltip("How long to pause at the last known location before actively searching around it")]
    public float investigationPause = 1f;

    [Tooltip("Radius to search around the last known position")]
    public float searchRadius = 5f;
    [HideInInspector] public Vector3 lastKnownPlayerPosition;


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

    public void StopAllStateSounds()
    {
        //Future boss audio stopper
    }
}
