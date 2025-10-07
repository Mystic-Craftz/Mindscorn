using UnityEngine;

public class ChainCollider : MonoBehaviour
{
    [SerializeField] private PhysicsChain parentChain;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("Player Door Collider") || collision.gameObject.CompareTag("Enemy"))
            parentChain.PlaySound();
    }
}
