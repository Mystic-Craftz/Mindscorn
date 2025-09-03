using UnityEngine;

public class BossSensor : MonoBehaviour
{
    [SerializeField] private float viewRadius = 10f;
    [SerializeField] private LayerMask targetMask;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private float heightOffset = 1.5f;

    private BossAI boss;

    public Transform DetectedPlayer { get; private set; }
    public bool PlayerInSight => DetectedPlayer != null;

    private void Awake()
    {
        boss = GetComponent<BossAI>();
    }

    private void Update()
    {
        DetectPlayer();
    }

    private void DetectPlayer()
    {
        DetectedPlayer = null;

        Collider[] hits = Physics.OverlapSphere(transform.position, viewRadius, targetMask);

        if (hits.Length > 0)
        {
            Transform target = hits[0].transform;
            Vector3 dir = (target.position + Vector3.up * heightOffset) - (transform.position + Vector3.up * heightOffset);

            if (!Physics.Raycast(transform.position + Vector3.up * heightOffset, dir.normalized, dir.magnitude, obstacleMask))
            {
                DetectedPlayer = target;
                boss.OnPlayerDetected(target);
                return;
            }
        }

        if (DetectedPlayer == null && boss.player != null)
        {
            boss.player = null;
            boss.OnPlayerLost();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, viewRadius);
    }
}
