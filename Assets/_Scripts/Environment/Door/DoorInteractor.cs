using UnityEngine;

public class DoorInteractor : MonoBehaviour, IAmInteractable
{
    [SerializeField] private DoorLockFeatures doorLockFeatures;
    public bool debug = false;

    public void Interact()
    {
        doorLockFeatures.PerformInteract();
    }

    public bool ShouldShowInteractionUI()
    {
        return doorLockFeatures.isLocked && !doorLockFeatures.isBroken;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (debug) Debug.Log("OnCollisionEnter" + collision.gameObject.name);
    }
}
