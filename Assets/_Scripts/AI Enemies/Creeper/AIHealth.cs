using UnityEngine;

[RequireComponent(typeof(MonsterAI))]
public class AIHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;

    private MonsterAI monsterAI;

    void Awake()
    {
        monsterAI = GetComponent<MonsterAI>();
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount, Vector3 hitOrigin, bool isHard, bool isStun = false)
    {
        if (monsterAI.isResurrecting || currentHealth <= 0f)
            return;

        if (monsterAI.isStartingIncapacitated)
        {
            monsterAI.isStartingIncapacitated = false;
            monsterAI.isIncapacitated = false;
            monsterAI.startingState = MonsterStartState.Wander;
            monsterAI.aiSensor.viewRadius = monsterAI.originalSensorRadius;

            if (monsterAI.agent.isOnNavMesh)
            {
                monsterAI.agent.updatePosition = true;
                monsterAI.agent.updateRotation = true;
                monsterAI.agent.isStopped = false;
            }
        }

        currentHealth -= amount;
        bool isDead = currentHealth <= 0f;
        if (isDead)
            currentHealth = 0f;

        monsterAI.RegisterHit(hitOrigin, isHard);

        // — SURGE INTERRUPT LOGIC —
        bool shouldInterrupt = true;
        if (monsterAI.stateMachine.CurrentState is ChaseState
            && monsterAI.isPowerSurging
            && !isHard
            && !isDead)
        {
            monsterAI.surgeNormalHitsCount++;
            shouldInterrupt = monsterAI.surgeNormalHitsCount >= monsterAI.normalHitsToInterrupt;
        }

        if (!shouldInterrupt)
            return;


        monsterAI.queuedStateAfterHit = null;

        if (isDead)
        {
            monsterAI.queuedStateAfterHit = monsterAI.dieState;
        }

        else if (isHard)
        {
            monsterAI.queuedStateAfterHit = monsterAI.chaseState;
        }

        else if (isStun)
        {
            monsterAI.queuedStateAfterHit = monsterAI.stunState;
            monsterAI.nextStateAfterStun = monsterAI.hissState;
            monsterAI.nextStateAfterHiss = monsterAI.chaseState;
        }
        else
        {
            if (!monsterAI.hasHissedAfterHit)
            {
                monsterAI.queuedStateAfterHit = monsterAI.hissState;
                monsterAI.nextStateAfterHiss = monsterAI.chaseState;
                monsterAI.hasHissedAfterHit = true;
            }
            else
            {
                monsterAI.queuedStateAfterHit = monsterAI.chaseState;
            }
        }

        monsterAI.lastHitWasStun = isStun;

        monsterAI.stateMachine.ChangeState(monsterAI.hitState);
    }

}
