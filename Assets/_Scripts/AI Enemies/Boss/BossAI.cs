using System;
using System.Collections;
using FMODUnity;
using FMOD.Studio;
using Unity.Cinemachine;
using UnityEngine;
using STOP_MODE = FMOD.Studio.STOP_MODE;
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

    [HideInInspector] public BossWanderState wanderState;
    [HideInInspector] public BossChaseState chaseState;
    [HideInInspector] public BossSearchState searchState;
    [HideInInspector] public BossAttackState attackState;
    [HideInInspector] public BossStunState stunState;

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

    [Header("Chase State Settings")]
    public float chaseSpeed = 3.5f;
    public float chaseStoppingDistance = 1f;
    [HideInInspector] public bool isPreparingDash = false;
    [HideInInspector] public float dashPreparationTime = 1.5f;
    [HideInInspector] public bool isInDashMode = false;

    [Header("Search State Settings")]
    public float searchDuration = 5f;
    public float investigationPause = 1f;
    public float searchRadius = 5f;
    [HideInInspector] public Vector3 lastKnownPlayerPosition;

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
    [HideInInspector] public bool nextAttackIsDash = false;

    [Header("Stun State Settings")]
    public float stunDuration = 3.0f;

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

    [Header("FMOD Stuff")]
    public EventReference attackSound;
    public EventReference hitSound;
    public EventReference breathSound;
    public EventReference laughSound;
    public EventReference singingSound;
    public EventReference angrySound;
    public EventReference CloseToPlayerSound;

    [Header("Close To Player Sound Settings")]
    [Tooltip("If checked, the close to player sound will play continuously (except when in menus or object is disabled)")]
    public bool playCloseToPlayerSound = true;

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

    [Tooltip("Play breathing during stun (one-shots)")]
    public bool playBreathInStun = true;

    // Sound instance management
    private EventInstance singingInstance;
    private EventInstance closeSoundInstance;
    private bool closeSoundPlaying = false;

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

        // Prepare sound instances
        closeSoundInstance = new EventInstance();
        if (!singingSound.IsNull)
        {
            singingInstance = RuntimeManager.CreateInstance(singingSound);
            RuntimeManager.AttachInstanceToGameObject(singingInstance, gameObject);
        }
    }

    void Start()
    {
        AIManager.Register(this);

        if (disableAfterRegistration && gameObject.activeSelf && !hasBeenActivated)
        {
            gameObject.SetActive(false);
        }

        if (playCloseToPlayerSound && !CloseToPlayerSound.IsNull)
        {
            InitializeCloseSound();
        }
    }

    void OnDestroy()
    {
        AIManager.Unregister(this);
        StopCloseToPlayerLoop();

        // Properly release FMOD instances
        if (closeSoundInstance.isValid())
        {
            closeSoundInstance.stop(STOP_MODE.IMMEDIATE);
            closeSoundInstance.release();
        }
        if (singingInstance.isValid())
        {
            singingInstance.stop(STOP_MODE.IMMEDIATE);
            singingInstance.release();
        }
    }

    private void Update()
    {
        stateMachine?.Update();
        currentStateName = stateMachine?.CurrentState != null
            ? stateMachine.CurrentState.GetType().Name
            : "None";

        UpdateMenuMuting();
    }

    private void UpdateMenuMuting()
    {
        if (!playCloseToPlayerSound || AudioManager.Instance == null) return;

        bool menusOpen = AudioManager.Instance.AreAIVoicesMuted();

        if (menusOpen && closeSoundPlaying)
        {
            PauseCloseToPlayerLoop(true);
        }
        else if (!menusOpen && closeSoundPlaying)
        {
            PauseCloseToPlayerLoop(false);
        }
    }

    private void InitializeCloseSound()
    {
        if (CloseToPlayerSound.IsNull) return;

        try
        {
            closeSoundInstance = RuntimeManager.CreateInstance(CloseToPlayerSound);
            RuntimeManager.AttachInstanceToGameObject(closeSoundInstance, gameObject);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BossAI] Failed to initialize close sound: {ex.Message}");
        }
    }

    public void StartCloseToPlayerLoop()
    {
        if (!playCloseToPlayerSound || CloseToPlayerSound.IsNull || closeSoundPlaying) return;

        try
        {
            if (!closeSoundInstance.isValid())
            {
                InitializeCloseSound();
            }

            closeSoundInstance.start();
            closeSoundPlaying = true;

            if (AudioManager.Instance != null && !AudioManager.Instance.AreAIVoicesMuted())
            {
                closeSoundInstance.setPaused(false);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BossAI] Failed to start close sound: {ex.Message}");
            closeSoundPlaying = false;
        }
    }

    public void StopCloseToPlayerLoop()
    {
        if (!closeSoundPlaying) return;

        try
        {
            if (closeSoundInstance.isValid())
            {
                closeSoundInstance.stop(STOP_MODE.ALLOWFADEOUT);
            }
            closeSoundPlaying = false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BossAI] Failed to stop close sound: {ex.Message}");
        }
    }

    private void PauseCloseToPlayerLoop(bool pause)
    {
        if (!closeSoundPlaying || !closeSoundInstance.isValid()) return;

        try
        {
            closeSoundInstance.setPaused(pause);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BossAI] Failed to pause/unpause close sound: {ex.Message}");
        }
    }

    void OnEnable()
    {
        ReenterCurrentState();
        laughPlayedThisEngagement = false;

        if (playCloseToPlayerSound)
        {
            StartCloseToPlayerLoop();
        }
    }

    void OnDisable()
    {
        StopAllStateSounds();
        StopCloseToPlayerLoop();
    }

    public void SetActiveState(bool isActive)
    {
        gameObject.SetActive(isActive);
        hasBeenActivated = true;
    }

    private void ReenterCurrentState()
    {
        var cur = stateMachine?.CurrentState;
        if (cur == null) return;

        try { cur.Exit(); } catch (Exception ex) { Debug.LogWarning($"[BossAI] Reenter Exit exception: {ex}"); }
        try { cur.Enter(); } catch (Exception ex) { Debug.LogWarning($"[BossAI] Reenter Enter exception: {ex}"); }
    }

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
        if (singingInstance.isValid())
        {
            singingInstance.stop(STOP_MODE.IMMEDIATE);
        }
        StopCloseToPlayerLoop();
    }
    
    public void TryPlaySingingOnce()
    {
        if (singingPlayedThisChase || singingSound.IsNull || !singingInstance.isValid()) return;

        singingInstance.getPlaybackState(out var state);
        if (state == PLAYBACK_STATE.PLAYING || state == PLAYBACK_STATE.STARTING)
        {
            return; // Already playing
        }

        singingInstance.start();
        singingPlayedThisChase = true;
    }

    public void StopSinging()
    {
        if (singingInstance.isValid())
        {
            singingInstance.stop(STOP_MODE.ALLOWFADEOUT);
        }
    }

    public void TryPlayOneShot3D(EventReference ev)
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

    public void TryPlayLaughOnce()
    {
        if (!playLaughOnDetect || laughPlayedThisEngagement) return;

        TryPlayOneShot3D(laughSound);
        laughPlayedThisEngagement = true;
    }

    public IEnumerator PlayEventAndWait(EventReference ev)
    {
        if (ev.IsNull) yield break;

        var instance = RuntimeManager.CreateInstance(ev);
        RuntimeManager.AttachInstanceToGameObject(instance, gameObject);

        var startResult = instance.start();
        if (startResult != FMOD.RESULT.OK)
        {
            instance.release();
            yield break;
        }

        while (true)
        {
            instance.getPlaybackState(out var state);

            if (state == PLAYBACK_STATE.STARTING || state == PLAYBACK_STATE.PLAYING)
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

    public void TriggerDashForNextAttack()
    {
        nextAttackIsDash = true;
        queuedDash = true;
        isDashing = true;
    }

    public void ResetDashFlags()
    {
        nextAttackIsDash = false;
        queuedDash = false;
        isDashing = false;
        isPreparingDash = false;
        isInDashMode = false;
    }

    public void OnPlayerDetected(Transform target)
    {
        if (lockStateTransition) return;

        player = target;
        if (player != null) lastKnownPlayerPosition = player.position;

        TryPlayLaughOnce();

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

    public void SetCloseToPlayerSoundEnabled(bool enabled)
    {
        if (playCloseToPlayerSound == enabled) return;

        playCloseToPlayerSound = enabled;

        if (enabled)
        {
            StartCloseToPlayerLoop();
        }
        else
        {
            StopCloseToPlayerLoop();
        }
    }
}