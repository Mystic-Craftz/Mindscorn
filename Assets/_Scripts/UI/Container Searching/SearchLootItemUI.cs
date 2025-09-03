using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SearchLootItemUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private CanvasGroup canvasGroup;

    public void SetItem(InventoryItem item, int index)
    {
        canvasGroup.DOFade(0, 0f);
        canvasGroup.DOFade(1, .5f).SetDelay(.15f * index).OnPlay(() =>
        {
            AudioManager.Instance.PlayOneShot(item.data.pickupSound, transform.position);
        });

        iconImage.sprite = item.data.itemIcon;
        if (item.data.isStackable)
            itemName.text = $"{item.data.itemName} ({item.quantity})";
        else
            itemName.text = item.data.itemName;

    }
}