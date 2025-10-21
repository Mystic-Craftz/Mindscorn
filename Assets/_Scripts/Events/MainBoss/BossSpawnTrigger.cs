using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BossSpawnTrigger : MonoBehaviour
{
    [Tooltip("Assign the empty GameObject (warp point) where you want the boss to appear.")]
    public Transform warpPoint;

    [Tooltip("If true, boss will be able to take damage (invincibility disabled) after warp.")]
    public bool disableInvincibilityOnSpawn = true;

    [Tooltip("Optional: only trigger once")]
    public bool triggerOnce = true;
    private bool hasTriggered = false;

    void Reset()
    {
        // ensure collider is a trigger in editor
        var c = GetComponent<Collider>();
        if (c) c.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && triggerOnce) return;
        if (!other.CompareTag("Player")) return;

        if (AIManager.Instance == null)
        {
            Debug.LogWarning("BossSpawnTrigger: No AIManager instance present.");
            return;
        }

        if (warpPoint == null)
        {
            Debug.LogWarning("BossSpawnTrigger: warpPoint not assigned.");
            return;
        }

        // Warp and activate boss
        AIManager.Instance.WarpBossTo(warpPoint, true);

        // Set boss invincibility
        AIManager.Instance.SetBossInvincibility(!disableInvincibilityOnSpawn ? true : false);

        hasTriggered = triggerOnce;
    }
}
