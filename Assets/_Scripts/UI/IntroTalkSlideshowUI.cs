using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using FMOD.Studio;
using FMODUnity;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class IntroTalkSlideshowUI : MonoBehaviour
{
    public static IntroTalkSlideshowUI Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI textBox;
    [SerializeField] private CanvasGroup textBoxGroup;
    [SerializeField] private TextMeshProUGUI finalText;
    [SerializeField] private CanvasGroup finalTextGroup;
    [SerializeField] private CanvasGroup bgGroup;
    [SerializeField] private Color finalTextFlashColor;
    [SerializeField] private float interPunctuationDelay = 5f;
    [SerializeField] private float nextLineScrollDelay = 5f;
    [SerializeField] private float charactersPerSecond = 5f;
    [SerializeField] private float waitUntilClosing = 5f;
    [SerializeField] private EventReference typewriterSound;
    [SerializeField] private EventReference typewriterScrollSound;
    [SerializeField] private EventReference ambientSound;

    private UnityAction onSlideshowComplete;
    private CanvasGroup canvasGroup;

    private Coroutine typewriterCoroutine;

    private bool atEnd = false;
    private bool isTypewriterStarted = false;
    private bool canSkip = false;
    private bool isOpen = false;
    private int currentVisibleCharactersIndex = 0;
    EventInstance typewriterSoundInstance;
    EventInstance ambientSoundInstance;


    private void Awake() => Instance = this;


    private void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        typewriterSoundInstance = AudioManager.Instance.CreateInstance(typewriterSound);
        Hide();
    }

    private void Update()
    {
        if (atEnd && InputManager.Instance.GetTorchToggle() && isOpen)
        {
            Hide(true);
        }

        if (!atEnd && isTypewriterStarted && InputManager.Instance.GetUseItem() && canSkip)
        {
            StopCoroutine(typewriterCoroutine);
            textBox.maxVisibleCharacters = textBox.textInfo.characterCount;
            typewriterSoundInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            StartCoroutine(WaitForClosing());
        }
    }

    private IEnumerator WaitForClosing()
    {
        yield return new WaitForSeconds(waitUntilClosing);
        bgGroup.DOFade(0, 1f);
        textBoxGroup.DOFade(0, 1f);
        finalTextGroup.DOFade(1, 1f).OnComplete(() =>
        {
            PlayerWeapons.Instance.SetDisableTorch(false);
            atEnd = true;
        });
        onSlideshowComplete?.Invoke();
        isTypewriterStarted = false;
        finalText.DOColor(finalTextFlashColor, 0.5f).SetLoops(-1, LoopType.Yoyo);
    }

    public void StartSlideshow(UnityAction onComplete)
    {
        StartCoroutine(RunAfterOneFrame(onComplete));
    }

    private IEnumerator RunAfterOneFrame(UnityAction onComplete)
    {
        yield return new WaitForEndOfFrame();
        if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);

        atEnd = false;
        isOpen = true;
        onSlideshowComplete = onComplete;
        transform.DOScale(new Vector3(1, 1, 1), 0f);
        finalTextGroup.DOFade(0, 0f);
        textBox.maxVisibleCharacters = 0;
        currentVisibleCharactersIndex = 0;
        EscapeMenuUI.Instance.DisableToggle();
        InventoryManager.Instance.DisableToggle();
        PlayerWeapons.Instance.SetDisableTorch(true);
        PlayerWeapons.Instance.DisableWeaponForASection(true);
        PlayerController.Instance.SetCanMove(false);
        ambientSoundInstance = AudioManager.Instance.CreateInstance(ambientSound);
        ambientSoundInstance.start();
        canvasGroup.DOFade(1, 2f).OnComplete(() =>
        {
            PlayerWeapons.Instance.DisableWeaponFunctions(true);
            typewriterCoroutine = StartCoroutine(TypeWriter());
        });
    }

    private IEnumerator TypeWriter()
    {
        canSkip = true;
        isTypewriterStarted = true;
        TMP_TextInfo textInfo = textBox.textInfo;
        PLAYBACK_STATE state;
        while (currentVisibleCharactersIndex < textInfo.characterCount)
        {
            char character = textInfo.characterInfo[currentVisibleCharactersIndex].character;

            typewriterSoundInstance.getPlaybackState(out state);
            textBox.maxVisibleCharacters++;
            // if (state == PLAYBACK_STATE.STOPPED)
            // typewriterSoundInstance.start();

            if (
                character == '!' || character == '.' || character == '?' || character == '\n' || character == ','
            )
            {
                if (character == '\n')
                {
                    // typewriterSoundInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                    AudioManager.Instance.PlayOneShot(typewriterScrollSound, transform.position);
                    yield return new WaitForSeconds(nextLineScrollDelay);
                }
                else
                {
                    // typewriterSoundInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                    yield return new WaitForSeconds(interPunctuationDelay);
                }
            }
            else
            {
                AudioManager.Instance.PlayOneShot(typewriterSound, transform.position);
                yield return new WaitForSeconds(1 / charactersPerSecond);
            }

            currentVisibleCharactersIndex++;
        }
        typewriterSoundInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        canSkip = false;

        yield return new WaitForSeconds(waitUntilClosing);

        bgGroup.DOFade(0, 1f);
        textBoxGroup.DOFade(0, 1f);
        finalTextGroup.DOFade(1, 1f).OnComplete(() =>
        {
            PlayerWeapons.Instance.SetDisableTorch(false);
            atEnd = true;
        });
        onSlideshowComplete?.Invoke();
        isTypewriterStarted = false;
        typewriterSoundInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        finalText.DOColor(finalTextFlashColor, 0.5f).SetLoops(-1, LoopType.Yoyo);
    }

    private void Hide(bool closedAfterTorchUsed = false)
    {
        transform.DOScale(new Vector3(1, 0, 1), 0f);
        canvasGroup.DOFade(0, 0f);
        PlayerController.Instance.SetCanMove(true);
        PlayerWeapons.Instance.DisableWeaponFunctions(false);
        PlayerWeapons.Instance.SetDisableTorch(false);
        PlayerWeapons.Instance.DisableWeaponForASection(false);
        EscapeMenuUI.Instance.EnableToggle();
        InventoryManager.Instance.EnableToggle();
        ambientSoundInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);

        if (closedAfterTorchUsed)
        {
            StartCoroutine(KnifeUseDialog());
        }
        isOpen = false;
    }

    private IEnumerator KnifeUseDialog()
    {
        yield return new WaitForSeconds(2f);
        DialogUI.Instance.ShowDialog("I should equip my knife <color=#4dff00>(1)</color> just to be safe", 3f);
    }
}
