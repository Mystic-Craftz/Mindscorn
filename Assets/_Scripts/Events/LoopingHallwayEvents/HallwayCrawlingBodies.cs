using System.Collections;
using UnityEngine;
using FMOD.Studio;
using FMODUnity;
using STOP_MODE = FMOD.Studio.STOP_MODE;

[RequireComponent(typeof(Animator))]
public class HallwayCrawlingBodies : MonoBehaviour
{
    public GameObject dimensionExitTrigger;

    [Header("Target")]
    public Transform target;
    public string targetTag = "Player";

    [Header("Movement (manual, no root motion)")]
    [Tooltip("Initial movement speed when enabled.")]
    public float initialMoveSpeed = 0.5f;
    [Tooltip("Movement speed after speed-up.")]
    public float boostedMoveSpeed = 1.5f;
    [Tooltip("Runtime current movement speed (used by movement logic).")]
    public float moveSpeed = 0.5f;

    public float rotationSpeed = 3f;
    public float stopDistance = 0.6f;

    [Header("Animation")]
    public Animator animator;
    public string crawlBool = "Crawling";
    [Tooltip("Initial animator.playback speed when enabled.")]
    public float initialAnimSpeed = 0.5f;
    [Tooltip("Animator.playback speed after speed-up.")]
    public float boostedAnimSpeed = 1.5f;

    [Header("Auto speed-up (on enable)")]
    [Tooltip("If true, when this GameObject becomes enabled it will start the delay and auto SpeedUp().")]
    public bool autoSpeedUpAfterTime = true;
    [Tooltip("Seconds to wait after enable before speed-up (set per-object in inspector).")]
    public float delayBeforeSpeedUp = 3f;

    [Header("Optional smoothing")]
    [Tooltip("If true, speed-up will lerp over speedUpDuration instead of being instant.")]
    public bool smoothSpeedUp = false;
    public float speedUpDuration = 0.3f;

    [Header("Dragging Sound")]
    [SerializeField] private EventReference draggingSoundEvent;
    [Tooltip("If true, will continuously restart the sound if it stops (for one-shot events)")]
    [SerializeField] private bool loopSoundIfStops = true;
    private EventInstance draggingSoundInstance;
    private bool isDraggingSoundPlaying = false;
    private Coroutine soundMonitorCoroutine;

    // internal state
    private bool hasSpeedUpTriggered = false;
    private Coroutine autoDelayCoroutine;

    // Ensure animator is found even if object starts disabled
    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();

        // If target not assigned in inspector, try find by tag
        if (target == null)
        {
            var p = GameObject.FindWithTag(targetTag);
            if (p != null) target = p.transform;
        }

        // Initialize dragging sound instance
        if (!draggingSoundEvent.IsNull)
        {
            draggingSoundInstance = RuntimeManager.CreateInstance(draggingSoundEvent);
        }
    }

    private void OnDestroy()
    {
        // Clean up dragging sound instance
        if (draggingSoundInstance.isValid())
        {
            draggingSoundInstance.stop(STOP_MODE.IMMEDIATE);
            draggingSoundInstance.release();
        }

        // Stop sound monitor coroutine
        if (soundMonitorCoroutine != null)
        {
            StopCoroutine(soundMonitorCoroutine);
        }
    }

    // Initialize movement/anim values whenever this object becomes enabled.
    private void OnEnable()
    {
        hasSpeedUpTriggered = false;

        moveSpeed = initialMoveSpeed;
        if (animator != null) animator.speed = initialAnimSpeed;

        if (animator != null) animator.SetBool(crawlBool, true);

        // Start dragging sound when enabled
        StartDraggingSound();

        if (autoSpeedUpAfterTime)
        {
            if (autoDelayCoroutine != null) StopCoroutine(autoDelayCoroutine);
            autoDelayCoroutine = StartCoroutine(AutoDelayCoroutine(delayBeforeSpeedUp));
        }
    }

    private void OnDisable()
    {
        // Stop dragging sound when disabled
        StopDraggingSound();

        // stop coroutines to avoid leaks when disabled
        if (autoDelayCoroutine != null)
        {
            StopCoroutine(autoDelayCoroutine);
            autoDelayCoroutine = null;
        }
    }

    private void Update()
    {
        if (target == null) return;

        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;
        float dist = toTarget.magnitude;

        if (toTarget.sqrMagnitude > 0.0001f)
        {
            Quaternion desired = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desired, rotationSpeed * Time.deltaTime);
        }

        // Update 3D position of dragging sound
        UpdateDraggingSoundPosition();

        if (dist > stopDistance)
        {
            if (animator != null) animator.SetBool(crawlBool, true);

            Vector3 targetXZ = new Vector3(target.position.x, transform.position.y, target.position.z);
            transform.position = Vector3.MoveTowards(transform.position, targetXZ, moveSpeed * Time.deltaTime);
        }
        else
        {
            if (animator != null) animator.SetBool(crawlBool, false);
        }
    }

    // Dragging Sound Methods
    private void StartDraggingSound()
    {
        if (draggingSoundEvent.IsNull || isDraggingSoundPlaying) return;

        try
        {
            if (!draggingSoundInstance.isValid())
            {
                draggingSoundInstance = RuntimeManager.CreateInstance(draggingSoundEvent);
            }

            draggingSoundInstance.start();
            isDraggingSoundPlaying = true;

            // Set initial 3D position
            UpdateDraggingSoundPosition();

            // Start monitoring the sound if it stops
            if (loopSoundIfStops)
            {
                if (soundMonitorCoroutine != null) StopCoroutine(soundMonitorCoroutine);
                soundMonitorCoroutine = StartCoroutine(MonitorDraggingSound());
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to start dragging sound: {e.Message}");
        }
    }

    private void StopDraggingSound()
    {
        if (!isDraggingSoundPlaying) return;

        try
        {
            if (soundMonitorCoroutine != null)
            {
                StopCoroutine(soundMonitorCoroutine);
                soundMonitorCoroutine = null;
            }

            if (draggingSoundInstance.isValid())
            {
                draggingSoundInstance.stop(STOP_MODE.ALLOWFADEOUT);
            }
            isDraggingSoundPlaying = false;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to stop dragging sound: {e.Message}");
        }
    }

    private void UpdateDraggingSoundPosition()
    {
        if (!isDraggingSoundPlaying || !draggingSoundInstance.isValid()) return;

        try
        {
            draggingSoundInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform.position));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to update dragging sound position: {e.Message}");
        }
    }

    private IEnumerator MonitorDraggingSound()
    {
        while (isDraggingSoundPlaying && loopSoundIfStops)
        {
            yield return new WaitForSeconds(0.1f); // Check every 0.1 seconds

            if (!isDraggingSoundPlaying) break;

            // Check if sound has stopped playing
            PLAYBACK_STATE playbackState;
            FMOD.RESULT result = draggingSoundInstance.getPlaybackState(out playbackState);

            if (result == FMOD.RESULT.OK && playbackState == PLAYBACK_STATE.STOPPED)
            {
                // Sound has stopped, restart it
                Debug.Log("Dragging sound stopped, restarting...");
                try
                {
                    draggingSoundInstance.start();
                    UpdateDraggingSoundPosition();
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to restart dragging sound: {e.Message}");
                }
            }
        }
    }

    private IEnumerator AutoDelayCoroutine(float delay)
    {
        // defensive clamp
        delay = Mathf.Max(0f, delay);
        yield return new WaitForSeconds(delay);
        autoDelayCoroutine = null;
        TriggerSpeedUp();
    }

    public void SpeedUp()
    {
        TriggerSpeedUp();
    }

    public void SetSpeedUpDelay(float seconds)
    {
        delayBeforeSpeedUp = Mathf.Max(0f, seconds);

        if (autoSpeedUpAfterTime && gameObject.activeInHierarchy)
        {
            if (autoDelayCoroutine != null) StopCoroutine(autoDelayCoroutine);
            autoDelayCoroutine = StartCoroutine(AutoDelayCoroutine(delayBeforeSpeedUp));
        }
    }

    private void TriggerSpeedUp()
    {
        if (hasSpeedUpTriggered) return;
        hasSpeedUpTriggered = true;

        if (autoDelayCoroutine != null)
        {
            StopCoroutine(autoDelayCoroutine);
            autoDelayCoroutine = null;
        }

        if (smoothSpeedUp && speedUpDuration > 0f)
        {
            StartCoroutine(SmoothSpeedUpCoroutine(speedUpDuration));
        }
        else
        {
            moveSpeed = boostedMoveSpeed;
            if (animator != null) animator.speed = boostedAnimSpeed;
        }
    }

    private IEnumerator SmoothSpeedUpCoroutine(float duration)
    {
        float t = 0f;
        float startMove = moveSpeed;
        float startAnim = animator != null ? animator.speed : 1f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Clamp01(t / duration);
            moveSpeed = Mathf.Lerp(startMove, boostedMoveSpeed, alpha);
            if (animator != null) animator.speed = Mathf.Lerp(startAnim, boostedAnimSpeed, alpha);
            yield return null;
        }

        moveSpeed = boostedMoveSpeed;
        if (animator != null) animator.speed = boostedAnimSpeed;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, stopDistance);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null) return;
        if (other.CompareTag(targetTag))
        {
            EnableDimensionExit();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null || collision.collider == null) return;
        if (collision.collider.CompareTag(targetTag))
        {
            EnableDimensionExit();
        }
    }

    private void EnableDimensionExit()
    {
        if (!dimensionExitTrigger.activeSelf)
        {
            dimensionExitTrigger.SetActive(true);
        }
    }

    // Public methods to control dragging sound externally if needed
    public bool IsDraggingSoundPlaying() => isDraggingSoundPlaying;
}