using UnityEngine;
using System.Collections;

public class NeonDimensionController : MonoBehaviour
{
    [Header("Material Reference")]
    [SerializeField] private Material neonMaterial;

    [Header("Transition Settings")]
    [SerializeField] private float transitionDuration = 2.0f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Default Shader Values")]
    [SerializeField] private Color defaultEdgeColor = Color.cyan;
    [SerializeField] private float defaultSceneDarkness = 0.8f;
    [SerializeField] private float defaultEdgeThreshold = 0.1f;
    [SerializeField] private float defaultDepthWeight = 1f;
    [SerializeField] private float defaultColorWeight = 1f;
    [SerializeField] private float defaultSampleRadius = 1f;

    private bool isInNeonDimension = false;
    private Coroutine transitionCoroutine;

    void Start()
    {
        if (neonMaterial != null)
        {
            neonMaterial.SetColor("_EdgeColor", defaultEdgeColor);
            neonMaterial.SetFloat("_SceneDarkness", defaultSceneDarkness);
            neonMaterial.SetFloat("_EdgeThreshold", defaultEdgeThreshold);
            neonMaterial.SetFloat("_DepthWeight", defaultDepthWeight);
            neonMaterial.SetFloat("_ColorWeight", defaultColorWeight);
            neonMaterial.SetFloat("_SampleRadius", defaultSampleRadius);
            neonMaterial.SetFloat("_Blend", 0f);
        }
        else
        {
            Debug.LogWarning("Neon material not assigned to NeonDimensionController");
        }
    }

    public void ToggleDimension()
    {
        if (isInNeonDimension)
        {
            ReturnToNormalDimension();
        }
        else
        {
            EnterNeonDimension();
        }
    }

    public void EnterNeonDimension()
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }
        transitionCoroutine = StartCoroutine(TransitionToNeon(transitionDuration));
    }

    public void ReturnToNormalDimension()
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }
        transitionCoroutine = StartCoroutine(TransitionToNormal(transitionDuration));
    }

    private IEnumerator TransitionToNeon(float duration)
    {
        isInNeonDimension = true;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = transitionCurve.Evaluate(elapsed / duration);

            if (neonMaterial != null)
            {
                neonMaterial.SetFloat("_Blend", t);
            }

            yield return null;
        }


        if (neonMaterial != null)
        {
            neonMaterial.SetFloat("_Blend", 1f);
        }

        transitionCoroutine = null;
    }

    private IEnumerator TransitionToNormal(float duration)
    {
        isInNeonDimension = false;

        float elapsed = 0f;
        float startBlend = neonMaterial != null ? neonMaterial.GetFloat("_Blend") : 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = transitionCurve.Evaluate(1f - (elapsed / duration));

            if (neonMaterial != null)
            {
                neonMaterial.SetFloat("_Blend", Mathf.Lerp(0f, startBlend, t));
            }

            yield return null;
        }


        if (neonMaterial != null)
        {
            neonMaterial.SetFloat("_Blend", 0f);
        }

        transitionCoroutine = null;
    }


    public void SetEdgeColor(Color color)
    {
        if (neonMaterial != null) neonMaterial.SetColor("_EdgeColor", color);
    }

    public void SetSceneDarkness(float darkness)
    {
        if (neonMaterial != null) neonMaterial.SetFloat("_SceneDarkness", darkness);
    }

    public void SetEdgeThreshold(float threshold)
    {
        if (neonMaterial != null) neonMaterial.SetFloat("_EdgeThreshold", threshold);
    }

    public void SetDepthWeight(float weight)
    {
        if (neonMaterial != null) neonMaterial.SetFloat("_DepthWeight", weight);
    }

    public void SetColorWeight(float weight)
    {
        if (neonMaterial != null) neonMaterial.SetFloat("_ColorWeight", weight);
    }

    public void SetSampleRadius(float radius)
    {
        if (neonMaterial != null) neonMaterial.SetFloat("_SampleRadius", radius);
    }


    public bool IsInNeonDimension()
    {
        return isInNeonDimension;
    }

    public Material GetNeonMaterial()
    {
        return neonMaterial;
    }
}