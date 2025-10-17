using System.Collections;
using System.Collections.Generic;
using System.IO;
using DG.Tweening;
using FMOD.Studio;
using FMODUnity;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public static MainMenu Instance { get; private set; }
    [SerializeField] private Button continueBtn;
    [SerializeField] private Button playBtn;
    [SerializeField] private Button settingsBtn;
    [SerializeField] private Button creditsBtn;
    [SerializeField] private Button exitBtn;
    [SerializeField] private Button discordBtn;
    [SerializeField] private Button steamBtn;
    [SerializeField] private Button xBtn;


    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private List<CanvasGroup> menuItems;
    [SerializeField] private CanvasGroup blackImage;
    [SerializeField] private EventReference btnClick;
    [SerializeField] private EventReference mainMenuMusic;
    [SerializeField] private EventReference typewriterSound;

    private bool doesHaveExistingSave = false;

    private int visibleTitleIndex = 0;

    private EventInstance mainMenuMusicInstance;

    private void Awake()
    {
        Instance = this;
        for (int i = 0; i < menuItems.Count; i++)
        {
            menuItems[i].alpha = 0;
        }
        titleText.maxVisibleCharacters = 0;
    }

    private void Start()
    {
        Time.timeScale = 1f;
        mainMenuMusicInstance = AudioManager.Instance.CreateInstance(mainMenuMusic);
        mainMenuMusicInstance.start();

        string saveFilePath = Path.Combine(Application.persistentDataPath, "demo-save.json");

        MainMenuAnimations();

        //? To delete save file for testing
        // File.Delete(saveFilePath);

        if (File.Exists(saveFilePath)) doesHaveExistingSave = true;



        continueBtn.gameObject.SetActive(doesHaveExistingSave);
        continueBtn.onClick.AddListener(() =>
        {
            mainMenuMusicInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            blackImage.DOFade(1f, 1f).OnComplete(() => SceneLoader.Load(SceneLoader.Scene.DEMOBUILD_MainScene));
            AudioManager.Instance.PlayOneShot(btnClick, transform.position);
        });
        playBtn.onClick.AddListener(() =>
        {
            if (doesHaveExistingSave)
            {
                File.Delete(saveFilePath);
            }
            mainMenuMusicInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            blackImage.DOFade(1f, 1f).OnComplete(() => SceneLoader.Load(SceneLoader.Scene.DEMOBUILD_MainScene));
            AudioManager.Instance.PlayOneShot(btnClick, transform.position);
        });
        settingsBtn.onClick.AddListener(() =>
        {
            SettingsMenu.Instance.Toggle();
        });
        creditsBtn.onClick.AddListener(() =>
        {

        });
        exitBtn.onClick.AddListener(() =>
        {
            mainMenuMusicInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            AudioManager.Instance.PlayOneShot(btnClick, transform.position);
            Application.Quit();
        });
        discordBtn.onClick.AddListener(() =>
        {
            Application.OpenURL("https://discord.gg/GCmvH97TVU");
        });
        steamBtn.onClick.AddListener(() =>
        {
            Application.OpenURL("https://store.steampowered.com/app/3944010/Mindscorn/");
        });
        xBtn.onClick.AddListener(() =>
        {
            Application.OpenURL("https://x.com/MysticCraftz_");
        });
    }

    private void MainMenuAnimations()
    {
        StartCoroutine(MainMenuAnimation());
    }

    private IEnumerator MainMenuAnimation()
    {
        yield return new WaitForSeconds(.5f);
        blackImage.DOFade(0f, 1f);
        yield return new WaitForSeconds(1f);
        visibleTitleIndex = 0;
        while (visibleTitleIndex < titleText.text.Length)
        {
            titleText.maxVisibleCharacters++;
            visibleTitleIndex++;
            AudioManager.Instance.PlayOneShot(typewriterSound, transform.position);
            yield return new WaitForSeconds(1f / 15f);
        }

        for (int i = 0; i < menuItems.Count; i++)
        {
            menuItems[i].DOFade(1f, 1f).SetDelay(.25f * i);
        }

        if (doesHaveExistingSave)
        {
            EventSystem.current.SetSelectedGameObject(continueBtn.gameObject);
        }
        else
        {
            EventSystem.current.SetSelectedGameObject(playBtn.gameObject);
        }
    }

    public void OnSettingsClosed()
    {
        EventSystem.current.SetSelectedGameObject(settingsBtn.gameObject);
    }
}