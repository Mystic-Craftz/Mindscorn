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
    [HideInInspector] public BossDeathState dieState;

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
    public float rotationSpeed = 10f;
    [HideInInspector] public bool playerWasInSight = false;
    [HideInInspector] public bool lockStateTransition = false;

    [Header("Special Attack Settings")]
    [Range(0f, 1f)]
    public float specialAttackChancePerSecond = 0.05f;
    public float specialAttackDamage = 20f;
    public int specialGlitchIntensity = 3;
    public float liftingIdleDuration = 1.5f;

    [Header("Special Attack Cooldowns")]
    [Tooltip("Cooldown (seconds) applied after a roll triggers while in Chase (pre-special cooldown).")]
    public float specialRollCooldown = 5f; // time between rolls while chasing

    [Tooltip("Cooldown (seconds) applied AFTER a special attack completes (post-special cooldown).")]
    public float specialPostAttackCooldown = 8f; // after special completes

    // runtime
    [HideInInspector] public bool pendingSpecialAttack = false;
    [HideInInspector] public bool canRollForSpecial = true;
    [HideInInspector] public float specialRollCooldownTimer = 0f;

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
    [HideInInspector] public string die = "Die";

    [Header("FMOD Stuff")]
    public EventReference attackSound;
    public EventReference hitSound;
    public EventReference breathSound;
    public EventReference laughSound;
    public EventReference singingSound;
    public EventReference angrySound;
    public EventReference CloseToPlayerSound;
    public EventReference itBeginsSound;

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

    // --- NEW: small guard so we don't attempt start if component is disabled/inactive ---
    private bool IsAllowedToPlayCloseLoop()
    {
        return playCloseToPlayerSound && gameObject.activeInHierarchy && this.enabled;
    }

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
        dieState = new BossDeathState(this);

        // use helper to select initial state (behavior unchanged)
        IState initial = GetInitialState();

        stateMachine = new BossStateMachine(this, initial);

        if (!singingSound.IsNull)
        {
            try
            {
                singingInstance = RuntimeManager.CreateInstance(singingSound);
                RuntimeManager.AttachInstanceToGameObject(singingInstance, gameObject);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[BossAI] Failed to create singing instance: {ex}");
                singingInstance = new EventInstance();
            }
        }

        closeSoundInstance = new EventInstance();
    }

    void Start()
    {
        AIManager.Register(this);

        if (disableAfterRegistration && gameObject.activeSelf && !hasBeenActivated)
        {
            gameObject.SetActive(false);
        }

        // Initialize + start close loop if allowed and menus aren't muting
        if (playCloseToPlayerSound && !CloseToPlayerSound.IsNull)
        {
            InitializeCloseSound();

            if (AudioManager.Instance == null || !AudioManager.Instance.AreAIVoicesMuted())
            {
                StartCloseToPlayerLoop();
            }
            // otherwise wait for UpdateMenuMuting to unpause when menus close
        }
    }

    void OnDestroy()
    {
        AIManager.Unregister(this);

        // Stop and release close sound
        try
        {
            if (closeSoundInstance.isValid())
            {
                closeSoundInstance.stop(STOP_MODE.IMMEDIATE);
                closeSoundInstance.release();
            }
        }
        catch { }

        // Stop and release singing instance
        try
        {
            if (singingInstance.isValid())
            {
                singingInstance.stop(STOP_MODE.IMMEDIATE);
                singingInstance.release();
            }
        }
        catch { }
    }

    private void Update()
    {
        stateMachine?.Update();
        currentStateName = stateMachine?.CurrentState != null
            ? stateMachine.CurrentState.GetType().Name
            : "None";

        UpdateMenuMuting();

        // If the GameObject was somehow disabled without OnDisable being processed (rare),
        // ensure the close sound is stopped. This is defensive.
        if (!gameObject.activeInHierarchy && closeSoundPlaying)
        {
            StopCloseToPlayerLoop(release: true);
        }

        // special attack cooldown
        if (!canRollForSpecial)
        {
            specialRollCooldownTimer -= Time.deltaTime;
            if (specialRollCooldownTimer <= 0f)
            {
                canRollForSpecial = true;
                specialRollCooldownTimer = 0f;
            }
        }
    }

    //  use pause/unpause for menus so resuming is instant 
    private void UpdateMenuMuting()
    {
        if (!playCloseToPlayerSound || AudioManager.Instance == null) return;

        bool menusOpen = AudioManager.Instance.AreAIVoicesMuted();

        if (menusOpen)
        {
            // pause the loop for instant resume later
            if (closeSoundInstance.isValid() && closeSoundPlaying)
            {
                PauseCloseToPlayerLoop(true);
            }
        }
        else
        {
            // unpause / start the loop quickly when menus close
            if (closeSoundInstance.isValid())
            {
                PauseCloseToPlayerLoop(false);
                if (!closeSoundPlaying)
                {
                    // If it was never started before, start it
                    StartCloseToPlayerLoop();
                }
            }
            else
            {
                if (!closeSoundPlaying && !CloseToPlayerSound.IsNull && playCloseToPlayerSound)
                {
                    InitializeCloseSound();
                    StartCloseToPlayerLoop();
                }
            }
        }
    }

    private void InitializeCloseSound()
    {
        if (CloseToPlayerSound.IsNull) return;

        try
        {
            if (!closeSoundInstance.isValid())
            {
                closeSoundInstance = RuntimeManager.CreateInstance(CloseToPlayerSound);
                RuntimeManager.AttachInstanceToGameObject(closeSoundInstance, gameObject);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BossAI] Failed to initialize close sound: {ex.Message}");
        }
    }

    // --- MODIFIED: refuse to start loop if component or GameObject is disabled (defensive) ---
    public void StartCloseToPlayerLoop()
    {
        if (!IsAllowedToPlayCloseLoop() || CloseToPlayerSound.IsNull || closeSoundPlaying) return;

        try
        {
            if (!closeSoundInstance.isValid())
            {
                InitializeCloseSound();
            }

            // If AudioManager is muting AI, keep paused and don't actually start audible playback
            if (AudioManager.Instance != null && AudioManager.Instance.AreAIVoicesMuted())
            {
                try { closeSoundInstance.setPaused(true); } catch { }
                closeSoundPlaying = false;
                return;
            }

            // Start (or resume) the instance and unpause
            var startResult = closeSoundInstance.start();
            try { closeSoundInstance.setPaused(false); } catch { }
            closeSoundPlaying = true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BossAI] Failed to start close sound: {ex.Message}");
            closeSoundPlaying = false;
        }
    }

    // Pause/resume
    public void PauseCloseToPlayerLoop(bool pause)
    {
        if (!closeSoundInstance.isValid()) return;

        try
        {
            closeSoundInstance.setPaused(pause);
            if (!pause && closeSoundInstance.isValid())
            {
                // ensure flag is true when unpausing so other logic knows it's active
                closeSoundPlaying = true;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[BossAI] Failed to {(pause ? "pause" : "unpause")} close sound: {ex}");
        }
    }

    // --- MODIFIED: make Stop logic defensive and ensure an instance exists to stop/release ---
    public void StopCloseToPlayerLoop(bool release = false)
    {
        // if nothing to do, exit early
        if (!closeSoundInstance.isValid() && !closeSoundPlaying) return;

        try
        {
            // If we don't have a valid instance handle but the flag indicates playing,
            // try to create a temporary instance and immediately stop+release it so no orphaned audio continues.
            if (!closeSoundInstance.isValid())
            {
                InitializeCloseSound();
            }

            if (closeSoundInstance.isValid())
            {
                closeSoundInstance.stop(release ? STOP_MODE.IMMEDIATE : STOP_MODE.ALLOWFADEOUT);

                if (release)
                {
                    closeSoundInstance.release();
                    closeSoundInstance = new EventInstance();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[BossAI] Failed to stop close sound: {ex}");
        }

        closeSoundPlaying = false;
    }

    // --------------------- NEW/CHANGED SECTION ---------------------

    // Helper: return the IState instance that corresponds to startingState
    private IState GetInitialState()
    {
        return startingState switch
        {
            BossStartState.Wander => wanderState,
            BossStartState.Chase => chaseState,
            BossStartState.Attack => attackState,
            BossStartState.Search => searchState,
            BossStartState.Stun => stunState,
            _ => wanderState
        };
    }


    public void ResetToStartingState()
    {

        try { StopAllCoroutines(); } catch { }

        StopAllStateSounds();

        try { anim?.ForceUnlock(); } catch { }

        pendingSpecialAttack = false;
        canRollForSpecial = true;
        specialRollCooldownTimer = 0f;
        singingPlayedThisChase = false;
        laughPlayedThisEngagement = false;
        lockStateTransition = false;

        player = null;
        lastKnownPlayerPosition = Vector3.zero;

        if (agent != null)
        {
            try
            {
                agent.ResetPath();
                agent.isStopped = true;
            }
            catch { }
        }

        var startState = GetInitialState();
        if (stateMachine != null && stateMachine.CurrentState != startState)
        {

            bool previousLock = lockStateTransition;
            lockStateTransition = false;
            stateMachine.ChangeState(startState, force: true);
            lockStateTransition = previousLock;
        }
    }


    void OnEnable()
    {
        ResetToStartingState();
        laughPlayedThisEngagement = false;

        if (playCloseToPlayerSound && !CloseToPlayerSound.IsNull)
        {
            InitializeCloseSound();
            if (AudioManager.Instance == null || !AudioManager.Instance.AreAIVoicesMuted())
            {
                StartCloseToPlayerLoop();
            }
            else
            {
                PauseCloseToPlayerLoop(true);
            }
        }
    }


    void OnDisable()
    {
        // Pause first for immediate silence, then stop+release to avoid orphaned FMOD instances.
        try
        {
            PauseCloseToPlayerLoop(true);
        }
        catch { }

        try
        {
            if (!closeSoundInstance.isValid() && !CloseToPlayerSound.IsNull)
            {
                InitializeCloseSound();
            }
        }
        catch { }

        StopCloseToPlayerLoop(release: true);

        // Make sure we wipe runtime state so the boss will always restart clean
        ResetToStartingState();

        StopAllStateSounds();
    }

    public void SetActiveState(bool isActive)
    {
        // Keep previous behavior (this triggers OnEnable/OnDisable which handle reset)
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
        try
        {
            if (singingInstance.isValid())
            {
                singingInstance.stop(STOP_MODE.IMMEDIATE);
                singingInstance.release();
                singingInstance = new EventInstance();
            }
        }
        catch { }

        // Ensure close loop is released too
        StopCloseToPlayerLoop(release: true);
    }

    public void TryPlaySingingOnce()
    {
        if (singingPlayedThisChase || singingSound.IsNull) return;
        if (!singingInstance.isValid())
        {
            try
            {
                singingInstance = RuntimeManager.CreateInstance(singingSound);
                RuntimeManager.AttachInstanceToGameObject(singingInstance, gameObject);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[BossAI] TryPlaySingingOnce: failed to create singing instance: {ex}");
                return;
            }
        }

        singingInstance.getPlaybackState(out var state);
        if (state == PLAYBACK_STATE.PLAYING || state == PLAYBACK_STATE.STARTING)
        {
            return;
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

    public void TryPlayOneShot2D(EventReference ev)
    {
        if (ev.IsNull)
        {
            Debug.LogWarning($"[BossAI] TryPlayOneShot2D called with null EventReference on {name}");
            return;
        }

        try
        {
            RuntimeManager.PlayOneShot(ev);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[BossAI] Failed to play 2D FMOD event: {ex.Message}");
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
            if (AudioManager.Instance == null || !AudioManager.Instance.AreAIVoicesMuted())
                StartCloseToPlayerLoop();
        }
        else
        {
            StopCloseToPlayerLoop(release: true);
        }
    }
}
