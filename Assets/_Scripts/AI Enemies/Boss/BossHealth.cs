using UnityEngine;
[RequireComponent(typeof(BossAI))]
public class BossHealth : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 1000f;
    public float currentHealth;

    [Header("Stun Trigger")]
    public float stunThresholdDamage = 200f; // amount of damage needed to trigger stun
    public bool isStalkingPhase = true; // if true, boss is invincible while stalking
    public bool invincibleDuringStalking = true; // if true, boss is invincible while stalking no death
    private float damageSinceLastStun = 0f;
    private BossAI bossAI;
    private bool isDead = false;

    void Awake()
    {
        bossAI = GetComponent<BossAI>();
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        damageSinceLastStun += amount;

        // Stun logic
        if (damageSinceLastStun >= stunThresholdDamage)
        {
            if (bossAI != null && !bossAI.lockStateTransition && bossAI.stateMachine.CurrentState != bossAI.stunState)
            {
                bossAI.stateMachine.ChangeState(bossAI.stunState);
            }
        }

        // Health logic when not in stalking phase
        if (!(isStalkingPhase && invincibleDuringStalking))
        {
            currentHealth -= amount;
            currentHealth = Mathf.Max(currentHealth, 0f);

            if (currentHealth <= 0f && !isDead)
            {
                isDead = true;
                currentHealth = 0f;

                // Force the boss into the death state immediately, bypassing any locks like stun.
                if (bossAI != null && bossAI.stateMachine != null)
                {
                    try
                    {
                        bossAI.stateMachine.ChangeState(bossAI.dieState, force: true);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[BossHealth] Failed to change to die state: {ex}");
                    }
                }
            }
        }
    }


    // Called by the stun state when stun actually begins .
    public void ClearStunAccumulation()
    {
        damageSinceLastStun = 0f;
    }

    // Utility for debugging / tuning
    public float GetAccumulatedDamage() => damageSinceLastStun;
}
