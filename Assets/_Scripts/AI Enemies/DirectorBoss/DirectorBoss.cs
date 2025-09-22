using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations.Rigging;

public class DirectorBoss : MonoBehaviour
{
    private const string IS_WALKING = "IsWalking";
    private const string IS_DASHING = "IsDashing";
    private const string IS_STUNNED = "IsStunned";
    private const string STUN = "Stun";
    private const string PUNCH_1 = "Punch1";
    private const string PUNCH_2 = "Punch2";
    private const string DEATH = "Death";
    private const string REAL_DEATH = "RealDeath";
    private const string GETTING_UP = "GettingUp";
    private const string HEADSHOT_1 = "Headshot1";
    private const string HEADSHOT_2 = "Headshot2";
    private const string OPENING_DOOR = "OpeningDoor";
    private const string PUNCH_MP = "PunchMP";
    private const string MOVE_MP = "MoveMP";

    [Header("Main Components")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator animator;
    [SerializeField] private Rig headRig;

    [Header("Agent Settings")]
    [SerializeField] private float chaseSpeed = 3.5f;
    [SerializeField] private float normalWalkSpeed = 3.5f;
    [SerializeField] private float punchingMoveSpeed = 1.2f;
    [SerializeField] private float dashSpeed = 6f;

    [Header("Director Settings")]
    [SerializeField] private float maxHealth = 4000f;
    [SerializeField] private float distanceToPunchFrom = 1.5f;
    [SerializeField, Range(0, 1)] private float dashChance = 0.3f;
    [SerializeField] private float distanceFromPlayerToDash = 5f;
    [SerializeField] private float dashCapsuleRadius = 0.5f;
    [SerializeField] private float capsuleBottomOffset = 0.2f;
    [SerializeField] private float capsuleTopOffset = 1.6f;
    [SerializeField] private float attackDamage = 20f;
    [SerializeField] private LayerMask dashLayerMask;

    [Header("Timers")]
    [SerializeField] private float idleTimerMax = 2f;
    private float idleTimer = 0f;
    [SerializeField] private float stunTimerMax = 2f;
    private float stunTimer = 0f;
    [SerializeField] private float dashDuration = 0.6f;
    private float dashTimer = 0f;


    [Header("Debug")]
    public string currentRunningState;
    public string currentHealthString;
    [SerializeField] private bool debug = true;



    public enum DirectorState
    {
        Chasing,
        Idle,
        Moving,
        Dashing,
        Stunned,
        Punching,
        ThrowingLimb,
        FakeDeath,
        GettingUp,
        RealDeath
    }

    private DirectorState currentState;
    private Transform player;
    private Transform mainCam;
    private float currentHealth;
    private Vector3 directionToDashTowards;

    //* Unity methods
    private void Start()
    {
        player = PlayerController.Instance.transform;
        mainCam = Camera.main.transform;
        currentState = DirectorState.Idle;
        currentHealth = maxHealth;
    }

    private void Update()
    {
        switch (currentState)
        {
            case DirectorState.Chasing:
                break;

            case DirectorState.Idle:
                IdleState();
                break;

            case DirectorState.Moving:
                MovingState();
                break;

            case DirectorState.Dashing:
                DashingState();
                break;

            case DirectorState.Stunned:
                StunnedState();
                break;

            case DirectorState.Punching:
                PunchingState();
                break;

            case DirectorState.ThrowingLimb:
                break;

            case DirectorState.FakeDeath:
                break;

            case DirectorState.GettingUp:
                break;

            case DirectorState.RealDeath:
                break;

            default:
                break;
        }

        // if (debug)
        // {
        //     float moveDistance = dashSpeed * Time.deltaTime;
        //     Vector3 capsulePoint1 = transform.position + Vector3.up * capsuleBottomOffset;
        //     Vector3 capsulePoint2 = transform.position + Vector3.up * capsuleTopOffset;
        //     RaycastHit hit;

        //     if (Physics.CapsuleCast(capsulePoint1, capsulePoint2, dashCapsuleRadius, transform.forward, out hit, moveDistance + 0.5f, dashLayerMask))
        //     {
        //         Collider c = hit.collider;
        //         Debug.Log("Hit: " + c.name);
        //     }
        // }

        if (debug)
        {
            currentRunningState = currentState.ToString();
            currentHealthString = currentHealth.ToString();
        }
    }

    //* States

    private void IdleState()
    {
        animator.SetBool(IS_WALKING, false);
        animator.SetBool(IS_DASHING, false);
        agent.isStopped = true;
        agent.speed = normalWalkSpeed;

        if (idleTimer >= idleTimerMax)
        {
            idleTimer = 0f;
            SwitchToMovingState();
        }
        else
        {
            idleTimer += Time.deltaTime;
        }
    }

    private void MovingState()
    {
        agent.SetDestination(player.position);
        agent.isStopped = false;
        animator.SetBool(IS_WALKING, true);
        agent.speed = normalWalkSpeed;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer <= distanceToPunchFrom)
        {
            SwitchToPunchingState();
        }
    }

    private void DashingState()
    {
        dashTimer += Time.deltaTime;
        animator.SetBool(IS_WALKING, false);
        animator.SetBool(IS_DASHING, true);
        agent.isStopped = true;
        Vector3 moveDirection = transform.forward;
        float moveDistance = dashSpeed * Time.deltaTime;
        Vector3 capsulePoint1 = transform.position + Vector3.up * capsuleBottomOffset;
        Vector3 capsulePoint2 = transform.position + Vector3.up * capsuleTopOffset;
        RaycastHit hit;
        if (Physics.CapsuleCast(capsulePoint1, capsulePoint2, dashCapsuleRadius, transform.forward, out hit, moveDistance + 0.5f, dashLayerMask))
        {
            Collider c = hit.collider;
            Debug.Log("Hit: " + c.name);
            if (c != null)
            {
                PlayerHealth playerHealth = c.GetComponentInParent<PlayerHealth>();
                if (playerHealth != null)
                {
                    PlayerHealth.Instance.TakeDamage(attackDamage);
                    SwitchToIdleState();
                    return;
                }
                else
                {
                    SwitchToStunnedState();
                    return;
                }
            }
        }

        agent.Move(moveDirection * moveDistance);

        if (dashTimer >= dashDuration)
        {
            SwitchToIdleState();
        }
    }

    private void StunnedState()
    {
        animator.SetBool(IS_WALKING, false);
        animator.SetBool(IS_DASHING, false);
        agent.isStopped = true;
        agent.speed = 0;

        if (stunTimer >= stunTimerMax)
        {
            stunTimer = 0f;
            animator.SetBool(IS_STUNNED, false);
            animator.SetLayerWeight(2, 0);
            DOTween.To(() => headRig.weight, x => headRig.weight = x, 1f, stunTimerMax).SetEase(Ease.OutQuad);
            SwitchToMovingState();
        }
        else
        {
            stunTimer += Time.deltaTime;
        }
    }

    private void PunchingState()
    {
        FaceDirection(player.position);
        agent.Move(transform.forward * punchingMoveSpeed * Time.deltaTime);
    }

    //* Misc
    public void Damage(float amount, GameObject hitBox, bool isStun = false)
    {
        //? spine.006 is head hitbox
        if (hitBox.name == "spine.006")
        {
            amount *= 1.2f;
            animator.SetLayerWeight(1, 1);
            int randomHeadshot = Random.Range(0, 2);
            if (randomHeadshot == 0)
                animator.CrossFade(HEADSHOT_1, 0, 1);
            else
                animator.CrossFade(HEADSHOT_2, 0, 1);
        }
    }

    private void SwitchToIdleState()
    {
        currentState = DirectorState.Idle;
        idleTimer = 0f;
    }

    private void SwitchToPunchingState()
    {
        currentState = DirectorState.Punching;
        int randomPunch = Random.Range(0, 2);
        if (randomPunch == 0)
            animator.SetTrigger(PUNCH_1);
        else
            animator.SetTrigger(PUNCH_2);
    }

    private void SwitchToMovingState()
    {
        currentState = DirectorState.Moving;
        agent.isStopped = false;
        bool shouldDash = Random.value < dashChance;
        if (shouldDash)
        {
            currentState = DirectorState.Dashing;
            // directionToDashTowards = (player.position - transform.position).normalized;
            Vector3 direction = (player.position - transform.position).normalized;
            Vector3 lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z)).eulerAngles;
            transform.DORotate(lookRotation, 0.2f);
            animator.SetBool(IS_DASHING, true);
            animator.SetBool(IS_WALKING, false);
            agent.isStopped = true;
            dashTimer = 0f;
        }
        else
        {
            currentState = DirectorState.Moving;
            animator.SetBool(IS_WALKING, true);
            animator.SetBool(IS_DASHING, false);
            agent.speed = normalWalkSpeed;
            agent.isStopped = false;
        }
    }

    private void SwitchToStunnedState()
    {
        currentState = DirectorState.Stunned;
        animator.SetBool(IS_STUNNED, true);
        animator.SetBool(IS_DASHING, false);
        animator.SetBool(IS_WALKING, false);
        animator.SetLayerWeight(2, 1);
        animator.CrossFade(STUN, 0.25f, 2);
        agent.isStopped = true;
        DOTween.To(() => headRig.weight, x => headRig.weight = x, 0f, stunTimerMax).SetEase(Ease.OutQuad);
        dashTimer = 0f;
    }

    private void FaceDirection(Vector3 position)
    {
        Vector3 direction = (position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
    }

    //* Animation Events
    public void HeadshotAnmationComplete()
    {
        animator.SetLayerWeight(1, 0);
    }

    public void PunchStarting()
    {
        animator.SetFloat(PUNCH_MP, 0f);
        StartCoroutine(PunchResume());
    }

    private IEnumerator PunchResume()
    {
        yield return new WaitForSeconds(0.15f);
        animator.SetFloat(PUNCH_MP, 1f);
    }

    public void FinishPunching()
    {
        SwitchToIdleState();
    }

    //* Gizmos
    private void DrawApproxCapsule(Vector3 bottomCenter, Vector3 topCenter, float radius)
    {
        if (debug == false) return;
        Vector3 right = transform.right;
        Vector3 forward = transform.forward;
        Vector3 b_r = bottomCenter + right * radius;
        Vector3 b_l = bottomCenter - right * radius;
        Vector3 b_f = bottomCenter + forward * radius;
        Vector3 b_b = bottomCenter - forward * radius;
        Vector3 t_r = topCenter + right * radius;
        Vector3 t_l = topCenter - right * radius;
        Vector3 t_f = topCenter + forward * radius;
        Vector3 t_b = topCenter - forward * radius;
        Gizmos.DrawWireSphere(bottomCenter, radius);
        Gizmos.DrawWireSphere(topCenter, radius);
        Gizmos.DrawLine(b_r, t_r);
        Gizmos.DrawLine(b_l, t_l);
        Gizmos.DrawLine(b_f, t_f);
        Gizmos.DrawLine(b_b, t_b);
        Gizmos.DrawLine(b_r, b_f);
        Gizmos.DrawLine(b_f, b_l);
        Gizmos.DrawLine(b_l, b_b);
        Gizmos.DrawLine(b_b, b_r);
        Gizmos.DrawLine(t_r, t_f);
        Gizmos.DrawLine(t_f, t_l);
        Gizmos.DrawLine(t_l, t_b);
        Gizmos.DrawLine(t_b, t_r);
    }

    private void OnDrawGizmosSelected()
    {
        if (debug == false) return;
        Gizmos.color = Color.magenta;
        Vector3 dir = directionToDashTowards;
        if (dir == Vector3.zero) dir = transform.forward;
        Vector3 bottom = transform.position + Vector3.up * capsuleBottomOffset;
        Vector3 top = transform.position + Vector3.up * capsuleTopOffset;
        DrawApproxCapsule(bottom, top, dashCapsuleRadius);
        Vector3 projected = transform.position + dir.normalized * dashSpeed * dashDuration;
        Vector3 pBottom = projected + Vector3.up * capsuleBottomOffset;
        Vector3 pTop = projected + Vector3.up * capsuleTopOffset;
        DrawApproxCapsule(pBottom, pTop, dashCapsuleRadius);
        Gizmos.DrawLine(bottom, pBottom);
        Gizmos.DrawLine(top, pTop);
    }
}
