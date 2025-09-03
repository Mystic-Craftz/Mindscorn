using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ObtainedItemTemplate : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI itemQuantity;

    private CanvasGroup cg;

    private void Start()
    {
        cg = GetComponent<CanvasGroup>();
    }

    public void Show(InventoryItem item)
    {
        icon.sprite = item.data.itemIcon;
        if (item.data.isStackable)
        {
            itemQuantity.text = item.quantity.ToString();
            itemQuantity.gameObject.SetActive(true);
        }
        else itemQuantity.gameObject.SetActive(false);
    }
}
