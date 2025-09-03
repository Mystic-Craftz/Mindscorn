using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InteractionUI : MonoBehaviour
{
    public static InteractionUI Instance { get; private set; }

    [SerializeField] private GameObject handImg;

    private bool preventingShowing = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        Hide();
    }

    public void Show()
    {
        if (preventingShowing) return;
        handImg.SetActive(true);
    }

    public void Hide(bool shouldPreventShowing = false)
    {
        handImg.SetActive(false);
        preventingShowing = shouldPreventShowing;
    }

}
