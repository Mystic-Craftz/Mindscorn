using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ConfirmNewGamePopup : MonoBehaviour
{
    public static ConfirmNewGamePopup Instance { get; private set; }
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;
    private CanvasGroup cg;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        cg = GetComponent<CanvasGroup>();
        yesButton.interactable = false;
        noButton.interactable = false;

        yesButton.onClick.AddListener(() =>
        {
            MainMenu.Instance.PlayButtonPressedInConfirmMenu();
            Hide();
        });

        noButton.onClick.AddListener(() => Hide());
    }


    public void Show()
    {
        transform.localScale = Vector3.one;
        cg.DOFade(1, .2f);
        yesButton.interactable = true;
        noButton.interactable = true;
    }

    public void Hide()
    {
        cg.DOFade(0, .2f).OnComplete(() =>
        {
            transform.localScale = new Vector3(1, 0, 1);
            yesButton.interactable = false;
            noButton.interactable = false;
        });
    }
}
