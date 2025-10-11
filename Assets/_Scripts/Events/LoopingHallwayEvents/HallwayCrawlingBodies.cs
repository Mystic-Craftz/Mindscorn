using System.Collections;
using UnityEngine;

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
    }

    // Initialize movement/anim values whenever this object becomes enabled.
    private void OnEnable()
    {
        hasSpeedUpTriggered = false;

        moveSpeed = initialMoveSpeed;
        if (animator != null) animator.speed = initialAnimSpeed;

        if (animator != null) animator.SetBool(crawlBool, true);


        if (autoSpeedUpAfterTime)
        {
            if (autoDelayCoroutine != null) StopCoroutine(autoDelayCoroutine);
            autoDelayCoroutine = StartCoroutine(AutoDelayCoroutine(delayBeforeSpeedUp));
        }
    }

    private void OnDisable()
    {
        // stop coroutines to avoid leaks when disabled
        if (autoDelayCoroutine != null)
        {
            StopCoroutine(autoDelayCoroutine);
            autoDelayCoroutine = null;
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
}
