using System.Collections;
using FMODUnity;
using Unity.Cinemachine;
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
    [SerializeField] private Color eyeColor = Color.white;
    [SerializeField] private CinemachineImpulseSource impulseSource;

    [SerializeField] private EventReference footstepSound;

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
    [SerializeField] private float movingMP = 1f;
    [SerializeField] private bool isReverseQuantumAI = false;
    [SerializeField] private bool isQuantumAIActive = false;
    [SerializeField] private bool registerOnStart = true;
    [SerializeField] private bool turnOffEyesLightAtStart = true;
    [SerializeField] private bool debug = false;



    private Camera playerCamera;
    private Transform player;

    private bool seen = false;
    private bool hasBeenTriggered = false;
    private bool canDamagePlayer = true;
    private bool pausedAfterDamagingPlayer = false;

    private Material eyesMaterial;

    private Coroutine holdCoroutine;


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
            headRig.weight = 0f;
        }

        eyesMaterial = mesh.materials[1];
        eyesMaterial.SetVector("_EmissionColor", eyeColor * 15f);
        eyesMaterial.SetVector("_BaseColor", eyeColor);

        if (turnOffEyesLightAtStart)
        {
            eyesMaterial.DisableKeyword("_EMISSION");
        }

        player = PlayerController.Instance.transform;
        playerCamera = Camera.main;
        if (isQuantumAIActive) TriggerQuantumAI();

        if (registerOnStart)
            MannequinManager.Instance.RegisterMannequin(this);
    }

    private void Update()
    {
        if (debug)
            Debug.Log(isQuantumAIActive);
        QuantumAI();
    }

    private void QuantumAI()
    {
        if (!isQuantumAIActive && !hasBeenTriggered)
        {
            if (debug)
                Debug.Log(0.1f);
            agent.enabled = false;
            return;
        }

        if (pausedAfterDamagingPlayer && hasBeenTriggered)
        {
            if (debug)
                Debug.Log(0.2f);
            agent.enabled = false;
            anim.enabled = false;
            return;
        }

        if (!isQuantumAIActive && hasBeenTriggered)
        {
            if (debug)
                Debug.Log(0.3f);
            agent.enabled = false;
            anim.enabled = false;
            return;
        }

        if (debug)
            Debug.Log(1);

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

    public void PlayUnderCounterScare()
    {
        if (debug)
            Debug.Log(10);
        gameObject.SetActive(true);
        anim.enabled = true;
        anim.Play(MannequinStartPoses.UnderTheCounterScare.ToString());
    }

    private void AllowMove()
    {
        if (debug)
            Debug.Log(2);
        if (NavMesh.SamplePosition(player.position, out NavMeshHit hit, Mathf.Infinity, NavMesh.AllAreas))
        {
            NavMeshPath path = new NavMeshPath();
            agent.CalculatePath(hit.position, path);
            if (debug)
                Debug.Log(path.status);
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
        }
        else
        {
            anim.enabled = true;
            anim.Play(movingPose.ToString());
            anim.SetFloat(MULTIPLIER, movingMP);
            anim.enabled = true;
            if (agent.isStopped)
            {
                agent.isStopped = false;
            }
        }
    }

    private void StopInstantly()
    {
        if (debug)
            Debug.Log(3);
        if (!agent.isStopped)
        {
            agent.isStopped = true;
        }
        anim.SetFloat(MULTIPLIER, 0f);
        anim.enabled = false;
    }

    private void DamagePlayer()
    {
        if (debug)
            Debug.Log(4);
        if (canDamagePlayer && Vector3.Distance(transform.position, player.position) <= 1.5f && !agent.isStopped)
        {
            if (isReverseQuantumAI && seen)
            {
                impulseSource.GenerateImpulse();
                PlayerHealth.Instance.TakeDamage(25f);
                if (holdCoroutine == null)
                    holdCoroutine = StartCoroutine(HoldAfterDamagingPlayer());
            }
            else if (!isReverseQuantumAI && !seen)
            {
                impulseSource.GenerateImpulse();
                PlayerHealth.Instance.TakeDamage(25f);
                if (holdCoroutine == null)
                    holdCoroutine = StartCoroutine(HoldAfterDamagingPlayer());
            }
        }
    }

    private IEnumerator HoldAfterDamagingPlayer()
    {
        if (debug)
            Debug.Log(5);
        canDamagePlayer = false;
        pausedAfterDamagingPlayer = true;
        yield return new WaitForSeconds(holdAfterDamagingPlayer);
        canDamagePlayer = true;
        pausedAfterDamagingPlayer = false;
        holdCoroutine = null;
        if (debug)
            Debug.Log(6);
    }

    public void TriggerQuantumAI()
    {
        if (debug)
            Debug.Log(7);
        hasBeenTriggered = true;
        isQuantumAIActive = true;
        agent.enabled = true;
        agent.SetDestination(player.position);
        eyesMaterial.EnableKeyword("_EMISSION");
        eyesMaterial.SetFloat("_EmissiveIntensity", 7f);
        eyesMaterial.SetColor("_EmissiveColor", new Color(1f, 0.2f, 0.2f));
        PlaySound();
    }

    public void StopMovement()
    {
        if (debug)
            Debug.Log(8);
        if (!isQuantumAIActive) return;
        isQuantumAIActive = false;
        agent.isStopped = true;
        anim.SetFloat(MULTIPLIER, 0f);
        anim.enabled = false;
        agent.enabled = false;
        eyesMaterial.DisableKeyword("_EMISSION");
    }

    public void PlaySound()
    {
        RuntimeManager.PlayOneShot(footstepSound, transform.position);
    }

    public void Hide()
    {
        if (debug)
            Debug.Log(9);
        StopMovement();
        anim.enabled = false;
        agent.enabled = false;
        gameObject.SetActive(false);
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
