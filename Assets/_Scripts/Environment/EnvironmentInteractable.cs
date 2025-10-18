using UnityEngine;
using UnityEngine.Events;

public class EnvironmentInteractable : MonoBehaviour, IAmInteractable
{
    [SerializeField] private UnityEvent onInteract;

    private bool hasBeenInteractedWith = false;

    public void Interact()
    {
        if (hasBeenInteractedWith) return;

        hasBeenInteractedWith = true;
        onInteract?.Invoke();
    }

    public bool ShouldShowInteractionUI()
    {
        return !hasBeenInteractedWith;
    }
}
