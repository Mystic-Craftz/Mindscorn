using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using FMODUnity;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    public static SettingsMenu Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown qualityDropdown;
    [SerializeField] private TMP_Dropdown vsyncDropdown;
    [SerializeField] private TMP_Dropdown windowModeDropdown;
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider mouseSensSlider;
    [SerializeField] private Slider fovSlider;


    [Header("Volume & PostProcessing")]
    [SerializeField] private VolumeProfile mainMenuProfile;
    [SerializeField] private VolumeProfile gameProfile;
    [SerializeField] private Volume globalVolume;

    [Header("SFX")]
    [SerializeField] private EventReference menuOpenSound;
    [SerializeField] private EventReference menuConfirmSound;
    [SerializeField] private EventReference menuCloseSound;
    [SerializeField] private ScrollRect scrollRect;

    private bool isOpen = false;
    private RectTransform rectTransform;

    // Resolutions we actually present to the user (keeps indices stable)
    private List<Resolution> availableResolutions = new List<Resolution>();

    // Cached ColorAdjustments (one per profile) - may be null if profile doesn't have it
    private ColorAdjustments mainMenuColorAdjustments;
    private ColorAdjustments gameColorAdjustments;

    private const string PREF_RES_INDEX = "resolutionIndex";
    private const string PREF_WINDOW_MODE = "windowModeIndex";
    private const string PREF_QUALITY = "qualityIndex";
    private const string PREF_VSYNC = "vsyncIndex";
    private const string PREF_BRIGHTNESS = "brightness";
    private const string PREF_VOLUME = "volume";
    private const string PREF_MUSIC = "music";
    private const string PREF_MOUSE_SENS = "mouseSens";
    private const string PREF_FOV = "fieldofview";

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        // start hidden (off-screen)
        rectTransform.DOAnchorPos(new Vector2(600f, 0f), 0f);
        isOpen = false;

        // Prepare things (order matters: populate resolutions before applying saved settings)
        PopulateResolutions();
        LoadPostProcessingReferences();
        Quality();
        Vsync();
        WindowMode();
        ApplySavedResolutionAndWindowMode(); // apply saved res & window mode before setting dropdown values
        Brightness();
        Volume();
        Music();
        MouseSensitivity();
        FOV();
        HookResolutionDropdown(); // hook after initial apply so we don't spam events
    }

    private void LateUpdate()
    {
        if (isOpen && (InputManager.Instance.GetPlayerEscape() || InputManager.Instance.GetUIBackTriggered()))
        {
            Toggle();
        }
    }

    public void Toggle()
    {
        if (isOpen)
        {
            rectTransform.DOAnchorPos(new Vector2(600f, 0f), 0.2f).SetEase(Ease.OutBack);
            AudioManager.Instance.PlayOneShot(menuCloseSound, transform.position);
            if (EscapeMenuUI.Instance != null) EscapeMenuUI.Instance.OnSettingsClosed();
            if (MainMenu.Instance != null) MainMenu.Instance.OnSettingsClosed();
            isOpen = false;
        }
        else
        {
            rectTransform.DOAnchorPos(new Vector2(0f, 0f), 0.2f).SetEase(Ease.OutBack);
            AudioManager.Instance.PlayOneShot(menuOpenSound, transform.position);
            EventSystem.current.SetSelectedGameObject(resolutionDropdown.gameObject);
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 1f;
            Canvas.ForceUpdateCanvases();
            isOpen = true;
        }
    }

    public bool IsOpen() { return isOpen; }


    #region Resolution & Window Mode

    private void PopulateResolutions()
    {
        // Grab system resolutions (for current display)
        var systemResolutions = Screen.resolutions;

        // Build a unique list by width/height/refreshRate, sorted highest -> lowest
        availableResolutions = systemResolutions
            .GroupBy(r => new { r.width, r.height, r.refreshRate })
            .Select(g => g.First())
            .OrderByDescending(r => r.width)
            .ThenByDescending(r => r.height)
            .ThenByDescending(r => r.refreshRate)
            .ToList();

        // Build option strings including refresh rate (helps players choose exact mode)
        List<string> options = availableResolutions
            .Select(r => $"{r.width}x{r.height} @ {r.refreshRate}Hz")
            .ToList();

        // Fill TMP dropdown
        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(options);
    }

    private void HookResolutionDropdown()
    {
        // Remove any previous listeners and add our handler
        resolutionDropdown.onValueChanged.RemoveAllListeners();
        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
    }

    private int GetClosestResolutionIndex()
    {
        // Try to match current Screen.width/height/refresh rate first
        for (int i = 0; i < availableResolutions.Count; i++)
        {
            var r = availableResolutions[i];
            if (r.width == Screen.width && r.height == Screen.height && r.refreshRate == Screen.currentResolution.refreshRate)
                return i;
        }

        // Try match width/height
        for (int i = 0; i < availableResolutions.Count; i++)
        {
            var r = availableResolutions[i];
            if (r.width == Screen.width && r.height == Screen.height) return i;
        }

        // fallback to first
        return Mathf.Clamp(0, 0, availableResolutions.Count - 1);
    }

    private void ApplySavedResolutionAndWindowMode()
    {
        // Window mode first
        int savedWindowMode = PlayerPrefs.GetInt(PREF_WINDOW_MODE, (int)Screen.fullScreenMode);
        savedWindowMode = Mathf.Clamp(savedWindowMode, 0, 2); // we expect 0..2 mapping below
        SetWindowMode(savedWindowMode, false); // false = don't save again while applying

        // Resolution index
        int defaultIndex = GetClosestResolutionIndex();
        int savedResIndex = PlayerPrefs.GetInt(PREF_RES_INDEX, defaultIndex);
        savedResIndex = Mathf.Clamp(savedResIndex, 0, availableResolutions.Count - 1);

        // Set dropdown value visually (no callback yet)
        resolutionDropdown.SetValueWithoutNotify(savedResIndex);
        resolutionDropdown.RefreshShownValue();

        // Apply resolution with current screen.fullScreenMode
        ApplyResolution(savedResIndex, Screen.fullScreenMode, savePref: false);
    }

    private void OnResolutionChanged(int value)
    {
        value = Mathf.Clamp(value, 0, availableResolutions.Count - 1);
        PlayerPrefs.SetInt(PREF_RES_INDEX, value);

        // Apply using current fullscreen mode
        ApplyResolution(value, Screen.fullScreenMode, savePref: false);
    }

    private void ApplyResolution(int resolutionIndex, FullScreenMode mode, bool savePref = true)
    {
        if (availableResolutions == null || availableResolutions.Count == 0) return;
        resolutionIndex = Mathf.Clamp(resolutionIndex, 0, availableResolutions.Count - 1);
        Resolution chosen = availableResolutions[resolutionIndex];

        // Use SetResolution with refreshRate if available
        Screen.SetResolution(chosen.width, chosen.height, mode, chosen.refreshRate);

        if (savePref) PlayerPrefs.SetInt(PREF_RES_INDEX, resolutionIndex);
    }

    private void WindowMode()
    {
        // windowModeDropdown expected options in this order:
        // 0 -> ExclusiveFullScreen (Exclusive / native fullscreen)
        // 1 -> FullScreenWindow (Borderless)
        // 2 -> Windowed
        int windowModeIndex = PlayerPrefs.GetInt(PREF_WINDOW_MODE, (int)Screen.fullScreenMode);
        windowModeIndex = Mathf.Clamp(windowModeIndex, 0, 2);
        windowModeDropdown.SetValueWithoutNotify(windowModeIndex);
        windowModeDropdown.RefreshShownValue();

        // Hook listener
        windowModeDropdown.onValueChanged.RemoveAllListeners();
        windowModeDropdown.onValueChanged.AddListener((value) =>
        {
            PlayerPrefs.SetInt(PREF_WINDOW_MODE, value);
            SetWindowMode(value, true); // save and reapply
        });
    }

    private void SetWindowMode(int value, bool savePref = true)
    {
        FullScreenMode mode = FullScreenMode.FullScreenWindow;
        switch (value)
        {
            case 0:
                mode = FullScreenMode.ExclusiveFullScreen;
                break;
            case 1:
                mode = FullScreenMode.FullScreenWindow;
                break;
            case 2:
                mode = FullScreenMode.Windowed;
                break;
        }

        // Apply the mode and re-apply resolution so the mode change takes effect correctly
        Screen.fullScreenMode = mode;

        // Re-apply currently selected resolution (clamped)
        int resIndex = Mathf.Clamp(resolutionDropdown.value, 0, availableResolutions.Count - 1);
        ApplyResolution(resIndex, mode, savePref: false);

        if (savePref) PlayerPrefs.SetInt(PREF_WINDOW_MODE, value);
    }

    #endregion

    #region Quality & VSync

    private void Quality()
    {
        int qualityIndex = PlayerPrefs.GetInt(PREF_QUALITY, QualitySettings.GetQualityLevel());
        qualityIndex = Mathf.Clamp(qualityIndex, 0, QualitySettings.names.Length - 1);
        qualityDropdown.SetValueWithoutNotify(qualityIndex);
        qualityDropdown.RefreshShownValue();

        qualityDropdown.onValueChanged.RemoveAllListeners();
        qualityDropdown.onValueChanged.AddListener((value) =>
        {
            value = Mathf.Clamp(value, 0, QualitySettings.names.Length - 1);
            PlayerPrefs.SetInt(PREF_QUALITY, value);
            QualitySettings.SetQualityLevel(value);
        });
    }

    private void Vsync()
    {
        int vsyncIndex = PlayerPrefs.GetInt(PREF_VSYNC, QualitySettings.vSyncCount);
        vsyncIndex = Mathf.Clamp(vsyncIndex, 0, 2); // typical options: 0 (off), 1, 2
        vsyncDropdown.SetValueWithoutNotify(vsyncIndex);
        vsyncDropdown.RefreshShownValue();

        vsyncDropdown.onValueChanged.RemoveAllListeners();
        vsyncDropdown.onValueChanged.AddListener((value) =>
        {
            value = Mathf.Clamp(value, 0, 2);
            PlayerPrefs.SetInt(PREF_VSYNC, value);
            QualitySettings.vSyncCount = value;
        });
    }

    #endregion

    #region Brightness (Post Processing)

    private void LoadPostProcessingReferences()
    {
        if (mainMenuProfile != null)
            mainMenuProfile.TryGet(out mainMenuColorAdjustments);
        if (gameProfile != null)
            gameProfile.TryGet(out gameColorAdjustments);
    }

    private void Brightness()
    {
        float savedBrightness = PlayerPrefs.GetFloat(PREF_BRIGHTNESS, 0f);

        // Clamp slider target range depending on your design; many games use -1..1 or -2..2 for exposure.
        brightnessSlider.SetValueWithoutNotify(savedBrightness);

        // Apply to both profiles if they exist
        if (mainMenuColorAdjustments != null) mainMenuColorAdjustments.postExposure.value = savedBrightness;
        if (gameColorAdjustments != null) gameColorAdjustments.postExposure.value = savedBrightness;

        brightnessSlider.onValueChanged.RemoveAllListeners();
        brightnessSlider.onValueChanged.AddListener((value) =>
        {
            Debug.Log("Setting brightness to " + value);
            PlayerPrefs.SetFloat(PREF_BRIGHTNESS, value);
            if (mainMenuColorAdjustments != null) mainMenuColorAdjustments.postExposure.value = value;
            if (gameColorAdjustments != null) gameColorAdjustments.postExposure.value = value;
            if (globalVolume != null)
            {
                globalVolume.profile.TryGet(out ColorAdjustments colorAdjustments);
                if (colorAdjustments != null) colorAdjustments.postExposure.value = value;
            }
        });
    }

    #endregion

    #region Volume / Mouse Sens

    private void Volume()
    {
        var masterBus = RuntimeManager.GetBus("bus:/Master");
        float currentVolume;
        masterBus.getVolume(out currentVolume);

        float savedVolume = PlayerPrefs.GetFloat(PREF_VOLUME, currentVolume);
        volumeSlider.SetValueWithoutNotify(savedVolume);
        // Apply immediately
        masterBus.setVolume(savedVolume);

        volumeSlider.onValueChanged.RemoveAllListeners();
        volumeSlider.onValueChanged.AddListener((value) =>
        {
            PlayerPrefs.SetFloat(PREF_VOLUME, value);
            masterBus.setVolume(value);
        });
    }

    private void Music()
    {
        var musicBus = RuntimeManager.GetBus("bus:/Master/SFX/Music");
        float currentVolume;
        musicBus.getVolume(out currentVolume);

        float savedVolume = PlayerPrefs.GetFloat(PREF_MUSIC, currentVolume);
        musicSlider.SetValueWithoutNotify(savedVolume);
        // Apply immediately
        musicBus.setVolume(savedVolume);

        musicSlider.onValueChanged.RemoveAllListeners();
        musicSlider.onValueChanged.AddListener((value) =>
        {
            PlayerPrefs.SetFloat(PREF_MUSIC, value);
            musicBus.setVolume(value);
        });
    }

    private void MouseSensitivity()
    {
        float savedSens = PlayerPrefs.GetFloat(PREF_MOUSE_SENS, 0.5f);
        mouseSensSlider.SetValueWithoutNotify(savedSens);

        mouseSensSlider.onValueChanged.RemoveAllListeners();
        mouseSensSlider.onValueChanged.AddListener((value) =>
        {
            PlayerPrefs.SetFloat(PREF_MOUSE_SENS, value);
            if (PlayerController.Instance != null) PlayerController.Instance.SetSensitivity(value);
        });
    }

    private void FOV()
    {
        float savedFOV = PlayerPrefs.GetFloat(PREF_FOV, 60f);
        fovSlider.SetValueWithoutNotify(savedFOV);
        fovSlider.onValueChanged.RemoveAllListeners();
        fovSlider.onValueChanged.AddListener((value) =>
        {
            PlayerPrefs.SetFloat(PREF_FOV, value);
            if (PlayerWeapons.Instance != null) PlayerWeapons.Instance.SetFOV(value);
        });
    }

    #endregion
}