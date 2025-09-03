using DG.Tweening;
using TMPro;
using UnityEngine;

public class NotificationUI : MonoBehaviour
{
    public static NotificationUI Instance { get; private set; }

    [SerializeField] private GameObject notificationTextTemplate;
    [SerializeField] private GameObject obtainedItemTemplate;
    [SerializeField] private Transform notificationParent;

    private void Awake() => Instance = this;

    private void Start()
    {
        notificationTextTemplate.SetActive(false);
        obtainedItemTemplate.SetActive(false);
    }

    public void ShowNotification(InventoryItem inventoryItem)
    {
        var notification = Instantiate(obtainedItemTemplate, notificationParent);
        notification.GetComponent<ObtainedItemTemplate>().Show(inventoryItem);
        notification.SetActive(true);
        notification.GetComponent<CanvasGroup>().DOFade(1, .5f).OnComplete(() =>
        {
            notification.GetComponent<CanvasGroup>().DOFade(0, 1f).SetDelay(2f).OnComplete(() => Destroy(notification));
        });
    }

    public void ShowNotification(string message)
    {
        var notification = Instantiate(notificationTextTemplate, notificationParent);
        notification.GetComponent<TextMeshProUGUI>().text = message;
        notification.SetActive(true);
        notification.transform.DOScale(Vector3.one, .5f);
        notification.GetComponent<CanvasGroup>().DOFade(1, .5f).OnComplete(() =>
        {
            notification.transform.DOScale(new Vector3(1, 0f, 1), .5f).SetDelay(2f);
            notification.GetComponent<CanvasGroup>().DOFade(0, 1f).SetDelay(2f).OnComplete(() => Destroy(notification));
        });
    }

}
