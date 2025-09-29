using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

public class ThrowableLimb : MonoBehaviour
{
    [SerializeField] private float throwForce = 10f;
    [SerializeField] private float downMP = 0f;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Transform director;
    [SerializeField] private ParticleSystem bloodParticle;
    [SerializeField] private LayerMask hitLayerMask;
    public string animationString;
    [SerializeField] private bool debug = false;

    private bool hasDamaged = false;
    private bool isThrown = false;
    private CapsuleCollider collider;

    private Action OnPlayerDamaged;

    private void Start()
    {
        collider = rb.gameObject.GetComponent<CapsuleCollider>();
        rb.isKinematic = true;
    }

    private void Update()
    {
        if (isThrown)
        {
            transform.Translate(transform.forward * Time.deltaTime * throwForce, Space.World);
        }
    }

    public void ThrowLimb(Transform target, Action OnPlayerDamaged)
    {
        // rb.isKinematic = false;
        // rb.AddForce(target.position - director.position + Vector3.down * downMP * throwForce, ForceMode.Impulse);
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        hasDamaged = false;
        isThrown = true;
        transform.forward = target.position - transform.position;
        // transform.DORotate(lookRotation.eulerAngles, 0.2f);
        this.OnPlayerDamaged = OnPlayerDamaged;
        gameObject.layer = LayerMask.NameToLayer("Monster");
        StartCoroutine(DisableRigidbodyAfterDelay());
    }

    private IEnumerator DisableRigidbodyAfterDelay()
    {
        yield return new WaitForSeconds(7.5f);
        rb.isKinematic = true;
        rb.gameObject.GetComponent<Collider>().enabled = false;
        bloodParticle.Stop();
    }

    public void Damage(RaycastHit hit, Transform origin)
    {
        if (!isThrown) return;
        rb.isKinematic = false;
        isThrown = false;
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(-(hit.point - director.position) * throwForce, ForceMode.Impulse);
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Collided with " + collision.gameObject.name);
        if (collision.gameObject.CompareTag("Player") && !hasDamaged)
        {
            // Debug.Log("Player hit by throwable limb!");
            PlayerHealth.Instance.TakeDamage(10);
            hasDamaged = true;
            OnPlayerDamaged?.Invoke();
        }
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = false;
    }
}
