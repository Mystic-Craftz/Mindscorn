using DG.Tweening;
using FMODUnity;
using UnityEngine;
using UnityEngine.UI;

public class EscapeMenuUI : MonoBehaviour
{
    public static EscapeMenuUI Instance { get; private set; }
    [SerializeField] private Button resumeBtn;
    [SerializeField] private Button settingsBtn;
    [SerializeField] private Button exitToMenuBtn;
    [SerializeField] private Button exitToDesktopBtn;
    [SerializeField] private EventReference openSound;
    [SerializeField] private EventReference closeSound;
    [SerializeField] private EventReference buttonSound;

    private CanvasGroup canvasGroup;

    private bool isDisabled = false;
    private bool isMenuOpen = false;
    [HideInInspector] public bool IsMenuOpen => isMenuOpen;

    private void Awake()
    {
        Instance = this;
        canvasGroup = GetComponent<CanvasGroup>();
        transform.DOScale(new Vector3(1, 0, 1), 0f);
        canvasGroup.DOFade(0, 0f);
    }

    private void Start()
    {
        resumeBtn.onClick.AddListener(() =>
        {
            Resume();
        });
        settingsBtn.onClick.AddListener(() =>
        {
            SettingsMenu.Instance.Toggle();
            AudioManager.Instance.PlayOneShot(buttonSound, transform.position);
        });
        exitToMenuBtn.onClick.AddListener(() =>
        {
            AudioManager.Instance.StopAllMusicImmediate();
            AudioManager.Instance.PlayOneShot(buttonSound, transform.position);
            SceneLoader.Load(SceneLoader.Scene.MainMenuScene);
        });
        exitToDesktopBtn.onClick.AddListener(() =>
        {
            AudioManager.Instance.PlayOneShot(buttonSound, transform.position);
            Application.Quit();
        });
    }


    public void Toggle()
    {
        if (isDisabled) return;

        if (canvasGroup.alpha == 0)
            Show();
        else
            Resume();
    }

    private void Show()
    {
        transform.DOScale(new Vector3(1, 1, 1), 0.2f);
        canvasGroup.DOFade(1, 0.2f);
        Time.timeScale = 0;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        PlayerController.Instance.SetCanMove(false);
        PlayerWeapons.Instance.DisableWeaponFunctions(true);
        AudioManager.Instance.PlayOneShot(openSound, transform.position);
        InventoryManager.Instance.DisableToggle();

        isMenuOpen = true;
    }
    private void Resume()
    {
        transform.DOScale(new Vector3(1, 0, 1), 0.2f);
        canvasGroup.DOFade(0, 0.2f);
        Time.timeScale = 1;
        // Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        PlayerController.Instance.SetCanMove(true);
        PlayerWeapons.Instance.DisableWeaponFunctions(false);
        AudioManager.Instance.PlayOneShot(closeSound, transform.position);
        InventoryManager.Instance.EnableToggle();

        isMenuOpen = false;
    }

    public void DisableToggle()
    {
        isDisabled = true;
    }
    public void EnableToggle()
    {
        isDisabled = false;
    }
}
