using System.Collections;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations.Rigging;

public class Mannequin : MonoBehaviour
{

    public enum MannequinStartPoses
    {
        None,
        Moving,
        Moving2,
        Pose1,
        Pose2,
        Pose3,
        AuraFarming,
        Tired,
        Welcome,
        UnderTheCounterScare
    }

    public enum MoveAnimationType
    {
        None,
        Moving,
        Moving2
    }

    private const string TAG_PLAYER = "Player";
    private const string MOVING = "Moving";
    private const string MOVING2 = "Moving2";
    private const string MULTIPLIER = "MP";


    [SerializeField] private bool shouldWorkOnStart;
    [SerializeField] private Animator anim;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Rig headRig;
    [SerializeField] private Renderer mesh;
    [SerializeField] private MannequinStartPoses startPose;
    [SerializeField] private MoveAnimationType movingPose;

    [Header("Quantum AI Settings")]
    [Tooltip("Transforms on the enemy to check visibility (head, chest, feet). If empty, uses transform.position+Vector3.up")]
    [SerializeField] private Transform[] sightPoints;
    [SerializeField, Range(0f, 180f)] private float viewAngle = 90f; // half-cone angle
    [Tooltip("If the enemy is closer than this, skip strict viewport bounds check.")]
    [SerializeField] private float minViewportDistance = 1.0f;
    [Tooltip("How far outside the screen we'll still count as 'on-screen' (useful for very close targets).")]
    [SerializeField] private float viewportMargin = 0.08f;
    [Tooltip("Radius used for a SphereCast to test occlusion (helps with thin/near obstacles).")]
    [SerializeField] private float sphereCastRadius = 0.12f;
    [Tooltip("Layers to include in occlusion checks (what should block sight).")]
    [SerializeField] private LayerMask occlusionMask = ~0; // default: all layers
    [SerializeField] private float holdAfterDamagingPlayer = 3f;
    [SerializeField] private bool isReverseQuantumAI = false;
    [SerializeField] private bool isQuantumAIActive = false;



    private Camera playerCamera;
    private Transform player;

    private bool seen = false;
    private bool hasBeenTriggered = false;

    private Material eyesMaterial;


    private void Start()
    {
        if (shouldWorkOnStart)
        {
            switch (startPose)
            {
                case MannequinStartPoses.Moving:
                    break;
                case MannequinStartPoses.Moving2:
                    break;
                case MannequinStartPoses.None:
                    anim.enabled = false;
                    break;
                case MannequinStartPoses.UnderTheCounterScare:
                    break;
                default:
                    anim.enabled = true;
                    anim.CrossFade(startPose.ToString(), 0f);
                    break;
            }
        }
        headRig.weight = 0f;
        player = PlayerController.Instance.transform;
        playerCamera = Camera.main;
        eyesMaterial = mesh.materials[1];
        eyesMaterial.DisableKeyword("_EMISSION");
        if (isQuantumAIActive) TriggerQuantumAI();
    }

    private void Update()
    {
        QuantumAI();
    }

    private void QuantumAI()
    {
        if (!isQuantumAIActive && !hasBeenTriggered)
        {
            agent.enabled = false;
            return;
        }

        agent.enabled = true;
        seen = IsSeenByPlayer();
        // if (false)
        if (isReverseQuantumAI)
        {
            if (seen)
            {
                AllowMove();
            }
            else
            {
                StopInstantly();
            }
        }
        else
        {
            if (seen)
            {
                StopInstantly();
            }
            else
            {
                AllowMove();
            }
        }

        headRig.weight = 1f;
    }

    private void AllowMove()
    {
        if (NavMesh.SamplePosition(player.position, out NavMeshHit hit, Mathf.Infinity, NavMesh.AllAreas))
        {
            NavMeshPath path = new NavMeshPath();
            agent.CalculatePath(hit.position, path);
            switch (path.status)
            {
                case NavMeshPathStatus.PathComplete:
                    agent.SetDestination(hit.position);
                    break;
                case NavMeshPathStatus.PathPartial:
                    StopInstantly();
                    break;
                case NavMeshPathStatus.PathInvalid:
                    StopInstantly();
                    break;
                default:
                    StopInstantly();
                    break;
            }
        }
        else
        {
            StopInstantly();
        }

        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            DamagePlayer();
            StartCoroutine(HoldAfterDamagingPlayer());
        }
        else
        {
            anim.enabled = true;
            anim.Play(movingPose.ToString());
            anim.SetFloat(MULTIPLIER, 1f);
            anim.enabled = true;
            if (agent.isStopped)
            {
                agent.isStopped = false;
            }
        }
    }

    private void StopInstantly()
    {
        if (!agent.isStopped)
        {
            agent.isStopped = true;
        }
        anim.SetFloat(MULTIPLIER, 0f);
        anim.enabled = false;
    }

    private void DamagePlayer()
    {
        if (isQuantumAIActive && Vector3.Distance(transform.position, player.position) <= 1.5f)
        {
            if (isReverseQuantumAI && seen)
                PlayerHealth.Instance.TakeDamage(25f);
            else if (!isReverseQuantumAI && !seen)
                PlayerHealth.Instance.TakeDamage(25f);
        }
    }

    private IEnumerator HoldAfterDamagingPlayer()
    {
        isQuantumAIActive = false;
        agent.isStopped = true;
        anim.SetFloat(MULTIPLIER, 0f);
        yield return new WaitForSeconds(holdAfterDamagingPlayer);
        isQuantumAIActive = true;
        agent.isStopped = false;
        anim.SetFloat(MULTIPLIER, 1f);
    }

    public void TriggerQuantumAI()
    {
        hasBeenTriggered = true;
        isQuantumAIActive = true;
        agent.enabled = true;
        agent.SetDestination(player.position);
        eyesMaterial.EnableKeyword("_EMISSION");
        eyesMaterial.SetFloat("_EmissiveIntensity", 7f);
        eyesMaterial.SetColor("_EmissiveColor", new Color(1f, 0.2f, 0.2f));
    }

    private bool IsSeenByPlayer()
    {
        if (playerCamera == null || player == null) return false;
        Transform camT = playerCamera.transform;

        // choose checks: provided sight points or a single fallback point
        Transform[] checks = (sightPoints != null && sightPoints.Length > 0) ? sightPoints : new Transform[] { null };

        foreach (Transform t in checks)
        {
            Vector3 targetPos = (t != null) ? t.position : (transform.position + Vector3.up);
            Vector3 toPoint = targetPos - camT.position;
            float dist = toPoint.magnitude;

            // quick "in front of camera" check (robust)
            if (Vector3.Dot(camT.forward, toPoint.normalized) <= 0f)
                continue; // behind camera -> not seen for this point

            // cone-angle test (primary on-screen check)
            float angle = Vector3.Angle(camT.forward, toPoint);
            if (angle > viewAngle) continue;

            // optional viewport bounds for mid/far targets only
            if (dist > minViewportDistance)
            {
                Vector3 vp = playerCamera.WorldToViewportPoint(targetPos);
                if (vp.z <= 0f) continue; // behind near clip plane

                float left = -viewportMargin;
                float right = 1f + viewportMargin;
                float bottom = -viewportMargin;
                float top = 1f + viewportMargin;
                if (vp.x < left || vp.x > right || vp.y < bottom || vp.y > top)
                    continue;
            }

            // Occlusion: SphereCastAll for robustness
            Ray ray = new Ray(camT.position, toPoint.normalized);
            float maxDist = dist + 0.05f;
            RaycastHit[] hits = Physics.SphereCastAll(ray, sphereCastRadius, maxDist, occlusionMask, QueryTriggerInteraction.Ignore);

            // No hits => unobstructed => seen
            if (hits == null || hits.Length == 0)
            {
                return true;
            }

            // Sort hits by distance
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            bool pointBlocked = false;
            foreach (var hit in hits)
            {
                if (hit.distance <= 0.0001f) continue; // ignore tiny hits

                // If hit is the enemy (or a child) -> visible
                if (hit.transform == transform || hit.transform.IsChildOf(transform) ||
                    (t != null && (hit.transform == t || hit.transform.IsChildOf(t))))
                {
                    return true;
                }

                // ignore hits that are part of the player (camera rig/body)
                if (player != null && (hit.transform == player || hit.transform.IsChildOf(player)))
                {
                    // keep checking other hits
                    continue;
                }

                // first meaningful hit is an obstacle -> this point is blocked
                pointBlocked = true;
                break;
            }

            if (!pointBlocked)
            {
                // all hits were player/ignored -> treat as unobstructed
                return true;
            }

            // otherwise this sight point is blocked — continue to next point
        }

        // none of the sight points were both within angle/on-screen and unobstructed
        return false;
    }
}
