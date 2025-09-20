using UnityEngine;

public class DimensionTrigger : MonoBehaviour
{
    [Header("Controller Reference")]
    [SerializeField] private NeonDimensionController dimensionController;

    [Header("Trigger Settings")]
    [SerializeField] private bool triggerOnEnter = true;
    [SerializeField] private bool triggerOnExit = false;
    [SerializeField] private bool triggerOnce = true;

    [Header("Transition Type")]
    [SerializeField] private bool enterNeonOnTrigger = true;

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!triggerOnEnter) return;


        if (other.CompareTag("Player"))
        {

            if (triggerOnce && hasTriggered) return;


            if (dimensionController != null)
            {
                if (enterNeonOnTrigger)
                {
                    dimensionController.EnterNeonDimension();
                }
                else
                {
                    dimensionController.ReturnToNormalDimension();
                }
                hasTriggered = true;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!triggerOnExit) return;

        if (other.CompareTag("Player"))
        {
            if (dimensionController != null)
            {
                if (enterNeonOnTrigger)
                {
                    dimensionController.ReturnToNormalDimension();
                }
                else
                {
                    dimensionController.EnterNeonDimension();
                }
            }
        }
    }


    public void ManuallyTriggerEnterNeon()
    {
        if (dimensionController != null)
        {
            dimensionController.EnterNeonDimension();
        }
    }

    public void ManuallyTriggerExitNeon()
    {
        if (dimensionController != null)
        {
            dimensionController.ReturnToNormalDimension();
        }
    }

    public void ResetTrigger()
    {
        hasTriggered = false;
    }
}