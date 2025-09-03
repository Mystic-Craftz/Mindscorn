using UnityEngine;

public class DamageDealer : MonoBehaviour
{
    public float damageAmount = 10f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (PlayerHealth.Instance != null)
            {
                PlayerHealth.Instance.TakeDamage(damageAmount);
                Debug.Log($"Player took {damageAmount} damage!");
            }
        }
    }
}
