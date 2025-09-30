using System.Collections.Generic;
using UnityEngine;

public class BossSensor : MonoBehaviour
{
    [SerializeField] public float viewRadius = 10f;
    [SerializeField] private float scanDistanceOffset = 1f;
    [SerializeField] private float heightOffset = 1.5f;
    [SerializeField] private float targetHeightOffset = 1.5f;
    [SerializeField] private LayerMask targetMask;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Transform ignoredBone;

    public Transform DetectedPlayer { get; private set; }
    public bool PlayerInSight => DetectedPlayer != null;

    private BossAI boss;
    private Transform tf;
    private static readonly Collider[] Hits = new Collider[1];
    private float scanInterval = 0.2f;
    private float nextScanTime;
    private bool isFirstEnable = true;

    private Collider[] ignoredColliders;
    private RaycastHit[] raycastHitBuffer = new RaycastHit[16];

    private RaycastHit? lastObstacleHit;
    private Vector3 lastTargetPoint;

    private void Awake()
    {
        tf = transform;
        boss = GetComponent<BossAI>();
        CacheIgnoredColliders();
    }

    private void OnEnable()
    {
        DetectedPlayer = null;
        lastObstacleHit = null;

        if (isFirstEnable)
        {
            float initialDelay = Random.Range(0f, scanInterval);
            nextScanTime = Time.time + initialDelay;
            isFirstEnable = false;
        }
        else
        {
            nextScanTime = Time.time;
        }
    }

    private void CacheIgnoredColliders()
    {
        var list = new List<Collider>();
        if (ignoredBone != null)
            list.AddRange(ignoredBone.GetComponentsInChildren<Collider>(true));
        ignoredColliders = list.ToArray();
    }

    private bool IsIgnoredCollider(Collider c)
    {
        if (c == null || ignoredColliders == null) return false;
        for (int i = 0; i < ignoredColliders.Length; i++)
            if (ignoredColliders[i] == c) return true;
        return false;
    }

    private void Update()
    {
        if (Time.time >= nextScanTime)
        {
            DetectPlayer();
            nextScanTime = Time.time + scanInterval;
        }
    }

    private void DetectPlayer()
    {
        Transform old = DetectedPlayer;
        DetectedPlayer = null;
        lastObstacleHit = null;

        Vector3 pos = tf.position;
        Vector3 forward = tf.forward;
        Vector3 scanOrigin = pos + forward * scanDistanceOffset + Vector3.up * heightOffset;

        int count = Physics.OverlapSphereNonAlloc(scanOrigin, viewRadius, Hits, targetMask);

        if (count > 0 && Hits[0] != null)
        {
            Collider hitCol = Hits[0];
            Vector3 bodyCenter = hitCol.bounds.center;
            lastTargetPoint = bodyCenter + Vector3.up * targetHeightOffset;

            Vector3 diff = lastTargetPoint - scanOrigin;
            float maxDist = diff.magnitude;
            Vector3 dir = diff.normalized;

            int hitCount = Physics.RaycastNonAlloc(scanOrigin, dir, raycastHitBuffer, maxDist, obstacleMask);

            RaycastHit? blockingHit = null;
            float closestDist = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                var h = raycastHitBuffer[i];
                if (IsIgnoredCollider(h.collider)) continue;
                if (h.distance < closestDist)
                {
                    closestDist = h.distance;
                    blockingHit = h;
                }
            }

            if (!blockingHit.HasValue)
            {
                DetectedPlayer = hitCol.transform;
                if (old == null)
                    boss?.OnPlayerDetected(hitCol.transform);
                return;
            }
            else
            {
                lastObstacleHit = blockingHit.Value;
            }
        }

        if (old != null && DetectedPlayer == null)
        {
            boss?.OnPlayerLost();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;
        if (tf == null) tf = transform;

        Vector3 pos = tf.position;
        Vector3 forward = tf.forward;
        Vector3 scanOrigin = pos + forward * scanDistanceOffset + Vector3.up * heightOffset;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(scanOrigin, viewRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(scanOrigin, 0.05f);

        if (DetectedPlayer != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(scanOrigin, lastTargetPoint);
        }
        if (lastObstacleHit.HasValue)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(scanOrigin, lastObstacleHit.Value.point);
            Gizmos.DrawSphere(lastObstacleHit.Value.point, 0.05f);
        }
    }

    [ContextMenu("RefreshIgnoredColliders")]
    public void RefreshIgnoredColliders()
    {
        CacheIgnoredColliders();
    }
}
