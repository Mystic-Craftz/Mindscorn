using UnityEngine;

public class DimensionTrigger : MonoBehaviour
{
    [SerializeField] private bool triggerOnce = true;
    [SerializeField] private bool enterNeonOnTrigger = true;

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && triggerOnce) return;
        if (!other.CompareTag("Player")) return;
        if (NeonDimensionController.Instance == null)
        {
            Debug.LogWarning("No NeonDimensionController instance found.");
            return;
        }

        if (enterNeonOnTrigger)
            NeonDimensionController.Instance.EnterNeonDimension();
        else
            NeonDimensionController.Instance.ReturnToNormalInstant();

        if (triggerOnce) hasTriggered = true;
    }
}
