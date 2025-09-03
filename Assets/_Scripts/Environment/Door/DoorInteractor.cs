using UnityEngine;

public class DoorInteractor : MonoBehaviour, IAmInteractable
{
    [SerializeField] private DoorLockFeatures doorLockFeatures;

    public void Interact()
    {
        doorLockFeatures.PerformInteract();
    }

    public bool ShouldShowInteractionUI()
    {
        return doorLockFeatures.isLocked && !doorLockFeatures.isBroken;
    }
}
