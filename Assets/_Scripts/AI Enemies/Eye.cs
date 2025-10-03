using System;
using UnityEngine;

public class Eye : MonoBehaviour
{
    // multicast fields (invokable by controller)
    public static Action<float, float, float> OnGlobalStartCreepy;
    public static Action OnGlobalStopCreepy;
    public static Action<bool> OnGlobalSetLook;

    // global sync controls (set by controller before invoke)
    public static float GlobalSyncEndTime = 0f;
    public static float GlobalSyncSeed = 0f;
    public static float GlobalDesyncSeedSpread = 0f;

    [Header("Look")]
    [SerializeField] private bool shouldStartLookingAtStart = false;
    [SerializeField] private float rotationSmoothness = 10f;

    [Header("Creepy Shake")]
    [SerializeField] private bool enableCreepyToggle = false;
    [SerializeField] private float defaultShakeAmplitude = 10f;
    [SerializeField] private float defaultShakeFrequency = 4f;
    [SerializeField] private bool startShakingOnStart = false;

    private Transform cameraTransform;
    private bool shouldLook = false;

    private bool isShaking = false;
    private float shakeAmplitude;
    private float shakeFrequency;
    private float shakeStartTime;
    private float shakeDuration;
    private float noiseSeed = 0f;

    private bool lastEnableCreepyToggle;
    private bool pendingDesync = false;
    private float localSyncEndTime = 0f;
    private float localDesyncSeedSpread = 0f;

    private void OnEnable()
    {
        OnGlobalStartCreepy += HandleGlobalStartCreepy;
        OnGlobalStopCreepy += HandleGlobalStopCreepy;
        OnGlobalSetLook += HandleGlobalSetLook;
    }

    private void OnDisable()
    {
        OnGlobalStartCreepy -= HandleGlobalStartCreepy;
        OnGlobalStopCreepy -= HandleGlobalStopCreepy;
        OnGlobalSetLook -= HandleGlobalSetLook;
        isShaking = false;
        pendingDesync = false;
    }

    private void Start()
    {
        cameraTransform = Camera.main ? Camera.main.transform : null;
        if (noiseSeed == 0f) noiseSeed = UnityEngine.Random.value * 1000f;
        lastEnableCreepyToggle = enableCreepyToggle;

        if (shouldStartLookingAtStart)
        {
            shouldLook = true;
            if (cameraTransform) transform.LookAt(cameraTransform.position);
        }

        if (startShakingOnStart || enableCreepyToggle)
        {
            StartCreepyShake(-1f, defaultShakeAmplitude, defaultShakeFrequency);
            enableCreepyToggle = true;
            lastEnableCreepyToggle = true;
        }
    }

    private void Update()
    {
        if (enableCreepyToggle != lastEnableCreepyToggle)
        {
            if (enableCreepyToggle)
                StartCreepyShake(-1f, defaultShakeAmplitude, defaultShakeFrequency);
            else
                StopCreepyShake();

            lastEnableCreepyToggle = enableCreepyToggle;
        }

        if (pendingDesync && Time.time >= localSyncEndTime)
        {
            DoDesync();
            pendingDesync = false;
        }

        if (!shouldLook || cameraTransform == null) return;

        Vector3 dir = (cameraTransform.position - transform.position).normalized;
        if (dir.sqrMagnitude < 0.0001f) return;
        Quaternion targetRotation = Quaternion.LookRotation(dir, Vector3.up);

        float yawOffset = 0f;
        if (isShaking)
        {
            float t = Time.time - shakeStartTime;
            if (shakeDuration > 0f && t >= shakeDuration)
            {
                isShaking = false;
            }
            else
            {
                float angularSin = Mathf.Sin((Time.time + noiseSeed) * shakeFrequency * Mathf.PI * 2f) * shakeAmplitude;
                float jitter = (Mathf.PerlinNoise(noiseSeed, Time.time * (shakeFrequency * 1.5f)) - 0.5f) * 2f * (shakeAmplitude * 0.35f);
                yawOffset = angularSin + jitter;
            }
        }

        Quaternion shakenTarget = targetRotation * Quaternion.Euler(0f, yawOffset, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, shakenTarget, Time.deltaTime * rotationSmoothness);
    }

    // start shaking; negative duration = infinite
    public void StartCreepyShake(float duration = -1f, float amplitude = -1f, float frequency = -1f)
    {
        if (amplitude <= 0f) amplitude = defaultShakeAmplitude;
        if (frequency <= 0f) frequency = defaultShakeFrequency;

        shakeAmplitude = amplitude;
        shakeFrequency = frequency;
        shakeDuration = duration;
        shakeStartTime = Time.time;
        isShaking = true;

        if (duration <= 0f)
        {
            enableCreepyToggle = true;
            lastEnableCreepyToggle = true;
        }
    }

    // stop shaking
    public void StopCreepyShake()
    {
        isShaking = false;
        shakeDuration = 0f;
        enableCreepyToggle = false;
        lastEnableCreepyToggle = false;
    }

    public void StartLookingAtPlayer() => shouldLook = true;
    public void StopLookingAtPlayer() => shouldLook = false;
    public void StartDoingIt() => enableCreepyToggle = true;
    public void StopDoingIt() => enableCreepyToggle = false;
    public void ToggleDoingIt() => enableCreepyToggle = !enableCreepyToggle;

    private void HandleGlobalStartCreepy(float duration, float amplitude, float frequency)
    {
        if (GlobalSyncSeed != 0f && Time.time < GlobalSyncEndTime)
        {
            noiseSeed = GlobalSyncSeed;
            pendingDesync = true;
            localSyncEndTime = GlobalSyncEndTime;
            localDesyncSeedSpread = GlobalDesyncSeedSpread;
        }
        else
        {
            noiseSeed = UnityEngine.Random.value * 1000f;
            pendingDesync = false;
        }

        StartCreepyShake(duration, amplitude, frequency);
    }

    private void HandleGlobalStopCreepy()
    {
        StopCreepyShake();
        pendingDesync = false;
    }

    private void HandleGlobalSetLook(bool look)
    {
        shouldLook = look;
    }

    // apply small randomized differences when desyncing
    private void DoDesync()
    {
        float spread = Mathf.Max(0f, localDesyncSeedSpread);
        noiseSeed += (UnityEngine.Random.value - 0.5f) * 2f * spread;

        float freqJitter = 0.95f + UnityEngine.Random.value * 0.1f;
        float ampJitter = 0.95f + UnityEngine.Random.value * 0.1f;
        shakeFrequency *= freqJitter;
        shakeAmplitude *= ampJitter;
    }
}
