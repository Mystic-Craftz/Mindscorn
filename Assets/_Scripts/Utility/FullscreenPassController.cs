using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Reflection;

public class FullscreenPassController : MonoBehaviour
{
    [Header("URP Renderer")]
    public UniversalRendererData rendererData;
    public string featureName = "FullScreenPassRendererFeature";

    [Header("Debug")]
    public bool debugLogs = false;

    private Material passMaterial;
    private float originalBlurOffset = 0f;
    private bool originalStored = false;

    private bool isRunning = false;

    void Start()
    {
        // find the renderer feature and extract passMaterial via reflection
        if (rendererData == null)
        {
            Debug.LogWarning("[FullscreenPassController] rendererData is null. Assign your URP renderer asset.");
            return;
        }

        foreach (var feature in rendererData.rendererFeatures)
        {
            if (feature == null) continue;
            if (feature.name != featureName) continue;

            var t = feature.GetType();
            if (debugLogs) Debug.Log($"Feature found: {feature.name}  Type: {t.FullName}");

            var fields = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var f in fields)
            {
                object value = null;
                try { value = f.GetValue(feature); } catch { value = "<get failed>"; }
                if (f.Name == "passMaterial")
                {
                    passMaterial = value as Material;
                    if (debugLogs) Debug.Log("passMaterial found and assigned.");
                }
            }
        }

        if (passMaterial == null)
        {
            Debug.LogWarning("[FullscreenPassController] passMaterial not found. Make sure featureName is correct and the renderer has that feature.");
            return;
        }
        passMaterial.SetFloat("_BlurOffset", (0.7f));
    }

    void Update()
    {
        if (passMaterial == null) return;

        if (isRunning)
        {
            if (debugLogs)
                Debug.Log(passMaterial.GetFloat("_BlurOffset"));

            passMaterial.SetFloat("_BlurOffset", Mathf.Lerp(0.7f, 10f, Mathf.PingPong(Time.time, 1f)));
        }
    }

    public void PlayEffect()
    {
        if (!originalStored)
        {
            originalBlurOffset = passMaterial.GetFloat("_BlurOffset");
            originalStored = true;
        }
        isRunning = true;
    }

    public void StopEffect()
    {
        if (originalStored)
        {
            passMaterial.SetFloat("_BlurOffset", originalBlurOffset);
        }
        isRunning = false;
    }

}
