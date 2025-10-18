using System;
using System.Collections;
using FMODUnity;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(BossSensor))]
[RequireComponent(typeof(NavMeshAgent))]
public class BossAI : MonoBehaviour
{
    public enum BossStartState { Wander, Chase, Attack, Search, Stun }

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

    // States
    [HideInInspector] public BossWanderState wanderState;
    [HideInInspector] public BossChaseState chaseState;
    [HideInInspector] public BossSearchState searchState;
    [HideInInspector] public BossAttackState attackState;
    [HideInInspector] public BossStunState stunState;

    // Wander Settings
    [Header("Wander State Settings")]
    public float wanderSpeed = 2f;
    public float wanderMinDistance = 3f;
    public float wanderMaxDistance = 8f;
    public float wanderRepositionInterval = 3.5f;
    [Range(0f, 1f)] public float obstructBias = 0.7f;
    public float wanderMoveDurationMin = 20f;
    public float wanderMoveDurationMax = 40f;
    public float wanderStopDurationMin = 3f;
    public float wanderStopDurationMax = 7f;
    public float playerFarDistance = 30f;
    public float stalkingCloseMin = 6f;
    public float stalkingCloseMax = 12f;

    // Chase
    [Header("Chase State Settings")]
    public float chaseSpeed = 3.5f;
    [HideInInspector] public bool isPreparingDash = false;
    [HideInInspector] public float dashPreparationTime = 1.5f;
    [HideInInspector] public bool isInDashMode = false;

    // Search
    [Header("Search State Settings")]
    public float searchDuration = 5f;
    public float investigationPause = 1f;
    public float searchRadius = 5f;
    [HideInInspector] public Vector3 lastKnownPlayerPosition;

    // Attack
    [Header("Attack State Settings")]
    public float attackDamage = 10f;
    public float attackRange = 1.5f;
    public bool showGizmo = false;
    public Vector3 attackOffset = Vector3.zero;
    public float attackRadius = 0.5f;
    public LayerMask hitLayers;
    [Range(0f, 1f)] public float dashChance = 0.2f;
    public float dashSpeedMultiplier = 2f;
    public float rotationSpeed = 10f;
    [HideInInspector] public bool isDashing = false;
    [HideInInspector] public bool playerWasInSight = false;
    [HideInInspector] public bool lockStateTransition = false;
    [HideInInspector] public bool queuedDash = false;

    // Stun
    [Header("Stun State Settings")]
    public float stunDuration = 3.0f;

    // Animation names
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
    [HideInInspector] public string prepareForDash = "PrepareToDash";

    // FMOD
    [Header("FMOD Stuff")]
    public EventReference attackSound;
    public EventReference hitSound;
    public EventReference breathSound;
    public EventReference laughSound;
    public EventReference singingSound;
    public EventReference angrySound;

    [Header("Audio Options")]
    private bool playLaughOnDetect = true;
    private bool playAngryOnLost = true;
    private bool laughPlayedThisEngagement = false;

    [HideInInspector] public bool singingPlayedThisChase = false;


    [Tooltip("Play occasional breathing one-shots while in Wander state")]
    public bool playBreathInWander = true;

    [Tooltip("Minimum seconds between breath one-shots (if equal to max, interval is fixed)")]
    public float breathIntervalMin = 6f;

    [Tooltip("Maximum seconds between breath one-shots")]
    public float breathIntervalMax = 12f;

    // new: optionally enable breath during stun separately (defaults true)
    [Tooltip("Play breathing during stun (one-shots)")]
    public bool playBreathInStun = true;

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
        if (lockStateTransition) return;

        player = target;
        if (player != null) lastKnownPlayerPosition = player.position;

        if (playLaughOnDetect && !laughPlayedThisEngagement)
        {
            TryPlayOneShot3D(laughSound);
            laughPlayedThisEngagement = true;
        }

        stateMachine.ChangeState(chaseState);
    }

    public void OnPlayerLost()
    {
        if (lockStateTransition) return;

        if (player != null) lastKnownPlayerPosition = player.position;

        if (playAngryOnLost && laughPlayedThisEngagement)
        {
            TryPlayOneShot3D(angrySound);
            laughPlayedThisEngagement = false;
        }

        stateMachine.ChangeState(searchState);
    }

    void OnEnable()
    {
        ReenterCurrentState();
        laughPlayedThisEngagement = false;
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

    // this method is called in animation event
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
        // stop all state sounds
    }


    public void TryPlayOneShot3D(FMODUnity.EventReference ev)
    {
        if (ev.IsNull)
        {
            Debug.LogWarning($"[BossAI] TryPlayOneShot3D called with null EventReference on {name}");
            return;
        }

        try
        {
            RuntimeManager.PlayOneShotAttached(ev, gameObject);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[BossAI] Failed to play FMOD event: {ex.Message}");
        }
    }


    // Plays an FMOD event attached to this GameObject and waits until it finishes.
    public IEnumerator PlayEventAndWait(FMODUnity.EventReference ev)
    {
        if (ev.IsNull)
            yield break;

        var instance = FMODUnity.RuntimeManager.CreateInstance(ev);
        FMODUnity.RuntimeManager.AttachInstanceToGameObject(instance, gameObject);

        // Start event
        var startResult = instance.start();
        if (startResult != FMOD.RESULT.OK)
        {
            instance.release();
            yield break;
        }

        // Wait until finished
        while (true)
        {
            instance.getPlaybackState(out var state);

            if (state == FMOD.Studio.PLAYBACK_STATE.STARTING || state == FMOD.Studio.PLAYBACK_STATE.PLAYING)
            {
                yield return null;
            }
            else
            {
                break;
            }
        }

        instance.release();
    }

}
