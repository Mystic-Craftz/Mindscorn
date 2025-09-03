using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(SaveableEntity))]
public class KeyInteractableItem : MonoBehaviour, IAmInteractable, ISaveable
{

    [SerializeField] private int itemToUseId;
    [SerializeField] private string declineDialogText;
    [SerializeField] private UnityEvent onInteractSuccess;
    [SerializeField] private UnityEvent onLoadAndHasBeenInteractedWith;
    [SerializeField] private UnityEvent onInteractCancel;
    [SerializeField] private UnityEvent onDeclineInteraction;
    [SerializeField] private bool removeItemFromInventoryAfterUse;

    private bool isInteractable = true;
    private bool isInteracting = false;



    public void Interact()
    {
        if (isInteractable && !isInteracting)
        {
            InventoryItem item = InventoryManager.Instance.GetItemByID(itemToUseId);
            if (item != null)
            {
                //? If user has the key item
                ConfirmItemUseUI.Instance.Show(
                    item,
                    () =>
                    {
                        if (removeItemFromInventoryAfterUse) InventoryManager.Instance.RemoveItem(item);
                        ConfirmItemUseUI.Instance.Hide();
                        isInteractable = false;
                        onInteractSuccess?.Invoke();
                        isInteracting = false;
                    },
                    () =>
                    {
                        ConfirmItemUseUI.Instance.Hide();
                        onInteractCancel?.Invoke();
                        isInteracting = false;
                    }
                );
                isInteracting = true;
            }
            else
            {
                //? If user doesn't have the key item
                DialogUI.Instance.ShowDialog(declineDialogText);
                onDeclineInteraction?.Invoke();
            }
        }
    }

    public bool ShouldShowInteractionUI()
    {
        return isInteractable;
    }

    public object CaptureState()
    {
        return new SaveData
        {
            isInteractable = isInteractable
        };
    }

    public string GetUniqueIdentifier()
    {
        return GetComponent<SaveableEntity>().UniqueId;
    }

    public void RestoreState(object state)
    {
        string json = state as string;
        SaveData data = JsonUtility.FromJson<SaveData>(json);
        isInteractable = data.isInteractable;

        if (!isInteractable) onLoadAndHasBeenInteractedWith?.Invoke();
    }

    class SaveData
    {
        public bool isInteractable;
    }
}
