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
    [SerializeField, Min(0f)] private float preDarkBlinkOnDuration = 0.06f;  // used for legacy API; not directly for smooth blink timings
    [SerializeField, Min(0f)] private float preDarkBlinkOffDuration = 0.06f;
    [SerializeField, Range(0.05f, 1f)] private float preDarkBlinkBlend = 0.6f; // temporary blend during pre-blinks

    [Header("Smooth blink tuning")]
    [SerializeField, Range(0.5f, 1f)] private float blinkCloseValue = 0.95f; // how closed the eye is at peak blink (0..1)
    [SerializeField, Min(0.01f)] private float blinkCloseDuration = 0.09f;    // time to close eye
    [SerializeField, Min(0.01f)] private float blinkOpenDuration = 0.12f;     // time to open eye
    [SerializeField, Min(0f)] private float blinkHoldTime = 0.02f;            // small hold at fully closed
    [SerializeField] private Ease blinkCloseEase = Ease.InSine;               // closing ease
    [SerializeField] private Ease blinkOpenEase = Ease.OutSine;              // opening ease

    [Header("Wake settings")]
    [SerializeField, Min(0f)] private float wakeDuration = 1.4f;
    [SerializeField] private Color fadeColor = Color.black;
    [SerializeField, Min(0f)] private float wakeDefaultOpenDuration = 1.4f;
    [SerializeField, Min(0f)] private float wakeDefaultBlendFade = 0.35f;

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

    void Awake()
    {
        // Singleton
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }

        DOTween.Init(false, true, LogBehaviour.ErrorsOnly);

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

        if (neonMaterial != null) neonMaterial.SetFloat(idBlend, 0f);
        if (glitchMaterial != null) glitchMaterial.SetFloat(idBlend, 0f);

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


    public void EnterNeonDimension(int blinks = -1)
    {
        if (neonMaterial == null) { Debug.LogWarning("NeonDimensionController: neonMaterial not assigned."); return; }

        int actualBlinks = (blinks > 0) ? blinks : neonBlinkCount;
        KillActiveSequence();

        activeSeq = DOTween.Sequence();

        if (glitchMaterial != null)
        {
            AppendBlinkSequence(activeSeq, glitchMaterial, actualBlinks, neonBlinkOnDuration, neonBlinkOffDuration);
            activeSeq.AppendCallback(() => SetBlendImmediate(glitchMaterial, 0f));
        }

        activeSeq.AppendCallback(() =>
        {
            SetBlendImmediate(neonMaterial, 1f);
            isInNeonDimension = true;
        });

        activeSeq.OnComplete(() => activeSeq = null);
        activeSeq.Play();
    }

    public void ReturnToNormalInstant()
    {
        KillActiveSequence();
        if (neonMaterial != null) SetBlendImmediate(neonMaterial, 0f);
        if (glitchMaterial != null) SetBlendImmediate(glitchMaterial, 0f);
        isInNeonDimension = false;
    }

    public void PlayGlitch(int blinks = -1)
    {
        if (glitchMaterial == null) { Debug.LogWarning("NeonDimensionController: glitchMaterial not assigned."); return; }

        int actualBlinks = (blinks > 0) ? blinks : generalBlinkCount;
        KillActiveSequence();

        activeSeq = DOTween.Sequence();
        AppendBlinkSequence(activeSeq, glitchMaterial, actualBlinks, generalBlinkOnDuration, generalBlinkOffDuration);
        activeSeq.AppendCallback(() => SetBlendImmediate(glitchMaterial, 0f));
        activeSeq.OnComplete(() => activeSeq = null);
        activeSeq.Play();
    }


    //Consciousness stuff

    public void StartBlink(bool resetBlendAfter = true, float optionalBlend = -1f)
    {
        if (consciousnessMat == null) { Debug.LogWarning("NeonDimensionController: consciousnessMat not assigned."); return; }
        KillActiveSequence();

        float blendToUse = (optionalBlend > 0f) ? optionalBlend : preDarkBlinkBlend;
        blendToUse = Mathf.Clamp(blendToUse, 0.05f, 1f);

        activeSeq = DOTween.Sequence();

        // Ensure shader active at a small blend so eyes (eye mask) are visible during blink
        activeSeq.AppendCallback(() => SetBlendImmediate(consciousnessMat, blendToUse));

        // Close
        activeSeq.Append(DOTween.To(() => consciousnessMat.GetFloat(idEyeClose), x => consciousnessMat.SetFloat(idEyeClose, x), blinkCloseValue, blinkCloseDuration)
            .SetEase(blinkCloseEase));

        // Small hold
        if (blinkHoldTime > 0f) activeSeq.AppendInterval(blinkHoldTime);

        // Open
        activeSeq.Append(DOTween.To(() => consciousnessMat.GetFloat(idEyeClose), x => consciousnessMat.SetFloat(idEyeClose, x), 0f, blinkOpenDuration)
            .SetEase(blinkOpenEase));

        // Optionally reset blend to 0 (i.e., effect off) after blink
        if (resetBlendAfter)
        {
            activeSeq.AppendCallback(() => SetBlendImmediate(consciousnessMat, 0f));
        }

        activeSeq.OnComplete(() => activeSeq = null);
        activeSeq.Play();
    }



    public void LoseConsciousness(float duration = -1f)
    {
        if (consciousnessMat == null) { Debug.LogWarning("NeonDimensionController: consciousnessMat not assigned."); return; }

        duration = (duration > 0f) ? duration : consciousnessDuration;
        KillActiveSequence();
        activeSeq = DOTween.Sequence();

        // Ensure shader is on for pre-blinks with a visible blend
        activeSeq.AppendCallback(() => SetBlendImmediate(consciousnessMat, preDarkBlinkBlend));

        // Pre-dark smooth blinks
        for (int i = 0; i < preDarkBlinkCount; i++)
        {
            // Close
            activeSeq.Append(DOTween.To(() => consciousnessMat.GetFloat(idEyeClose), x => consciousnessMat.SetFloat(idEyeClose, x), blinkCloseValue, blinkCloseDuration).SetEase(blinkCloseEase));
            // Hold
            if (blinkHoldTime > 0f) activeSeq.AppendInterval(blinkHoldTime);
            // Open
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

        activeSeq.OnComplete(() => { activeSeq = null; });
        activeSeq.Play();
    }

    //wraper for unity event
    public void WakeUp()
    {
        // Calls the existing method with defaults
        WakeUp(wakeDefaultOpenDuration, wakeDefaultBlendFade);
    }

    // reset to default state
    public void WakeUp(float openDuration = -1f, float blendFadeDuration = 0.35f)
    {
        if (consciousnessMat == null) { Debug.LogWarning("NeonDimensionController: consciousnessMat not assigned."); return; }
        KillActiveSequence();

        float openDur = (openDuration > 0f) ? openDuration : wakeDuration;
        openDur = Mathf.Max(0.01f, openDur);
        blendFadeDuration = Mathf.Max(0f, blendFadeDuration);

        activeSeq = DOTween.Sequence();

        // Ensure shader is active and start from the 'unconscious' eye state so opening looks correct
        activeSeq.AppendCallback(() =>
        {
            // Ensure blend is on so the eye mask is visible while opening
            SetBlendImmediate(consciousnessMat, 1f);

            // If eyes are not already partially closed, put them in the typical unconscious end pose
            float currentEyeClose = consciousnessMat.GetFloat(idEyeClose);
            if (currentEyeClose < 0.5f)
                consciousnessMat.SetFloat(idEyeClose, 0.52f);

            // Keep heavy properties as-is (they will be cleared when blend -> 0)
        });

        // Open eyes slowly (smooth)
        activeSeq.Append(
            DOTween.To(() => consciousnessMat.GetFloat(idEyeClose),
                       x => consciousnessMat.SetFloat(idEyeClose, x),
                       0f,
                       openDur
            ).SetEase(blinkOpenEase)
        );

        // tiny buffer
        activeSeq.AppendInterval(0.05f);

        // Fade blend to 0 (this will trigger SetBlendImmediate cleanup behavior)
        if (blendFadeDuration > 0f)
        {
            activeSeq.Append(
                DOTween.To(() => consciousnessMat.GetFloat(idBlend),
                           x => consciousnessMat.SetFloat(idBlend, x),
                           0f,
                           blendFadeDuration
                )
            );
        }
        else
        {
            // immediate
            activeSeq.AppendCallback(() => SetBlendImmediate(consciousnessMat, 0f));
        }

        // Final cleanup: enforce immediate reset (guards against tiny float leftovers)
        activeSeq.AppendCallback(() => SetBlendImmediate(consciousnessMat, 0f));

        activeSeq.OnComplete(() => activeSeq = null);
        activeSeq.Play();
    }



    public void SetPreDarkBlinkSettings(int blinkCount, float onDuration, float offDuration, float blendDuringBlink = 0.6f)
    {
        preDarkBlinkCount = Mathf.Max(0, blinkCount);
        preDarkBlinkOnDuration = Mathf.Max(0f, onDuration);
        preDarkBlinkOffDuration = Mathf.Max(0f, offDuration);
        preDarkBlinkBlend = Mathf.Clamp01(blendDuringBlink);
        if (preDarkBlinkBlend < 0.05f) preDarkBlinkBlend = 0.05f; // ensure >0 so the shader runs during blinks
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

        // If it's the consciousness material and you're turning it off, zero out heavy properties
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
    }

    public bool IsInNeonDimension() => isInNeonDimension;
}
