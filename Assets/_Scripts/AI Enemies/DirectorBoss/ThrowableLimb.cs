using System;
using System.Collections;
using UnityEngine;

public class ThrowableLimb : MonoBehaviour
{
    [SerializeField] private float throwForce = 10f;
    [SerializeField] private float downMP = 0f;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Transform director;
    public string animationString;
    [SerializeField] private bool debug = false;

    private bool hasDamaged = false;
    private bool isThrown = false;

    private Action OnPlayerDamaged;

    public void ThrowLimb(Transform target, Action OnPlayerDamaged)
    {
        rb.isKinematic = false;
        rb.AddForce(target.position - director.position + Vector3.down * downMP * throwForce, ForceMode.Impulse);
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        hasDamaged = false;
        isThrown = true;
        this.OnPlayerDamaged = OnPlayerDamaged;
        gameObject.layer = LayerMask.NameToLayer("Monster");
        StartCoroutine(DisableRigidbodyAfterDelay());
    }

    private IEnumerator DisableRigidbodyAfterDelay()
    {
        yield return new WaitForSeconds(7.5f);
        rb.isKinematic = true;
        rb.gameObject.GetComponent<Collider>().enabled = false;
    }

    public void Damage(RaycastHit hit, Transform origin)
    {
        if (!isThrown) return;
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(-(hit.point - director.position) * throwForce, ForceMode.Impulse);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player") && !hasDamaged)
        {
            Debug.Log("Player hit by throwable limb!");
            PlayerHealth.Instance.TakeDamage(10);
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            hasDamaged = true;
            OnPlayerDamaged?.Invoke();
        }
    }
}
