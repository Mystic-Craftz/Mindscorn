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
    // Both initial speeds set to 0.5 as you requested.
    public float initialMoveSpeed = 0.5f;
    public float boostedMoveSpeed = 1.5f;
    [Tooltip("Current movement speed (updated at runtime).")]
    public float moveSpeed = 0.5f;

    public float rotationSpeed = 3f;
    public float stopDistance = 0.6f;

    [Header("Animation")]
    public Animator animator;
    public string crawlBool = "Crawling";
    // Initial animation playback speed is 0.5
    public float initialAnimSpeed = 0.5f;
    public float boostedAnimSpeed = 1.5f;

    [Header("Auto speed-up (delay control)")]
    [Tooltip("If true, the mannequin will automatically speed up after delayBeforeSpeedUp seconds.")]
    public bool autoSpeedUpAfterTime = true;
    [Tooltip("Seconds to wait before automatically triggering speed-up. You can change this in the inspector or at runtime via SetSpeedUpDelay().")]
    public float delayBeforeSpeedUp = 3f;

    [Header("Optional triggers")]
    public bool speedUpWhenClose = false;
    public float speedUpDistance = 5f;

    [Header("Smoothing (optional)")]
    [Tooltip("If true, speeds will smoothly lerp over speedUpDuration instead of instantly changing.")]
    public bool smoothSpeedUp = false;
    public float speedUpDuration = 0.5f;

    bool hasSpeedUpTriggered = false;
    float startTime;

    void Start()
    {
        if (animator == null) animator = GetComponent<Animator>();

        if (target == null)
        {
            GameObject p = GameObject.FindWithTag(targetTag);
            if (p != null) target = p.transform;
        }

        if (animator != null) animator.applyRootMotion = false;

        // Initialize speeds (both set to 0.5 by default)
        moveSpeed = initialMoveSpeed;
        if (animator != null) animator.speed = initialAnimSpeed;

        if (animator != null && target != null)
        {
            animator.SetBool(crawlBool, true);
        }

        startTime = Time.time;

        if (autoSpeedUpAfterTime)
        {
            // Start automatic delayed speed-up
            StartCoroutine(AutoDelayCoroutine(delayBeforeSpeedUp));
        }
    }

    void Update()
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

        // Optional proximity trigger
        if (!hasSpeedUpTriggered && speedUpWhenClose && dist <= speedUpDistance)
        {
            TriggerSpeedUp();
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


    // Call this to manually trigger the sudden speed-up.
    public void SpeedUp()
    {
        if (!hasSpeedUpTriggered)
            TriggerSpeedUp();
    }


    // Change the auto delay at runtime. If autoSpeedUpAfterTime is enabled and the coroutine is running,
    // this will restart the auto-delay with the new value. 
    public void SetSpeedUpDelay(float seconds)
    {
        delayBeforeSpeedUp = Mathf.Max(0f, seconds);

        if (autoSpeedUpAfterTime && !hasSpeedUpTriggered)
        {
            StopAllCoroutines();
            StartCoroutine(AutoDelayCoroutine(delayBeforeSpeedUp));
        }
    }

    private IEnumerator AutoDelayCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        TriggerSpeedUp();
    }

    private void TriggerSpeedUp()
    {
        if (hasSpeedUpTriggered) return;
        hasSpeedUpTriggered = true;

        StopAllCoroutines();

        if (smoothSpeedUp && speedUpDuration > 0f)
        {
            StartCoroutine(SmoothSpeedUpCoroutine(speedUpDuration));
        }
        else
        {
            // Instant (sudden) change
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

        // Ensure final values
        moveSpeed = boostedMoveSpeed;
        if (animator != null) animator.speed = boostedAnimSpeed;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, stopDistance);

        if (speedUpWhenClose)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, speedUpDistance);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other == null) return;
        if (other.CompareTag(targetTag))
        {
            EnableDimensionExit();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision == null || collision.collider == null) return;
        if (collision.collider.CompareTag(targetTag))
        {
            EnableDimensionExit();
        }
    }

    private void EnableDimensionExit()
    {
        if (dimensionExitTrigger == null) return;
        if (!dimensionExitTrigger.activeSelf)
        {
            dimensionExitTrigger.SetActive(true);
        }
    }
}
