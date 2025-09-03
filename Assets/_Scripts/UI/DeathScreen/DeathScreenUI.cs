using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class DeathScreenUI : MonoBehaviour
{
    public static DeathScreenUI Instance { get; private set; }

    [SerializeField] private DeathScreenTextEffect deathTextEffect;
    [SerializeField] private Button continueBtn;
    [SerializeField] private Button mainMenuBtn;

    private CanvasGroup canvasGroup;

    [Header("Music Settings (Death Screen)")]
    [SerializeField] private int deathMusicTrackId = 1;
    [SerializeField] private float deathMusicCrossfade = 0.5f;
    [SerializeField] private bool stopMusicImmediateOnInput = true;
    [SerializeField] private float stopFadeDuration = 0.5f;

    private bool deathScreenActive = false;
    private bool musicStoppedByInput = false;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        // ensure canvasGroup exists
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0;
        transform.DOScale(new Vector3(1, 0, 1), 0f);
    }

    private void Start()
    {

        continueBtn.onClick.AddListener(() =>
        {
            StopDeathMusicByButton();
            SceneLoader.Load(SceneLoader.Scene.DEMOBUILD_MainScene);
        });

        mainMenuBtn.onClick.AddListener(() =>
        {
            StopDeathMusicByButton();
            SceneLoader.Load(SceneLoader.Scene.MainMenuScene);
        });
    }

    private void Update()
    {
        if (deathScreenActive && !musicStoppedByInput)
        {

            if (Input.anyKeyDown || Input.GetMouseButtonDown(0) || Input.touchCount > 0)
            {
                StopDeathMusicByInput();
            }
        }
    }

    public void Show()
    {
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        if (EscapeMenuUI.Instance != null) EscapeMenuUI.Instance.DisableToggle();
        if (InventoryManager.Instance != null) InventoryManager.Instance.DisableToggle();


        transform.DOScale(new Vector3(1, 1, 1), 0).SetDelay(2f);
        canvasGroup.DOFade(1, 0.5f).SetDelay(2f);

        deathTextEffect?.StartEffect();
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.Confined;


        if (AudioManager.Instance != null)
        {
            Debug.Log("[DeathScreenUI] Stopping current music and playing death music.");

            AudioManager.Instance.StopHeartbeat();

            AudioManager.Instance.StopAllMusicImmediate();

            AudioManager.Instance.PlayMusic(deathMusicTrackId, deathMusicCrossfade, true);
        }
        else
        {
            Debug.LogWarning("[DeathScreenUI] AudioManager instance not found when trying to play death music.");
        }

        deathScreenActive = true;
        musicStoppedByInput = false;
    }

    private void StopDeathMusicByInput()
    {
        Debug.Log("[DeathScreenUI] Input detected - stopping music (input path).");

        if (AudioManager.Instance != null)
        {
            if (stopMusicImmediateOnInput)
            {
                // only stop music immediately (per your stated preference)
                AudioManager.Instance.StopAllMusicImmediate();
            }
            else
            {
                AudioManager.Instance.StopMusic(stopFadeDuration);
            }
        }
        else
        {
            Debug.LogWarning("[DeathScreenUI] AudioManager.Instance is null when trying to StopDeathMusicByInput.");
        }

        musicStoppedByInput = true;
    }

    private void StopDeathMusicByButton()
    {
        Debug.Log("[DeathScreenUI] Button pressed - stopping all audio (button path).");

        if (AudioManager.Instance != null)
        {
            // When player confirms (Continue / Main Menu) we stop EVERYTHING to be safe.
            AudioManager.Instance.StopAllPossibleInstances();
        }
        else
        {
            Debug.LogWarning("[DeathScreenUI] AudioManager.Instance is null when trying to StopDeathMusicByButton.");
        }

        musicStoppedByInput = true;
        deathScreenActive = false;
    }
}
