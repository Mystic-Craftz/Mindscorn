using Unity.Cinemachine;
using UnityEngine;

public class ScreenShakeManager : MonoBehaviour
{
    /// <summary>
    /// This class manages the screen shaking effect. It is a singleton and therefor
    /// should be accessed only through ScreenShakeManager.Instance.
    /// </summary>
    public static ScreenShakeManager Instance { get; private set; }
    [SerializeField] private CinemachineBasicMultiChannelPerlin screenShakeChannel;

    private float shakeTimer = 0f;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            if (shakeTimer <= 0)
            {
                screenShakeChannel.AmplitudeGain = 0;
            }
        }
    }

    public void ShakeCamera(float intensity, float duration)
    {
        screenShakeChannel.AmplitudeGain = intensity;
        shakeTimer = duration;
    }
}
