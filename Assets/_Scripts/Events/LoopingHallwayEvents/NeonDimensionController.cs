using UnityEngine;
using DG.Tweening;
using System;

public class NeonDimensionController : MonoBehaviour
{
    public static NeonDimensionController Instance { get; private set; }

    [Header("Materials (assign the material assets used by your renderer features)")]
    [SerializeField] private Material neonMaterial;
    [SerializeField] private Material glitchMaterial;
    [SerializeField] private Material consciousnessMat;

    [Header("General (PlayGlitch) settings")]
    [SerializeField, Min(1)] private int generalBlinkCount = 2;
    [SerializeField, Min(0f)] private float generalBlinkOnDuration = 0.06f;
    [SerializeField, Min(0f)] private float generalBlinkOffDuration = 0.06f;

    [Header("Neon-enter (EnterNeonDimension) settings")]
    [SerializeField, Min(1)] private int neonBlinkCount = 3;
    [SerializeField, Min(0f)] private float neonBlinkOnDuration = 0.06f;
    [SerializeField, Min(0f)] private float neonBlinkOffDuration = 0.06f;

    [Header("Consciousness tuning (targets)")]
    [SerializeField, Range(0f, 8f)] private float targetBlur = 5f;
    [SerializeField, Range(0f, 2f)] private float targetRadial = 0.9f;
    [SerializeField, Range(0f, 1f)] private float targetDouble = 0.6f;
    [SerializeField, Range(0f, 10f)] private float targetChroma = 2.0f;
    [SerializeField, Range(0f, 1f)] private float targetVignette = 0.55f;
    [SerializeField, Range(0f, 1f)] private float targetWobble = 0.35f;
    [SerializeField, Range(0f, 40f)] private float targetDoubleOffset = 10f;
    [SerializeField, Min(0f)] private float consciousnessDuration = 2.2f;

    [Header("Pre-dark blink settings (before full blackout)")]
    [SerializeField, Min(0)] private int preDarkBlinkCount = 2;
    [SerializeField, Min(0f)] private float preDarkBlinkOnDuration = 0.06f;
    [SerializeField, Min(0f)] private float preDarkBlinkOffDuration = 0.06f;
    [SerializeField, Range(0.05f, 1f)] private float preDarkBlinkBlend = 0.6f;

    [Header("Smooth blink tuning")]
    [SerializeField, Range(0.5f, 1f)] private float blinkCloseValue = 0.95f;
    [SerializeField, Min(0.01f)] private float blinkCloseDuration = 0.09f;
    [SerializeField, Min(0.01f)] private float blinkOpenDuration = 0.12f;
    [SerializeField, Min(0f)] private float blinkHoldTime = 0.02f;
    [SerializeField] private Ease blinkCloseEase = Ease.InSine;
    [SerializeField] private Ease blinkOpenEase = Ease.OutSine;

    [Header("Wake settings")]
    [SerializeField, Min(0f)] private float wakeDuration = 1.4f;
    [SerializeField] private Color fadeColor = Color.black;
    [SerializeField, Min(0f)] private float wakeDefaultOpenDuration = 1.4f;
    [SerializeField, Min(0f)] private float wakeDefaultBlendFade = 0.35f;

    [Header("Wake blink tuning (added)")]
    [SerializeField, Min(0)] private int wakeMidBlinkCount = 1;
    [SerializeField, Min(0)] private int wakeRapidBlinkCount = 3;
    [SerializeField, Min(0.01f)] private float wakeRapidCloseDuration = 0.05f;
    [SerializeField, Min(0.01f)] private float wakeRapidOpenDuration = 0.06f;
    [SerializeField, Min(0f)] private float wakeRapidHold = 0.01f;

    [Header("Audio settings")]
    [Tooltip("Music track id to switch to when entering Neon Dimension (creepy piano). Default 6.")]
    [SerializeField] private int neonMusicTrackId = 6;

    private int idBlend;
    private int idBlur;
    private int idRadial;
    private int idDouble;
    private int idChroma;
    private int idVignette;
    private int idEyeClose;
    private int idEyeSoftness;
    private int idDoubleOffset;
    private int idFadeColor;
    private int idWobble;

    private Sequence activeSeq;
    private bool isInNeonDimension = false;
    private bool sprintDisabledByController = false;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        DOTween.Init(false, true, LogBehaviour.ErrorsOnly);
        InitializeMaterials();
    }

    private void InitializeMaterials()
    {
        idBlend = Shader.PropertyToID("_Blend");
        idBlur = Shader.PropertyToID("_BlurAmount");
        idRadial = Shader.PropertyToID("_RadialSmear");
        idDouble = Shader.PropertyToID("_DoubleStrength");
        idChroma = Shader.PropertyToID("_Chroma");
        idVignette = Shader.PropertyToID("_Vignette");
        idEyeClose = Shader.PropertyToID("_EyeClose");
        idEyeSoftness = Shader.PropertyToID("_EyeSoftness");
        idDoubleOffset = Shader.PropertyToID("_DoubleOffset");
        idFadeColor = Shader.PropertyToID("_FadeColor");
        idWobble = Shader.PropertyToID("_WobbleStrength");

        if (neonMaterial != null)
            neonMaterial.SetFloat(idBlend, 0f);
        if (glitchMaterial != null)
            glitchMaterial.SetFloat(idBlend, 0f);
        if (consciousnessMat != null)
        {
            consciousnessMat.SetFloat(idBlend, 0f);
            consciousnessMat.SetFloat(idBlur, 0f);
            consciousnessMat.SetFloat(idRadial, 0f);
            consciousnessMat.SetFloat(idDouble, 0f);
            consciousnessMat.SetFloat(idChroma, 0f);
            consciousnessMat.SetFloat(idVignette, 0.25f);
            consciousnessMat.SetFloat(idEyeClose, 0f);
            consciousnessMat.SetFloat(idEyeSoftness, 0.15f);
            consciousnessMat.SetFloat(idDoubleOffset, 8f);
            consciousnessMat.SetFloat(idWobble, 0f);
            consciousnessMat.SetColor(idFadeColor, fadeColor);
        }
    }

    private void OnDestroy()
    {
        if (isInNeonDimension && AudioManager.Instance != null)
        {
            AudioManager.Instance.ExitNeonDimensionAudio();
        }
    }

    public void EnterNeonDimension(int blinks = -1)
    {
        if (neonMaterial == null)
        {
            Debug.LogWarning("NeonDimensionController: neonMaterial not assigned.");
            return;
        }

        int actualBlinks = (blinks > 0) ? blinks : neonBlinkCount;
        KillActiveSequence();
        activeSeq = DOTween.Sequence();

        if (glitchMaterial != null)
        {
            // Start glitch audio loop at the beginning
            activeSeq.AppendCallback(() =>
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.StartGlitchLoop();
                }
            });

            // disable sprint before glitch sequence
            activeSeq.AppendCallback(() =>
            {
                if (PlayerController.Instance != null)
                {
                    PlayerController.Instance.SetDisableSprint(true);
                    sprintDisabledByController = true;
                }
            });

            AppendBlinkSequence(activeSeq, glitchMaterial, actualBlinks, neonBlinkOnDuration, neonBlinkOffDuration);

            // After blink sequence, reset glitch material and re-enable sprint
            activeSeq.AppendCallback(() =>
            {
                SetBlendImmediate(glitchMaterial, 0f);
                if (sprintDisabledByController && PlayerController.Instance != null)
                {
                    PlayerController.Instance.SetDisableSprint(false);
                    sprintDisabledByController = false;
                }

                // Stop glitch audio when visual glitches end
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.StopGlitchLoop();
                }
            });
        }

        activeSeq.AppendCallback(() =>
        {
            SetBlendImmediate(neonMaterial, 1f);
            isInNeonDimension = true;

            // Start neon music and whisper loop
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.EnterNeonDimensionAudio(neonMusicTrackId);
            }
        });

        activeSeq.OnComplete(() => activeSeq = null);
        activeSeq.Play();
    }

    public void ReturnToNormalInstant()
    {
        KillActiveSequence();
        if (neonMaterial != null)
            SetBlendImmediate(neonMaterial, 0f);
        if (glitchMaterial != null)
            SetBlendImmediate(glitchMaterial, 0f);

        if (AudioManager.Instance != null)
            AudioManager.Instance.ExitNeonDimensionAudio();

        isInNeonDimension = false;
    }

    public void PlayGlitch(int blinks = -1)
    {
        if (glitchMaterial == null)
        {
            Debug.LogWarning("NeonDimensionController: glitchMaterial not assigned.");
            return;
        }

        int actualBlinks = (blinks > 0) ? blinks : generalBlinkCount;
        KillActiveSequence();
        activeSeq = DOTween.Sequence();

        // Start glitch audio loop at the beginning
        activeSeq.AppendCallback(() =>
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StartGlitchLoop();
            }
        });

        // disable sprint for this glitch sequence
        activeSeq.AppendCallback(() =>
        {
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.SetDisableSprint(true);
                sprintDisabledByController = true;
            }
        });

        AppendBlinkSequence(activeSeq, glitchMaterial, actualBlinks, generalBlinkOnDuration, generalBlinkOffDuration);

        // clear blend and re-enable sprint
        activeSeq.AppendCallback(() =>
        {
            SetBlendImmediate(glitchMaterial, 0f);
            if (sprintDisabledByController && PlayerController.Instance != null)
            {
                PlayerController.Instance.SetDisableSprint(false);
                sprintDisabledByController = false;
            }
        });

        // Stop glitch audio at the end
        activeSeq.AppendCallback(() =>
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopGlitchLoop();
            }
        });

        activeSeq.OnComplete(() => activeSeq = null);
        activeSeq.Play();
    }

    // Consciousness methods
    public void StartBlink(bool resetBlendAfter = true, float optionalBlend = -1f)
    {
        if (consciousnessMat == null)
        {
            Debug.LogWarning("NeonDimensionController: consciousnessMat not assigned.");
            return;
        }

        KillActiveSequence();
        float blendToUse = (optionalBlend > 0f) ? optionalBlend : preDarkBlinkBlend;
        blendToUse = Mathf.Clamp(blendToUse, 0.05f, 1f);

        activeSeq = DOTween.Sequence();
        activeSeq.AppendCallback(() => SetBlendImmediate(consciousnessMat, blendToUse));
        activeSeq.Append(DOTween.To(() => consciousnessMat.GetFloat(idEyeClose), x => consciousnessMat.SetFloat(idEyeClose, x), blinkCloseValue, blinkCloseDuration)
            .SetEase(blinkCloseEase));

        if (blinkHoldTime > 0f)
            activeSeq.AppendInterval(blinkHoldTime);

        activeSeq.Append(DOTween.To(() => consciousnessMat.GetFloat(idEyeClose), x => consciousnessMat.SetFloat(idEyeClose, x), 0f, blinkOpenDuration)
            .SetEase(blinkOpenEase));

        if (resetBlendAfter)
        {
            activeSeq.AppendCallback(() => SetBlendImmediate(consciousnessMat, 0f));
        }

        activeSeq.OnComplete(() => activeSeq = null);
        activeSeq.Play();
    }

    public void LoseConsciousness(float duration = -1f)
    {
        if (consciousnessMat == null)
        {
            Debug.LogWarning("NeonDimensionController: consciousnessMat not assigned.");
            return;
        }

        duration = (duration > 0f) ? duration : consciousnessDuration;
        KillActiveSequence();
        activeSeq = DOTween.Sequence();
        activeSeq.AppendCallback(() => SetBlendImmediate(consciousnessMat, preDarkBlinkBlend));

        for (int i = 0; i < preDarkBlinkCount; i++)
        {
            activeSeq.Append(DOTween.To(() => consciousnessMat.GetFloat(idEyeClose), x => consciousnessMat.SetFloat(idEyeClose, x), blinkCloseValue, blinkCloseDuration).SetEase(blinkCloseEase));
            if (blinkHoldTime > 0f)
                activeSeq.AppendInterval(blinkHoldTime);
            activeSeq.Append(DOTween.To(() => consciousnessMat.GetFloat(idEyeClose), x => consciousnessMat.SetFloat(idEyeClose, x), 0f, blinkOpenDuration).SetEase(blinkOpenEase));
            activeSeq.AppendInterval(Mathf.Max(0f, preDarkBlinkOffDuration));
        }

        activeSeq.AppendInterval(0.05f);
        activeSeq.Append(DOTween.To(() => consciousnessMat.GetFloat(idBlend), x => consciousnessMat.SetFloat(idBlend, x), 1f, duration * 0.6f));
        activeSeq.Join(DOTween.To(() => consciousnessMat.GetFloat(idBlur), x => consciousnessMat.SetFloat(idBlur, x), targetBlur, duration * 0.6f));
        activeSeq.Join(DOTween.To(() => consciousnessMat.GetFloat(idRadial), x => consciousnessMat.SetFloat(idRadial, x), targetRadial, duration * 0.6f));
        activeSeq.Join(DOTween.To(() => consciousnessMat.GetFloat(idDouble), x => consciousnessMat.SetFloat(idDouble, x), targetDouble, duration * 0.55f));
        activeSeq.Join(DOTween.To(() => consciousnessMat.GetFloat(idChroma), x => consciousnessMat.SetFloat(idChroma, x), targetChroma, duration * 0.6f));
        activeSeq.Join(DOTween.To(() => consciousnessMat.GetFloat(idVignette), x => consciousnessMat.SetFloat(idVignette, x), targetVignette, duration * 0.6f));
        activeSeq.Join(DOTween.To(() => consciousnessMat.GetFloat(idDoubleOffset), x => consciousnessMat.SetFloat(idDoubleOffset, x), targetDoubleOffset, duration * 0.6f));
        activeSeq.Join(DOTween.To(() => consciousnessMat.GetFloat(idWobble), x => consciousnessMat.SetFloat(idWobble, x), targetWobble, duration * 0.6f));
        activeSeq.Join(DOTween.To(() => consciousnessMat.GetFloat(idEyeClose), x => consciousnessMat.SetFloat(idEyeClose, x), 0.52f, duration * 0.7f));
        activeSeq.Join(DOTween.To(() => consciousnessMat.GetFloat(idEyeSoftness), x => consciousnessMat.SetFloat(idEyeSoftness, x), 0.22f, duration * 0.6f));

        activeSeq.AppendInterval(0.25f);
        activeSeq.OnComplete(() => activeSeq = null);
        activeSeq.Play();
    }

    public void WakeUp()
    {
        WakeUp(wakeDefaultOpenDuration, wakeDefaultBlendFade);
    }

    public void InstantBlackout()
    {
        InstantBlackout(0.15f, 1f);
    }

    public void WakeUp(float openDuration = -1f, float blendFadeDuration = 0.35f)
    {
        if (consciousnessMat == null)
        {
            Debug.LogWarning("NeonDimensionController: consciousnessMat not assigned.");
            return;
        }

        KillActiveSequence();
        float openDur = (openDuration > 0f) ? openDuration : wakeDuration;
        openDur = Mathf.Max(0.01f, openDur);
        blendFadeDuration = Mathf.Max(0f, blendFadeDuration);

        activeSeq = DOTween.Sequence();
        activeSeq.AppendCallback(() =>
        {
            SetBlendImmediate(consciousnessMat, 1f);
            float currentEyeClose = consciousnessMat.GetFloat(idEyeClose);
            if (currentEyeClose < 0.5f)
                consciousnessMat.SetFloat(idEyeClose, 0.52f);
        });

        activeSeq.Append(
            DOTween.To(() => consciousnessMat.GetFloat(idEyeClose), x => consciousnessMat.SetFloat(idEyeClose, x), 0f, openDur)
            .SetEase(blinkOpenEase)
        );

        activeSeq.AppendInterval(0.05f);

        if (wakeMidBlinkCount > 0)
        {
            Sequence midSeq = DOTween.Sequence();
            for (int i = 0; i < wakeMidBlinkCount; i++)
            {
                midSeq.Append(DOTween.To(() => consciousnessMat.GetFloat(idEyeClose), x => consciousnessMat.SetFloat(idEyeClose, x), blinkCloseValue, blinkCloseDuration).SetEase(blinkCloseEase));
                if (blinkHoldTime > 0f)
                    midSeq.AppendInterval(blinkHoldTime);
                midSeq.Append(DOTween.To(() => consciousnessMat.GetFloat(idEyeClose), x => consciousnessMat.SetFloat(idEyeClose, x), 0f, blinkOpenDuration).SetEase(blinkOpenEase));
                midSeq.AppendInterval(0.02f);
            }
            activeSeq.Append(midSeq);
        }

        if (blendFadeDuration > 0f)
        {
            Tween blendTween = DOTween.To(() => consciousnessMat.GetFloat(idBlend), x => consciousnessMat.SetFloat(idBlend, x), 0f, blendFadeDuration);
            Sequence rapidSeq = DOTween.Sequence();
            for (int i = 0; i < wakeRapidBlinkCount; i++)
            {
                rapidSeq.Append(DOTween.To(() => consciousnessMat.GetFloat(idEyeClose), x => consciousnessMat.SetFloat(idEyeClose, x), blinkCloseValue, Mathf.Max(0.01f, wakeRapidCloseDuration)).SetEase(blinkCloseEase));
                if (wakeRapidHold > 0f)
                    rapidSeq.AppendInterval(wakeRapidHold);
                rapidSeq.Append(DOTween.To(() => consciousnessMat.GetFloat(idEyeClose), x => consciousnessMat.SetFloat(idEyeClose, x), 0f, Mathf.Max(0.01f, wakeRapidOpenDuration)).SetEase(blinkOpenEase));
                rapidSeq.AppendInterval(0.01f);
            }
            activeSeq.Append(blendTween);
            activeSeq.Join(rapidSeq);
        }
        else
        {
            activeSeq.AppendCallback(() => SetBlendImmediate(consciousnessMat, 0f));
        }

        activeSeq.AppendCallback(() => SetBlendImmediate(consciousnessMat, 0f));
        activeSeq.OnComplete(() => activeSeq = null);
        activeSeq.Play();
    }

    public void InstantBlackout(float wobbleStartDuration = 0.15f, float wobbleStrength = 1f)
    {
        if (consciousnessMat == null)
        {
            Debug.LogWarning("NeonDimensionController: consciousnessMat not assigned.");
            return;
        }

        KillActiveSequence();
        activeSeq = DOTween.Sequence();
        activeSeq.AppendCallback(() => SetBlendImmediate(consciousnessMat, 1f));
        activeSeq.Append(DOTween.To(() => consciousnessMat.GetFloat(idWobble), x => consciousnessMat.SetFloat(idWobble, x), wobbleStrength, Mathf.Max(0.01f, wobbleStartDuration)).SetEase(Ease.InOutSine));
        activeSeq.AppendCallback(() =>
        {
            SetBlendImmediate(consciousnessMat, 1f);
            consciousnessMat.SetFloat(idEyeClose, 1f);
            consciousnessMat.SetFloat(idBlur, 0f);
            consciousnessMat.SetFloat(idRadial, 0f);
            consciousnessMat.SetFloat(idDouble, 0f);
            consciousnessMat.SetFloat(idChroma, 0f);
            consciousnessMat.SetFloat(idVignette, 1f);
            consciousnessMat.SetFloat(idEyeSoftness, 0.3f);
        });
        activeSeq.AppendInterval(0.01f);
        activeSeq.OnComplete(() => activeSeq = null);
        activeSeq.Play();
    }

    public void SetPreDarkBlinkSettings(int blinkCount, float onDuration, float offDuration, float blendDuringBlink = 0.6f)
    {
        preDarkBlinkCount = Mathf.Max(0, blinkCount);
        preDarkBlinkOnDuration = Mathf.Max(0f, onDuration);
        preDarkBlinkOffDuration = Mathf.Max(0f, offDuration);
        preDarkBlinkBlend = Mathf.Clamp01(blendDuringBlink);
        if (preDarkBlinkBlend < 0.05f)
            preDarkBlinkBlend = 0.05f;
    }

    public void SetBlinkTiming(float closeDur, float openDur, float holdTime = 0.02f)
    {
        blinkCloseDuration = Mathf.Max(0.01f, closeDur);
        blinkOpenDuration = Mathf.Max(0.01f, openDur);
        blinkHoldTime = Mathf.Max(0f, holdTime);
    }

    private void AppendBlinkSequence(Sequence seq, Material mat, int count, float onDuration, float offDuration)
    {
        for (int i = 0; i < count; i++)
        {
            seq.AppendCallback(() => SetBlendImmediate(mat, 1f));
            seq.AppendInterval(Mathf.Max(0f, onDuration));
            seq.AppendCallback(() => SetBlendImmediate(mat, 0f));
            seq.AppendInterval(Mathf.Max(0f, offDuration));
        }
    }

    private void SetBlendImmediate(Material mat, float value)
    {
        if (mat == null) return;
        value = Mathf.Clamp01(value);
        mat.SetFloat(idBlend, value);

        if (mat == consciousnessMat)
        {
            if (value <= (0f + 1e-6f))
            {
                mat.SetFloat(idBlur, 0f);
                mat.SetFloat(idRadial, 0f);
                mat.SetFloat(idDouble, 0f);
                mat.SetFloat(idChroma, 0f);
                mat.SetFloat(idVignette, 0.25f);
                mat.SetFloat(idEyeClose, 0f);
                mat.SetFloat(idEyeSoftness, 0.15f);
                mat.SetFloat(idDoubleOffset, 8f);
                mat.SetFloat(idWobble, 0f);
            }
        }
    }

    private void KillActiveSequence()
    {
        if (activeSeq != null)
        {
            activeSeq.Kill();
            activeSeq = null;
        }

        // Stop glitch audio if sequence is killed
        if (AudioManager.Instance != null && AudioManager.Instance.IsGlitchPlaying())
        {
            AudioManager.Instance.StopGlitchLoop();
        }

        if (sprintDisabledByController)
        {
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.SetDisableSprint(false);
            }
            sprintDisabledByController = false;
        }
    }

    public bool IsInNeonDimension() => isInNeonDimension;

    public void SafeExitNeonDimension()
    {
        if (isInNeonDimension)
        {
            ReturnToNormalInstant();
        }
    }

    public bool IsAudioSetupValid()
    {
        return AudioManager.Instance != null;
    }
}