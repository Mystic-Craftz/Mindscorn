using System.Collections;
using System.IO;
using DG.Tweening;
using FMODUnity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DemoEndScreenUI : MonoBehaviour
{
    public static DemoEndScreenUI Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI[] text;
    [SerializeField] private CanvasGroup[] textCG;
    [SerializeField] private Button mainMenuBtn;
    [SerializeField] private CanvasGroup mainMenuBtnCG;
    [SerializeField] private EventReference buttonSound;
    [SerializeField] private EventReference typewriterSound;
    [SerializeField] private TextMeshProUGUI developerNote;

    private CanvasGroup canvasGroup;

    private bool doesHaveExistingSave = false;
    private int visibleTitleIndex = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        canvasGroup = GetComponent<CanvasGroup>();
        transform.DOScale(new Vector3(1, 0, 1), 0f);
        canvasGroup.DOFade(0, 0f);
        for (int i = 0; i < text.Length; i++)
        {
            textCG[i].alpha = 0;
        }
        mainMenuBtnCG.alpha = 0;
        mainMenuBtn.interactable = false;
    }


    private void Start()
    {
        string saveFilePath = Path.Combine(Application.persistentDataPath, "demo-save.json");

        if (File.Exists(saveFilePath)) doesHaveExistingSave = true;

        mainMenuBtn.onClick.AddListener(() =>
        {
            if (doesHaveExistingSave)
            {
                File.Delete(saveFilePath);
            }
            AudioManager.Instance.StopAllPossibleInstances();
            AudioManager.Instance.PlayOneShot(buttonSound, transform.position);
            SceneLoader.Load(SceneLoader.Scene.MainMenuScene);
        });
    }

    public void Show()
    {
        if (EscapeMenuUI.Instance != null) EscapeMenuUI.Instance.DisableToggle();
        if (InventoryManager.Instance != null) InventoryManager.Instance.DisableToggle();
        StartCoroutine(PlayDemoEndAnimation());
    }

    private IEnumerator PlayDemoEndAnimation()
    {
        developerNote.maxVisibleCharacters = 0;
        transform.DOScale(new Vector3(1, 1, 1), 0f);
        canvasGroup.DOFade(1, 0.5f);
        yield return new WaitForSeconds(1f);

        for (int i = 0; i < text.Length; i++)
        {
            textCG[i].DOFade(1, 0.5f).SetDelay(0.3f * i);
        }

        yield return new WaitForSeconds(2f);
        mainMenuBtn.interactable = true;
        mainMenuBtnCG.DOFade(1, 0.5f).OnComplete(() =>
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        });

        visibleTitleIndex = 0;
        developerNote.maxVisibleCharacters = 0;
        while (visibleTitleIndex < developerNote.text.Length)
        {
            developerNote.maxVisibleCharacters++;
            visibleTitleIndex++;
            AudioManager.Instance.PlayOneShot(typewriterSound, transform.position);
            yield return new WaitForSeconds(1f / 15f);
        }

    }
}
