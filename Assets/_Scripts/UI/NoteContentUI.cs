using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using FMODUnity;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class NoteContentUI : MonoBehaviour
{
    public static NoteContentUI Instance { get; private set; }
    [SerializeField] private TextMeshProUGUI contentText;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Transform scrollContentTransform;
    [SerializeField] private Button prevBtn;
    [SerializeField] private Button nextBtn;
    [SerializeField] private Button closeBtn;
    [SerializeField] private EventReference btnSound;
    [SerializeField] private EventReference closeSound;

    private int listIndex = 0;

    private void Awake() => Instance = this;

    private List<string> contentList = new List<string>();

    private bool isOpen = false;

    private void Start()
    {
        Hide();
        prevBtn.onClick.AddListener(() =>
        {
            if (listIndex > 0)
            {
                listIndex -= 1;
                contentText.text = contentList[listIndex];
                AudioManager.Instance.PlayOneShot(btnSound, transform.position);
            }
        });
        nextBtn.onClick.AddListener(() =>
        {
            if (listIndex < contentList.Count - 1)
            {
                listIndex += 1;
                contentText.text = contentList[listIndex];
                AudioManager.Instance.PlayOneShot(btnSound, transform.position);
            }
        });
        closeBtn.onClick.AddListener(() =>
        {
            Hide();
            AudioManager.Instance.PlayOneShot(closeSound, transform.position);
        });
    }

    private void Update()
    {
        if (listIndex < 1) prevBtn.gameObject.SetActive(false);
        else prevBtn.gameObject.SetActive(true);
        if (listIndex >= contentList.Count - 1) nextBtn.gameObject.SetActive(false);
        else nextBtn.gameObject.SetActive(true);

        if (isOpen)
        {
            if (InputManager.Instance.GetPlayerEscape() || InputManager.Instance.GetUIBackTriggered())
            {
                Hide();
                AudioManager.Instance.PlayOneShot(closeSound, transform.position);
            }
        }
    }

    public void ShowContentFromList(List<string> texts)
    {
        contentText.text = texts[0];
        contentList = texts;
        gameObject.SetActive(true);
        listIndex = 0;
        canvasGroup.DOFade(1, .2f);
        scrollContentTransform.DOScale(new Vector3(1, 1, 1), .2f).SetDelay(.1f).OnComplete(() =>
        {
            ScrollRect scrollRect = scrollContentTransform.GetComponent<ScrollRect>();
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 1f;
            Canvas.ForceUpdateCanvases();
        });
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.Confined;
        Time.timeScale = 0f;
        PlayerController.Instance.SetCanMove(false);
        PlayerWeapons.Instance.DisableWeaponFunctions(true);
        EscapeMenuUI.Instance.DisableToggle();
        InventoryManager.Instance.DisableToggle();
        isOpen = true;
        EventSystem.current.SetSelectedGameObject(scrollContentTransform.gameObject);
    }

    public void Hide()
    {
        scrollContentTransform.DOScale(new Vector3(1, 0, 1), .2f);
        canvasGroup.DOFade(0, .2f).SetDelay(.1f).OnComplete(() => gameObject.SetActive(false));
        // Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        Time.timeScale = 1f;
        PlayerController.Instance.SetCanMove(true);
        PlayerWeapons.Instance.DisableWeaponFunctions(false);
        EscapeMenuUI.Instance.EnableToggle();
        InventoryManager.Instance.EnableToggle();
        isOpen = false;
    }

    public bool IsOpen() => isOpen;
}
