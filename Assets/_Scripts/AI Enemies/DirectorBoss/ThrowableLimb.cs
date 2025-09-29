using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

public class ThrowableLimb : MonoBehaviour
{
    [SerializeField] private float throwForce = 10f;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Transform director;
    [SerializeField] private Transform meshHolder;
    [SerializeField] private ParticleSystem bloodParticle;
    [SerializeField] private LayerMask hitLayerMask;
    public string animationString;
    [SerializeField] private bool debug = false;

    public float throwSpeed = 10f; // units per second
    public float stopDistance = 0.2f; // when close enough to target, consider "arrived"
    public float hitRadius = 0.5f;

    // --- Spin settings ---
    public bool useSpin = true;
    public float spinSpeed = 720f; // degrees per second
    public Vector3 spinAxis = Vector3.up; // used if useRandomSpin=false
    public bool useRandomSpinAxis = true; // pick a random spin axis each throw

    // --- Align settings (alternative to spin) ---
    public bool alignToMotion = false; // if true, will rotate mesh to face motion direction
    public float alignSpeed = 720f; // degrees per second for rotation toward motion

    private Vector3 targetPosition;
    private Vector3 moveDirection;

    private bool hasDamaged = false;
    private bool isThrown = false;
    private CapsuleCollider collider;

    private Action OnPlayerDamaged;
    private Vector3 previousPosition;

    private Vector3 currentSpinAxis = Vector3.up;

    private void Start()
    {
        collider = rb.gameObject.GetComponent<CapsuleCollider>();
        rb.isKinematic = true;
    }

    private void Update()
    {
        if (!isThrown) return;

        Vector3 step = moveDirection * throwSpeed * Time.deltaTime;
        Vector3 nextPos = transform.position + step;

        Collider[] overlaps = Physics.OverlapSphere(transform.position, hitRadius, hitLayerMask, QueryTriggerInteraction.Collide);
        if (overlaps.Length > 0)
        {
            HandleHit(overlaps[0]);
            isThrown = false;
            return;
        }

        transform.position = nextPos;

        if (useSpin && !alignToMotion)
        {
            // constant spin around axis in local space
            meshHolder.Rotate(currentSpinAxis, spinSpeed * Time.deltaTime, Space.Self);
        }
        else if (alignToMotion)
        {
            // orient the mesh to face the movement direction smoothly
            if (moveDirection.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDirection, Vector3.up);
                // rotate the mesh toward the target rotation
                meshHolder.rotation = Quaternion.RotateTowards(
                    meshHolder.rotation,
                    targetRot,
                    alignSpeed * Time.deltaTime
                );
            }
        }

        if (moveDirection.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(moveDirection);

        if (Vector3.Distance(transform.position, targetPosition) <= stopDistance)
        {
            isThrown = false;
            OnArrivedAtTarget();
        }

        previousPosition = transform.position;
    }

    private void HandleHit(Collider other)
    {
        Debug.Log("Throwable limb hit: " + other.gameObject.name);

        if ((other.CompareTag("Player") || other.CompareTag("Player Door Collider")) && !hasDamaged)
        {
            Debug.Log("Player hit by throwable limb!");
            PlayerHealth.Instance.TakeDamage(10);
            OnPlayerDamaged?.Invoke();
            hasDamaged = true;
        }

        transform.eulerAngles = Vector3.zero;
        rb.isKinematic = false;
        collider.enabled = true;
        rb.AddForce(transform.forward * throwForce, ForceMode.VelocityChange);
        collider.isTrigger = false;
        StartCoroutine(DisableRigidbodyAfterDelay());
    }

    private void OnArrivedAtTarget()
    {
        rb.isKinematic = false;
        collider.enabled = true;
        collider.isTrigger = false;
        rb.AddForce(transform.forward * throwForce, ForceMode.VelocityChange);
        transform.eulerAngles = Vector3.zero;
        StartCoroutine(DisableRigidbodyAfterDelay());
    }

    public void ThrowLimb(Transform target, Action OnPlayerDamaged)
    {
        targetPosition = target.position;
        moveDirection = (targetPosition - transform.position).normalized;
        previousPosition = transform.position;
        rb.isKinematic = true;
        hasDamaged = false;
        isThrown = true;
        this.OnPlayerDamaged = OnPlayerDamaged;
        gameObject.layer = LayerMask.NameToLayer("Monster");
    }

    private IEnumerator DisableRigidbodyAfterDelay()
    {
        yield return new WaitForSeconds(3f);
        rb.isKinematic = true;
        rb.gameObject.GetComponent<Collider>().enabled = false;
        bloodParticle.Stop();
    }

    public void Damage(RaycastHit hit, Transform origin)
    {
        if (!isThrown) return;
        rb.isKinematic = false;
        isThrown = false;
        collider.enabled = true;
        rb.linearVelocity = Vector3.zero;
        hasDamaged = true;
        meshHolder.eulerAngles = Vector3.zero;
        rb.AddForce(-(hit.point - director.position) * throwForce / 4, ForceMode.VelocityChange);
        StartCoroutine(DisableRigidbodyAfterDelay());
    }
}
