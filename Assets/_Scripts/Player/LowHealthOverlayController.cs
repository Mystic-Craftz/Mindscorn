using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LowHealthEffectController : MonoBehaviour
{
    [Header("Renderer References")]
    [SerializeField] private UniversalRendererData rendererData;
    [SerializeField] private string featureName = "LowHealth";

    [Header("Health Settings")]
    [SerializeField] private float healthThreshold = 0.3f;

    private ScriptableRendererFeature lowHealthFeature;
    private bool isEffectActive = false;

    void Start()
    {
        InitializeFeature();

        SetEffectActive(false);
    }

    void Update()
    {
        if (PlayerHealth.Instance == null) return;

        float healthPercent = PlayerHealth.Instance.GetCurrentHealth() / PlayerHealth.Instance.GetMaxHealth();
        bool shouldBeActive = healthPercent <= healthThreshold;

        // Only toggle if state changed
        if (shouldBeActive != isEffectActive)
        {
            SetEffectActive(shouldBeActive);
        }
    }

    private void InitializeFeature()
    {
        if (rendererData == null)
        {
            Debug.LogError("RendererData is not assigned!", this);
            enabled = false;
            return;
        }

        // Find the renderer feature by name
        foreach (var feature in rendererData.rendererFeatures)
        {
            if (feature != null && feature.name == featureName)
            {
                lowHealthFeature = feature;
                break;
            }
        }

        if (lowHealthFeature == null)
        {
            Debug.LogError($"Renderer feature '{featureName}' not found!", this);
            enabled = false;
        }
    }

    private void SetEffectActive(bool active)
    {
        if (lowHealthFeature != null)
        {
            lowHealthFeature.SetActive(active);
            isEffectActive = active;

            rendererData.SetDirty();
        }
    }

    private void OnDestroy()
    {
        if (isEffectActive) SetEffectActive(false);
    }
}