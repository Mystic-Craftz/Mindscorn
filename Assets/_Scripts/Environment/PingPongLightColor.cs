using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Light))]
public class PingPongLightColor : MonoBehaviour
{
    [Header("Colors")]
    [SerializeField] private Color colorA = Color.white; // start color
    [SerializeField] private Color colorB = Color.red;   // target color

    [Header("Timing (seconds)")]
    [SerializeField, Min(0f)] private float transitionTime = 1.5f; // time to lerp between colors
    [SerializeField, Min(0f)] private float holdTime = 0.5f;       // how long to keep each color before returning
    [SerializeField, Min(0f)] private float startDelay = 0f;       // delay before starting effect

    [Header("Behavior")]
    [SerializeField] private bool startOnAwake = true;  // start automatically
    [SerializeField] private bool loop = true;          // repeat forever
    [SerializeField] private bool useUnscaledTime = false; // ignore Time.timeScale if true

    private Light _light;
    private Coroutine _routine;
    private bool _running = false;

    private void Awake()
    {
        _light = GetComponent<Light>();
        if (_light == null)
        {
            Debug.LogError("PingPongLightColor requires a Light component.");
            enabled = false;
            return;
        }

        _light.color = colorA;
    }

    private void Start()
    {
        if (startOnAwake) StartEffect();
    }


    // Starts the ping-pong color effect. If already running, restarts it.   
    public void StartEffect()
    {
        StopEffect();
        _routine = StartCoroutine(PingPongRoutine());
    }


    // Stops the color effect and resets to colorA.
    public void StopEffect()
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }
        _running = false;
        if (_light != null) _light.color = colorA;
    }

    private IEnumerator PingPongRoutine()
    {
        _running = true;

        if (startDelay > 0f)
            yield return Wait(startDelay);

        do
        {

            yield return LerpColor(colorA, colorB, transitionTime);


            if (holdTime > 0f)
                yield return Wait(holdTime);


            yield return LerpColor(colorB, colorA, transitionTime);


            if (holdTime > 0f)
                yield return Wait(holdTime);

        } while (loop && _running);
    }

    private IEnumerator LerpColor(Color from, Color to, float duration)
    {
        if (duration <= 0f)
        {
            _light.color = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration && _running)
        {
            float t = elapsed / duration;
            _light.color = Color.Lerp(from, to, t);
            elapsed += DeltaTime();
            yield return null;
        }
        if (_running) _light.color = to;
    }

    private float DeltaTime()
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }

    private IEnumerator Wait(float seconds)
    {
        if (seconds <= 0f) yield break;
        float elapsed = 0f;
        while (elapsed < seconds && _running)
        {
            elapsed += DeltaTime();
            yield return null;
        }
    }

    private void OnDisable()
    {
        _running = false;
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }
    }
}
