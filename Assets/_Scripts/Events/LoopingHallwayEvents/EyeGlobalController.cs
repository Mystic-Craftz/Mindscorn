using UnityEngine;

public class EyeGlobalController : MonoBehaviour
{
    [Header("Defaults")]
    public float defaultDuration = -1f;
    public float defaultAmplitude = 10f;
    public float defaultFrequency = 4f;

    [Header("Sync")]
    public float defaultSyncDuration = 1f;
    public float defaultDesyncSeedSpread = 200f;

    private bool lastToggleState = false;

    // start without sync
    public void StartCreepyAll()
    {
        Eye.GlobalSyncSeed = 0f;
        Eye.GlobalSyncEndTime = 0f;
        Eye.GlobalDesyncSeedSpread = 0f;
        Eye.OnGlobalStartCreepy?.Invoke(defaultDuration, defaultAmplitude, defaultFrequency);
    }

    // custom params
    public void StartCreepyAll_Custom(float duration, float amplitude, float frequency)
    {
        Eye.GlobalSyncSeed = 0f;
        Eye.GlobalSyncEndTime = 0f;
        Eye.GlobalDesyncSeedSpread = 0f;
        Eye.OnGlobalStartCreepy?.Invoke(duration, amplitude, frequency);
    }

    // start with sync then desync (uses defaults)
    public void StartCreepyAll_WithSync()
    {
        StartCreepyAll_WithSync(defaultDuration, defaultAmplitude, defaultFrequency, defaultSyncDuration, defaultDesyncSeedSpread);
    }

    // fully custom sync call
    public void StartCreepyAll_WithSync(float duration, float amplitude, float frequency, float syncDuration, float desyncSeedSpread)
    {
        Eye.GlobalSyncSeed = UnityEngine.Random.value * 1000f;
        Eye.GlobalSyncEndTime = Time.time + Mathf.Max(0f, syncDuration);
        Eye.GlobalDesyncSeedSpread = Mathf.Max(0f, desyncSeedSpread);

        Eye.OnGlobalStartCreepy?.Invoke(duration, amplitude, frequency);
    }

    public void StopCreepyAll()
    {
        Eye.OnGlobalStopCreepy?.Invoke();
        Eye.GlobalSyncSeed = 0f;
        Eye.GlobalSyncEndTime = 0f;
        Eye.GlobalDesyncSeedSpread = 0f;
    }

    public void ToggleCreepyAll()
    {
        lastToggleState = !lastToggleState;
        if (lastToggleState) StartCreepyAll();
        else StopCreepyAll();
    }

    public void SetLookAll(bool shouldLook)
    {
        Eye.OnGlobalSetLook?.Invoke(shouldLook);
    }
}
