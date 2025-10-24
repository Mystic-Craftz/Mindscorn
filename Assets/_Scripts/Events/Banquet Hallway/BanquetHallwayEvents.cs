using UnityEngine;
using System.Collections;

public class BanquetHallwayEvents : MonoBehaviour
{
    public GameObject enemyWave1;
    public GameObject enemyWave2;
    public GameObject enemyWave3;

    [Tooltip("Seconds between waves.")]
    public float waveInterval = 5f;

    [Tooltip("How many glitch blinks to play before each wave. Passed to NeonDimensionController.PlayGlitch().")]
    public int glitchBlinks = 3;

    [Tooltip("If true, assigned wave GameObjects will be deactivated in Start so they only appear when triggered.")]
    public bool deactivateWavesOnStart = false;

    private Coroutine wavesRoutine;

    void Start()
    {
        if (deactivateWavesOnStart)
        {
            if (enemyWave1 != null) enemyWave1.SetActive(false);
            if (enemyWave2 != null) enemyWave2.SetActive(false);
            if (enemyWave3 != null) enemyWave3.SetActive(false);
        }
    }

    // Triggers the wave sequence coroutine.
    public void TriggerWavesSequence()
    {
        if (wavesRoutine != null)
        {
            StopCoroutine(wavesRoutine);
            wavesRoutine = null;
        }

        wavesRoutine = StartCoroutine(WavesCoroutine(waveInterval));
    }

    // Cancels any running wave sequence coroutine immediately.
    public void CancelWaves()
    {
        if (wavesRoutine != null)
        {
            StopCoroutine(wavesRoutine);
            wavesRoutine = null;
        }
    }

    // Coroutine for spawning waves of enemies and respecting the game's timescale.
    private IEnumerator WavesCoroutine(float interval)
    {
        GameObject[] waves = new GameObject[] { enemyWave1, enemyWave2, enemyWave3 };

        // If the game is currently paused, wait until it is resumed before starting.
        if (Time.timeScale <= 0f)
        {
            yield return new WaitUntil(() => Time.timeScale > 0f);
        }

        for (int i = 0; i < waves.Length; i++)
        {
            GameObject w = waves[i];
            if (w == null)
                continue;

            if (NeonDimensionController.Instance != null)
            {
                NeonDimensionController.Instance.PlayGlitch(glitchBlinks);
            }
            else
            {
                Debug.LogWarning($"BanquetHallwayEvents: NeonDimensionController.Instance is null; cannot play glitch for wave {i + 1}.");
            }

            // Give one frame for the glitch to begin.
            yield return null;

            // If paused during that frame, wait until unpaused before spawning.
            if (Time.timeScale <= 0f)
            {
                yield return new WaitUntil(() => Time.timeScale > 0f);
                yield return null;
            }

            w.SetActive(true);

            // If not the last wave, wait using scaled time so timers pause with the game's timescale.
            if (i < waves.Length - 1)
            {
                yield return new WaitForSeconds(Mathf.Max(0f, interval));
            }
        }

        wavesRoutine = null;
    }
}
