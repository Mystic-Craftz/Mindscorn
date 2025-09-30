using UnityEngine;
using DG.Tweening; // DOTween
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

    [Header("Consciousness settings")]
    [SerializeField] private float duration = 2f;



    private readonly int blendId = Shader.PropertyToID("_Blend");
    private Sequence activeSeq;
    private bool isInNeonDimension = false;
    private int blendID_Consciousness;

    void Awake()
    {

        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        DOTween.Init(false, true, LogBehaviour.ErrorsOnly);

        if (neonMaterial != null) neonMaterial.SetFloat(blendId, 0f);
        if (glitchMaterial != null) glitchMaterial.SetFloat(blendId, 0f);
        if (consciousnessMat != null) blendID_Consciousness = Shader.PropertyToID("_Blend");

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


    // Play general glitch snaps using the GENERAL settings
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
        AppendBlinkSequence(activeSeq, glitchMaterial, actualBlinks, generalBlinkOnDuration, generalBlinkOffDuration);
        activeSeq.AppendCallback(() => SetBlendImmediate(glitchMaterial, 0f));
        activeSeq.OnComplete(() => activeSeq = null);
        activeSeq.Play();
    }

    public void SetGeneralBlinkSettings(int blinkCount, float onDuration, float offDuration)
    {
        generalBlinkCount = Mathf.Max(1, blinkCount);
        generalBlinkOnDuration = Mathf.Max(0f, onDuration);
        generalBlinkOffDuration = Mathf.Max(0f, offDuration);
    }

    public void SetNeonBlinkSettings(int blinkCount, float onDuration, float offDuration)
    {
        neonBlinkCount = Mathf.Max(1, blinkCount);
        neonBlinkOnDuration = Mathf.Max(0f, onDuration);
        neonBlinkOffDuration = Mathf.Max(0f, offDuration);
    }

    // for consciousness
    public void FadeToBlack()
    {
        if (consciousnessMat == null) return;
        consciousnessMat.DOFloat(1f, blendID_Consciousness, duration);
    }

    public void FadeFromBlack()
    {
        if (consciousnessMat == null) return;
        consciousnessMat.DOFloat(0f, blendID_Consciousness, duration);
    }


    // Helpers 

    // Append an instant 0->1->0 blink sequence (repeated count times) to a DOTween Sequence
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
        mat.SetFloat(blendId, Mathf.Clamp01(value));
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
