using UnityEngine;

[RequireComponent(typeof(BossSensor))]
[RequireComponent(typeof(AIAnimationController))]
public class BossAI : MonoBehaviour
{
    [Header("Boss Settings")]
    public float wanderSpeed = 2f;
    public float chaseSpeed = 4f;
    public float attackRange = 2f;
    public float searchDuration = 5f;

    [Header("Debug Info")]
    public string currentStateName;

    [HideInInspector] public Transform player;
    [HideInInspector] public BossStateMachine stateMachine;
    [HideInInspector] public AIAnimationController anim;
    [HideInInspector] public BossSensor sensor;

    // States
    [HideInInspector] public BossWanderState wanderState;
    [HideInInspector] public BossChaseState chaseState;
    [HideInInspector] public BossSearchState searchState;
    [HideInInspector] public BossAttackState attackState;

    private void Awake()
    {
        anim = GetComponent<AIAnimationController>();
        sensor = GetComponent<BossSensor>();

        // init states
        wanderState = new BossWanderState(this);
        chaseState = new BossChaseState(this);
        searchState = new BossSearchState(this);
        attackState = new BossAttackState(this);

        stateMachine = new BossStateMachine(this, wanderState);
    }

    private void Update()
    {
        stateMachine.Update();
        currentStateName = stateMachine.CurrentState.GetType().Name;
    }

    // Called by sensor
    public void OnPlayerDetected(Transform target)
    {
        player = target;
        stateMachine.ChangeState(chaseState);
    }

    public void OnPlayerLost()
    {
        stateMachine.ChangeState(searchState);
    }
}
