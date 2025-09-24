using UnityEngine;

public class Eye : MonoBehaviour
{
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
    private float noiseSeed;

    private bool lastEnableCreepyToggle;

    private void Start()
    {
        cameraTransform = Camera.main ? Camera.main.transform : null;
        noiseSeed = Random.value * 1000f;
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

        if (!shouldLook || cameraTransform == null) return;

        // Base look rotation
        Vector3 dir = (cameraTransform.position - transform.position).normalized;
        if (dir.sqrMagnitude < 0.0001f) return;
        Quaternion targetRotation = Quaternion.LookRotation(dir, Vector3.up);

        // Compute yaw offset if shaking
        float yawOffset = 0f;
        if (isShaking)
        {
            float t = Time.time - shakeStartTime;
            // if one-shot and finished, stop
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

    private void OnDisable()
    {
        isShaking = false;
    }


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


    public void StopCreepyShake()
    {
        isShaking = false;
        shakeDuration = 0f;
        enableCreepyToggle = false;
        lastEnableCreepyToggle = false;
    }

    public void StartLookingAtPlayer() => shouldLook = true;
    public void StopLookingAtPlayer() => shouldLook = false;


    public void StartDoingIt()
    {
        enableCreepyToggle = true;
    }

    public void StopDoingIt()
    {
        enableCreepyToggle = false;
    }

    public void ToggleDoingIt()
    {
        enableCreepyToggle = !enableCreepyToggle;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            return;
        }
    }
#endif
}
