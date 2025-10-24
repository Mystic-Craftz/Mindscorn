using UnityEngine;

public class LookAtPlayer : MonoBehaviour
{
    [Tooltip("The object to look at.")]
    public Transform target;

    [Tooltip("If true rotate only around the Y axis (no tilting).")]
    public bool onlyYAxis = true;

    [Tooltip("Rotation speed in degrees per second. Use a high value for snappy turns.")]
    public float rotationSpeed = 360f;

    [Tooltip("If true, rotation is smoothed. If false, snaps instantly.")]
    public bool smooth = true;

    void Start()
    {
        if (target == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) target = go.transform;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 dir = target.position - transform.position;
        if (dir.sqrMagnitude < 0.0001f) return;

        if (onlyYAxis)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;

            Quaternion desired = Quaternion.LookRotation(dir.normalized, Vector3.up);
            float currentY = transform.eulerAngles.y;
            float targetY = desired.eulerAngles.y;

            if (smooth)
            {
                float newY = Mathf.MoveTowardsAngle(currentY, targetY, rotationSpeed * Time.deltaTime);
                Vector3 e = transform.eulerAngles;
                e.y = newY;
                transform.eulerAngles = e;
            }
            else
            {
                Vector3 e = transform.eulerAngles;
                e.y = targetY;
                transform.eulerAngles = e;
            }
        }
        else
        {
            Quaternion desired = Quaternion.LookRotation(dir.normalized, Vector3.up);
            if (smooth)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, desired, rotationSpeed * Time.deltaTime);
            }
            else
            {
                transform.rotation = desired;
            }
        }
    }

    public void SetTarget(Transform t) => target = t;

    public void SnapToTarget()
    {
        if (target == null) return;
        Vector3 d = target.position - transform.position;
        if (onlyYAxis) d.y = 0f;
        if (d.sqrMagnitude < 0.0001f) return;
        transform.rotation = Quaternion.LookRotation(d.normalized, Vector3.up);
    }
}
