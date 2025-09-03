using TMPro;
using UnityEngine;

public class WeaponAmmoUI : MonoBehaviour
{
    enum AmmoType { Revolver, Shotgun, Rifle };
    [SerializeField] private InventoryItemSO revolverAmmoSO;
    [SerializeField] private InventoryItemSO shotgunAmmoSO;
    [SerializeField] private InventoryItemSO rifleAmmoSO;
    [SerializeField] private AmmoType ammoType;
    [SerializeField] private float fadeSpeed;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI ammoText;

    private bool shouldShow;

    private void Start()
    {
        canvasGroup.alpha = 0;
    }

    private void Update()
    {
        if (shouldShow)
        {
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 1, fadeSpeed * Time.deltaTime);
        }
        else
        {
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 0, fadeSpeed * Time.deltaTime);
        }

        if (shouldShow)
        {
            ammoText.text = $"X {GetAmmoCount()}";
        }
    }

    private int GetAmmoCount()
    {
        InventoryItem item;
        switch (ammoType)
        {
            case AmmoType.Revolver:
                item = InventoryManager.Instance.GetItemByID(revolverAmmoSO.itemID);
                if (item == null) return 0;
                else return item.quantity;
            case AmmoType.Shotgun:
                item = InventoryManager.Instance.GetItemByID(shotgunAmmoSO.itemID);
                if (item == null) return 0;
                else return item.quantity;
            case AmmoType.Rifle:
                item = InventoryManager.Instance.GetItemByID(rifleAmmoSO.itemID);
                if (item == null) return 0;
                else return item.quantity;
            default:
                return 0;
        }
    }

    public void SetShouldShow(bool value) => shouldShow = value;
}
