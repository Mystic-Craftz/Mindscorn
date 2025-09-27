using UnityEngine;

[RequireComponent(typeof(Animator))]
public class HallwayCrawlingBodies : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public string targetTag = "Player";

    [Header("Movement (manual, no root motion)")]
    public float moveSpeed = 0.6f;
    public float rotationSpeed = 3f;
    public float stopDistance = 0.6f;

    [Header("Animation")]
    public Animator animator;
    public string crawlBool = "Crawling";

    void Start()
    {
        if (animator == null) animator = GetComponent<Animator>();

        if (target == null)
        {
            GameObject p = GameObject.FindWithTag(targetTag);
            if (p != null) target = p.transform;
        }

        if (animator != null) animator.applyRootMotion = false;

        if (animator != null && target != null)
        {
            animator.SetBool(crawlBool, true);
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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, stopDistance);
    }
}
