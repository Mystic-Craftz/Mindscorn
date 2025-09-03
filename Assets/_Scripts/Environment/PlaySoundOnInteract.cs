using FMODUnity;
using UnityEngine;

public class PlaySoundOnInteract : MonoBehaviour, IAmInteractable
{
    [SerializeField] private EventReference soundToPlay;
    [SerializeField] private bool canOnlyInteractOnce = false;

    private bool shouldShowInteractionUI = true;

    public void Interact()
    {
        if (shouldShowInteractionUI)
        {
            AudioManager.Instance.PlayOneShot(soundToPlay, transform.position);

            if (canOnlyInteractOnce)
            {
                shouldShowInteractionUI = false;
                InteractionUI.Instance.Hide();
            }
        }
    }

    public bool ShouldShowInteractionUI()
    {
        return shouldShowInteractionUI;
    }
}
