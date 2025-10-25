using UnityEngine;
using UnityEngine.Events;

public class BossDeathWatcher : MonoBehaviour
{
    [Tooltip("Invoked once when BossHealth.currentHealth <= 0")]
    public UnityEvent onBossDeath;
    [SerializeField] private BossHealth bossHealth;
    private bool deathInvoked = false;



    void Update()
    {
        if (deathInvoked) return;

        if (bossHealth == null) return;

        if (bossHealth.currentHealth <= 0f)
        {
            deathInvoked = true;
            onBossDeath?.Invoke();
        }
    }
}
