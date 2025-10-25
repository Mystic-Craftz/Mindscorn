using FMODUnity;
using UnityEngine;
using UnityEngine.Events;

public class EnvironmentInteractable : MonoBehaviour, IAmInteractable
{
    [SerializeField] private UnityEvent onInteract;
    [SerializeField] private EventReference onCollisionSound;

    private bool hasBeenInteractedWith = false;

    private bool canPlaySoundAgain = true;

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

    private void OnCollisionEnter(Collision collision)
    {
        if (onCollisionSound.IsNull) return;
        if (canPlaySoundAgain)
        {
            AudioManager.Instance.PlayOneShot(onCollisionSound, transform.position);
            canPlaySoundAgain = false;
        }
    }
}
