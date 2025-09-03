using System;
using FMODUnity;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(SaveableEntity))]
public class ItemPickup : MonoBehaviour, IAmInteractable, ISaveable
{
    [SerializeField] private InventoryItem item;
    [SerializeField] private UnityEvent onPickup;

    private bool isAlreadyPicked = false;

    public void Interact()
    {
        PickUp();
    }

    public void PickUp()
    {
        InventoryManager.Instance.AddItem(item);
        if (item.data.isStackable && item.quantity > 1)
        {
            NotificationUI.Instance.ShowNotification(item);
        }
        else
        {
            NotificationUI.Instance.ShowNotification(item);
        }

        if (item.data.itemType == ItemType.File)
        {
            NoteContentUI.Instance.ShowContentFromList(item.data.text);
        }
        gameObject.SetActive(false);
        isAlreadyPicked = true;
        AudioManager.Instance.PlayOneShot(item.data.pickupSound, transform.position);
        onPickup.Invoke();
    }

    public bool ShouldShowInteractionUI()
    {
        return true;
    }

    public object CaptureState()
    {
        return new SaveData { isPickedUp = isAlreadyPicked };
    }

    public string GetUniqueIdentifier()
    {
        return GetComponent<SaveableEntity>().UniqueId;
    }

    public void RestoreState(object state)
    {
        string json = state as string;
        SaveData data = JsonUtility.FromJson<SaveData>(json);
        isAlreadyPicked = data.isPickedUp;
        if (isAlreadyPicked) gameObject.SetActive(false);
    }

    [Serializable]
    class SaveData
    {
        public bool isPickedUp;
    }
}