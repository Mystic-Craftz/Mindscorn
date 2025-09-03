using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class Inspectable : MonoBehaviour, IAmInteractable
{
    [SerializeField] private DialogSO dialogSO;
    [SerializeField] private UnityEvent onInteract;
    [SerializeField] private float duration = 2f;

    private bool canInteractAgain = true;

    public void Interact()
    {
        if (canInteractAgain)
        {
            DialogUI.Instance.ShowDialog(dialogSO.text, duration);
            onInteract?.Invoke();
            canInteractAgain = false;
            StartCoroutine(ResetCanInteractAgain());
        }

    }

    private IEnumerator ResetCanInteractAgain()
    {
        yield return new WaitForSeconds(duration);
        canInteractAgain = true;
    }

    public bool ShouldShowInteractionUI()
    {
        return true;
    }
}
