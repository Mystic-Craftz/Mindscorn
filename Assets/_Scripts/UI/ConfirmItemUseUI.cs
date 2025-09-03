using System;
using DG.Tweening;
using FMODUnity;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ConfirmItemUseUI : MonoBehaviour
{
    public static ConfirmItemUseUI Instance { get; private set; }

    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private float fadeSpeed = 1f;
    [SerializeField] private GameObject layout;
    [SerializeField] private EventReference openSound;
    [SerializeField] private EventReference cancelSound;
    [SerializeField] private EventReference confirmSound;

    private bool isOpen = false;

    private UnityAction onConfirm;
    private UnityAction onCancel;

    private void Awake() => Instance = this;

    private void Start()
    {
        canvasGroup.alpha = 0;
        transform.DOScale(new Vector3(1, 0, 1), 0f);
    }

    private void Update()
    {
        if (isOpen)
        {
            if (InputManager.Instance.GetUseItem())
            {
                AudioManager.Instance.PlayOneShot(confirmSound, transform.position);
                onConfirm?.Invoke();
            }
            if (InputManager.Instance.GetCloseTriggered())
            {
                AudioManager.Instance.PlayOneShot(cancelSound, transform.position);
                onCancel?.Invoke();
            }
        }
    }

    public void Show(InventoryItem item, UnityAction OnConfirm, UnityAction OnCancel)
    {
        iconImage.sprite = item.data.itemIcon;
        itemNameText.text = item.data.itemName;
        onConfirm = null;
        onCancel = null;
        onConfirm = OnConfirm;
        onCancel = OnCancel;
        PlayerController.Instance.SetCanMove(false);
        PlayerWeapons.Instance.DisableWeaponFunctions(true);
        gameObject.SetActive(true);
        transform.DOScale(new Vector3(1, 1, 1), 0.2f);
        canvasGroup.DOFade(1, 0.2f).OnComplete(() => isOpen = true);
        AudioManager.Instance.PlayOneShot(openSound, transform.position);
    }

    public void Hide()
    {
        PlayerController.Instance.SetCanMove(true);
        PlayerWeapons.Instance.DisableWeaponFunctions(false);
        canvasGroup.DOFade(0, 0.2f);
        onConfirm = null;
        onCancel = null;
        transform.DOScale(new Vector3(1, 0, 1), 0.2f).OnComplete(() =>
        {
            isOpen = false;
            gameObject.SetActive(false);
        });
    }

    public bool IsOpen() => isOpen;
}
