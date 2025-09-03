using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Light))]
public class FlickeringLight : MonoBehaviour
{
    public enum Mode { Random, PerlinNoise, Sine, Curve }

    [Header("Light Settings")]
    public Light targetLight;

    [Tooltip("Minimum intensity the light can reach.")]
    public float minIntensity = 0f;
    [Tooltip("Maximum intensity the light can reach.")]
    public float maxIntensity = 1.5f;

    [Header("Flicker Control")]
    [Tooltip("How fast the flicker effect runs")]
    public float speed = 1f;
    [Tooltip("How 'wild' random flickers can be. 0 = smooth, larger = more variation.")]
    [Range(0f, 1f)]
    public float randomness = 0.25f;

    [Tooltip("Choose a flicker mode.")]
    public Mode mode = Mode.Random;

    [Tooltip("Use an AnimationCurve (0..1 X range). X = time normalized, Y = intensity factor 0..1.")]
    public AnimationCurve intensityCurve = AnimationCurve.EaseInOut(0, 0.5f, 1, 0.5f);

    [Header("Runtime")]
    public bool playOnStart = true;
    public bool affectRange = false;
    public float minRange = 0.5f;
    public float maxRange = 10f;

    [Header("Events")]
    public UnityEvent onFlickerStart;
    public UnityEvent onFlickerStop;
    private Coroutine flickerCoroutine;
    private float baseIntensity;
    private float baseRange;

    void Reset()
    {
        targetLight = GetComponent<Light>();
        if (targetLight) baseIntensity = targetLight.intensity;
    }

    void Awake()
    {
        if (targetLight == null) targetLight = GetComponent<Light>();
        if (targetLight == null)
        {
            Debug.LogError("FlickeringLight requires a Light component.");
            enabled = false;
            return;
        }
        baseIntensity = targetLight.intensity;
        baseRange = targetLight.range;
    }

    void Start()
    {
        if (playOnStart) StartFlicker();
    }

    public void StartFlicker()
    {
        if (flickerCoroutine != null) return;
        flickerCoroutine = StartCoroutine(FlickerLoop());
        onFlickerStart?.Invoke();
    }

    public void StopFlicker()
    {
        if (flickerCoroutine != null)
        {
            StopCoroutine(flickerCoroutine);
            flickerCoroutine = null;
        }
        ResetLightToBase();
        onFlickerStop?.Invoke();
    }

    private void ResetLightToBase()
    {
        if (targetLight == null) return;
        targetLight.intensity = baseIntensity;
        if (affectRange) targetLight.range = baseRange;
    }

    private IEnumerator FlickerLoop()
    {
        float time = 0f;

        float seed = Random.Range(0f, 1000f);

        while (true)
        {
            time += Time.deltaTime * speed;

            float t = 0f;
            switch (mode)
            {
                case Mode.Random:
                    t = Mathf.Lerp(t, Random.Range(0f, 1f), randomness > 0f ? randomness : Time.deltaTime * speed);
                    t = Mathf.Clamp01(Mathf.PerlinNoise(seed + time * 0.5f, 0f) * (1f - randomness) + Random.Range(0f, 1f) * randomness);
                    break;

                case Mode.PerlinNoise:
                    t = Mathf.PerlinNoise(seed + time, seed + time * 0.5f);
                    t = Mathf.Clamp01(t + (Random.Range(-randomness, randomness) * 0.15f));
                    break;

                case Mode.Sine:
                    t = (Mathf.Sin(time * Mathf.PI * 2f) + 1f) * 0.5f; // 0..1
                    t = Mathf.Clamp01(Mathf.Lerp(t, Random.Range(0f, 1f), randomness * 0.1f));
                    break;

                case Mode.Curve:
                    float loopT = time - Mathf.Floor(time);
                    t = intensityCurve.Evaluate(loopT);
                    t = Mathf.Clamp01(Mathf.Lerp(t, Random.Range(0f, 1f), randomness * 0.2f));
                    break;
            }

            float newIntensity = Mathf.Lerp(minIntensity, maxIntensity, t);
            targetLight.intensity = newIntensity;

            if (affectRange)
            {
                float newRange = Mathf.Lerp(minRange, maxRange, t);
                targetLight.range = newRange;
            }

            yield return null;
        }
    }

    public void SetSpeed(float newSpeed) => speed = Mathf.Max(0f, newSpeed);
    public void SetIntensityRange(float minI, float maxI)
    {
        minIntensity = Mathf.Min(minI, maxI);
        maxIntensity = Mathf.Max(minI, maxI);
    }

    void OnValidate()
    {
        if (minIntensity < 0f) minIntensity = 0f;
        if (maxIntensity < 0f) maxIntensity = 0f;
        if (maxIntensity < minIntensity) maxIntensity = minIntensity;
        speed = Mathf.Max(0f, speed);
        randomness = Mathf.Clamp01(randomness);
        if (targetLight == null) targetLight = GetComponent<Light>();
    }

    private void OnEnable()
    {
        StartFlicker();
    }

    private void OnDisable()
    {
        StopFlicker();
    }
}
