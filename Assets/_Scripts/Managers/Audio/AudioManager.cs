using System.Collections;
using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using STOP_MODE = FMOD.Studio.STOP_MODE;

public class AudioManager : MonoBehaviour, ISaveable
{
    public static AudioManager Instance { get; private set; }

    [Header("Music Use a unique int Track ID for each entry.")]
    public List<MusicEntry> musicEntries = new List<MusicEntry>();

    [Header("Optional FMOD Bus")]
    [SerializeField] private string aiBusPath = "bus:/SFX/AI";

    [Header("Heartbeat")]
    [SerializeField] private EventReference heartbeatEvent;

    [Header("Glitch & Neon")]
    [SerializeField] private EventReference glitchMultiToolEvent;
    [SerializeField] private EventReference whisperLoopEvent;
    [SerializeField] private int neonMusicTrackId = 6;

    [Header("Glitch Audio Settings")]
    [SerializeField, Min(0.01f)] private float glitchMinInterval = 0.05f;
    [SerializeField, Min(0.01f)] private float glitchMaxInterval = 0.15f;

    private Dictionary<int, EventInstance> stateSoundInstances = new Dictionary<int, EventInstance>();
    private Dictionary<int, EventInstance> resumableOneShots = new Dictionary<int, EventInstance>();
    private int nextTransientId = 1;

    // heartbeat instance
    private EventInstance heartbeatInstance = new EventInstance();
    private bool heartbeatPlaying = false;

    // music management
    [System.Serializable]
    public struct MusicEntry
    {
        public int trackId;
        public EventReference fmodEvent;
    }

    private Dictionary<int, EventReference> musicMap = new Dictionary<int, EventReference>();
    private EventInstance currentMusicInstance = new EventInstance();
    private int? currentMusicTrackId = null;
    private EventReference currentMusicRef = new EventReference();
    private Coroutine crossfadeCoroutine;

    // reference counting for tracks with multiple instances
    private Dictionary<int, int> musicRefCounts = new Dictionary<int, int>();
    private bool aiVoicesMuted = false;

    // --- neon/whisper state
    private int? savedMusicBeforeNeon = null;
    private EventInstance whisperInstance = new EventInstance();
    private bool neonAudioActive = false;
    private bool whisperPlaying = false;

    // Glitch control
    private Coroutine glitchCoroutine;
    private bool glitchPlaying = false;

    private void Awake()
    {
        Instance = this;
        musicMap.Clear();
        foreach (var e in musicEntries)
        {
            if (!musicMap.ContainsKey(e.trackId))
                musicMap[e.trackId] = e.fmodEvent;
            else
                Debug.LogWarning($"AudioManager: duplicate music trackId '{e.trackId}' in musicEntries.");
        }

        // Initialize whisper instance
        if (!whisperLoopEvent.IsNull)
        {
            whisperInstance = RuntimeManager.CreateInstance(whisperLoopEvent);
        }

        if (glitchMultiToolEvent.IsNull)
        {
            Debug.LogWarning("AudioManager: Glitch multi-tool event not assigned!");
        }
    }

    private void OnDestroy()
    {
        // Clean up instances
        if (whisperInstance.handle != null)
        {
            whisperInstance.stop(STOP_MODE.IMMEDIATE);
            whisperInstance.release();
        }

        // Stop glitch coroutine if running
        if (glitchCoroutine != null)
        {
            StopCoroutine(glitchCoroutine);
        }
    }

    private void Update()
    {
        bool shouldMuteAI = false;
        if (InventoryManager.Instance != null && InventoryManager.Instance.IsInventoryOpen)
            shouldMuteAI = true;
        if (EscapeMenuUI.Instance != null && EscapeMenuUI.Instance.IsMenuOpen)
            shouldMuteAI = true;

        if (shouldMuteAI != aiVoicesMuted)
        {
            SetAIVoicesMute(shouldMuteAI);
        }
    }

    // 3D Sound Utilities
    public EventInstance CreateInstance(EventReference sound) => RuntimeManager.CreateInstance(sound);
    public void PlayOneShot(EventReference sound, Vector3 position) => RuntimeManager.PlayOneShot(sound, position);

    // Simple direct glitch playback
    public void PlayGlitchOneShot()
    {
        if (glitchMultiToolEvent.IsNull)
        {
            return;
        }

        RuntimeManager.PlayOneShot(glitchMultiToolEvent);
    }

    public void PlayStateSound(EventReference sound, Vector3 position, int stateInstanceId)
    {
        StopStateSound(stateInstanceId);
        var inst = RuntimeManager.CreateInstance(sound);
        inst.set3DAttributes(RuntimeUtils.To3DAttributes(position));
        inst.start();
        if (aiVoicesMuted) inst.setPaused(true);
        stateSoundInstances[stateInstanceId] = inst;
    }

    public void StopStateSound(int stateInstanceId)
    {
        if (stateSoundInstances.TryGetValue(stateInstanceId, out var inst))
        {
            inst.stop(STOP_MODE.ALLOWFADEOUT);
            inst.release();
            stateSoundInstances.Remove(stateInstanceId);
        }
    }

    public void UpdateStateSoundPosition(int stateInstanceId, Vector3 position)
    {
        if (stateSoundInstances.TryGetValue(stateInstanceId, out var inst))
        {
            inst.set3DAttributes(RuntimeUtils.To3DAttributes(position));
        }
    }

    // Resumable One-Shots
    public int PlayResumableOneShot(EventReference sound, Vector3 position)
    {
        int id = nextTransientId++;
        var inst = RuntimeManager.CreateInstance(sound);
        inst.set3DAttributes(RuntimeUtils.To3DAttributes(position));
        inst.start();
        if (aiVoicesMuted) inst.setPaused(true);
        resumableOneShots[id] = inst;
        StartCoroutine(MonitorAndCleanupOneShot(id, inst));
        return id;
    }

    public void StopResumableOneShot(int id)
    {
        if (resumableOneShots.TryGetValue(id, out var inst))
        {
            inst.stop(STOP_MODE.ALLOWFADEOUT);
            inst.release();
            resumableOneShots.Remove(id);
        }
    }

    private IEnumerator MonitorAndCleanupOneShot(int id, EventInstance inst)
    {
        FMOD.Studio.PLAYBACK_STATE state;
        while (true)
        {
            var result = inst.getPlaybackState(out state);
            if (result != FMOD.RESULT.OK || state == FMOD.Studio.PLAYBACK_STATE.STOPPED || state == FMOD.Studio.PLAYBACK_STATE.STOPPING)
                break;
            yield return null;
        }

        if (resumableOneShots.TryGetValue(id, out var tracked) && tracked.handle != null)
        {
            try
            {
                tracked.release();
            }
            catch { }
            resumableOneShots.Remove(id);
        }
    }

    public void StopAllPossibleInstances()
    {
        foreach (var kv in stateSoundInstances)
        {
            try
            {
                kv.Value.stop(STOP_MODE.IMMEDIATE);
                kv.Value.release();
            }
            catch { }
        }
        stateSoundInstances.Clear();

        foreach (var kv in resumableOneShots)
        {
            try
            {
                kv.Value.stop(STOP_MODE.IMMEDIATE);
                kv.Value.release();
            }
            catch { }
        }
        resumableOneShots.Clear();

        // stop heartbeat if playing
        StopHeartbeat(true);

        // stop whisper if playing
        StopWhisperLoop();

        // stop glitch if playing
        StopGlitchLoop();

        // stop music too
        StopAllMusicImmediate();
    }

    // HEARTBEAT control
    public void StartHeartbeat()
    {
        if (heartbeatPlaying) return;
        if (heartbeatEvent.IsNull)
        {
            Debug.LogWarning("[AudioManager] Heartbeat event not assigned.");
            return;
        }

        heartbeatInstance = RuntimeManager.CreateInstance(heartbeatEvent);
        try
        {
            heartbeatInstance.start();
            if (aiVoicesMuted) heartbeatInstance.setPaused(true);
            heartbeatPlaying = true;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[AudioManager] Failed to start heartbeat: " + ex.Message);
            heartbeatInstance = new EventInstance();
            heartbeatPlaying = false;
        }
    }

    public void StopHeartbeat(bool allowFadeOut = true)
    {
        if (heartbeatInstance.handle == null)
        {
            heartbeatPlaying = false;
            return;
        }

        try
        {
            heartbeatInstance.stop(allowFadeOut ? STOP_MODE.ALLOWFADEOUT : STOP_MODE.IMMEDIATE);
            heartbeatInstance.release();
        }
        catch { }
        heartbeatInstance = new EventInstance();
        heartbeatPlaying = false;
    }

    // Pause/resume
    public void PauseHeartbeat(bool pause)
    {
        if (heartbeatInstance.handle == null) return;
        try
        {
            heartbeatInstance.setPaused(pause);
        }
        catch { }
    }

    // music system
    public void PlayMusic(int trackId, float crossfade = 1f, bool force = false)
    {
        if (!musicMap.TryGetValue(trackId, out var evRef) || evRef.IsNull)
        {
            Debug.LogWarning($"AudioManager: requested music id {trackId} not assigned.");
            return;
        }

        if (!force && currentMusicTrackId == trackId) return;
        PlayMusic(evRef, trackId, crossfade);
    }

    private void PlayMusic(EventReference eventRef, int trackId, float crossfade)
    {
        if (eventRef.IsNull) return;

        var nextInstance = RuntimeManager.CreateInstance(eventRef);
        try
        {
            nextInstance.setVolume(0f);
        }
        catch { }
        nextInstance.start();

        if (crossfadeCoroutine != null)
            StopCoroutine(crossfadeCoroutine);

        crossfadeCoroutine = StartCoroutine(CrossfadeMusicCoroutine(currentMusicInstance, nextInstance, crossfade));
        currentMusicInstance = nextInstance;
        currentMusicRef = eventRef;
        currentMusicTrackId = trackId;
    }

    public void StopMusic(float fadeOut = 1f)
    {
        if (crossfadeCoroutine != null)
        {
            StopCoroutine(crossfadeCoroutine);
            crossfadeCoroutine = null;
        }

        if (currentMusicInstance.handle != null)
        {
            StartCoroutine(FadeOutAndStop(currentMusicInstance, fadeOut));
        }

        currentMusicInstance = new EventInstance();
        currentMusicRef = new EventReference();
        currentMusicTrackId = null;
    }

    public void StopAllMusicImmediate()
    {
        if (crossfadeCoroutine != null)
        {
            StopCoroutine(crossfadeCoroutine);
            crossfadeCoroutine = null;
        }

        try
        {
            if (currentMusicInstance.handle != null)
            {
                currentMusicInstance.stop(STOP_MODE.IMMEDIATE);
                currentMusicInstance.release();
            }
        }
        catch { }

        currentMusicInstance = new EventInstance();
        currentMusicRef = new EventReference();
        currentMusicTrackId = null;
    }

    // Ref-count Music (good for Combat Music)
    public void RequestMusic(int trackId, float crossfade = 1f)
    {
        if (!musicMap.ContainsKey(trackId)) return;

        if (!musicRefCounts.TryGetValue(trackId, out var count))
            count = 0;
        count++;
        musicRefCounts[trackId] = count;

        if (count == 1)
            PlayMusic(trackId, crossfade, force: true);
    }

    public void ReleaseMusic(int trackId, float fadeOut = 1f)
    {
        if (!musicRefCounts.TryGetValue(trackId, out var count))
            count = 0;
        count = Mathf.Max(0, count - 1);
        musicRefCounts[trackId] = count;

        if (count == 0 && currentMusicTrackId == trackId)
            StopMusic(fadeOut);
    }

    // Neon dimension audio control
    public void EnterNeonDimensionAudio(int musicTrackIdOverride = -1)
    {
        if (neonAudioActive) return;

        savedMusicBeforeNeon = currentMusicTrackId;
        int idToPlay = (musicTrackIdOverride > 0) ? musicTrackIdOverride : neonMusicTrackId;

        PlayMusic(idToPlay, crossfade: 1f, force: true);
        StartWhisperLoop();
        neonAudioActive = true;
    }

    public void ExitNeonDimensionAudio()
    {
        if (!neonAudioActive) return;

        StopWhisperLoop();
        if (savedMusicBeforeNeon.HasValue && savedMusicBeforeNeon.Value != -1)
        {
            PlayMusic(savedMusicBeforeNeon.Value, crossfade: 1f, force: true);
        }
        else
        {
            StopMusic(0.5f);
        }
        savedMusicBeforeNeon = null;
        neonAudioActive = false;
    }

    // Glitch loop methods
    public void StartGlitchLoop()
    {
        if (glitchMultiToolEvent.IsNull)
        {
            return;
        }

        if (glitchPlaying)
        {
            StopGlitchLoop();
        }

        glitchPlaying = true;
        glitchCoroutine = StartCoroutine(GlitchLoopCoroutine());
    }

    public void StopGlitchLoop()
    {
        if (!glitchPlaying) return;

        glitchPlaying = false;

        if (glitchCoroutine != null)
        {
            StopCoroutine(glitchCoroutine);
            glitchCoroutine = null;
        }
    }

    private IEnumerator GlitchLoopCoroutine()
    {
        // Play at least one glitch sound immediately
        PlayGlitchOneShot();

        while (glitchPlaying)
        {
            // Wait for random interval before next glitch sound
            float waitTime = Random.Range(glitchMinInterval, glitchMaxInterval);
            yield return new WaitForSeconds(waitTime);

            // Check again if we should still be playing
            if (glitchPlaying)
            {
                PlayGlitchOneShot();
            }
        }
    }

    // Whisper loop methods
    private void StartWhisperLoop()
    {
        if (whisperLoopEvent.IsNull)
        {
            return;
        }

        if (whisperPlaying) return;

        try
        {
            if (whisperInstance.handle == null)
            {
                whisperInstance = RuntimeManager.CreateInstance(whisperLoopEvent);
            }

            whisperInstance.start();
            whisperPlaying = true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to start whisper loop: {e.Message}");
            whisperPlaying = false;
        }
    }

    private void StopWhisperLoop()
    {
        if (!whisperPlaying) return;

        try
        {
            if (whisperInstance.handle != null)
            {
                whisperInstance.stop(STOP_MODE.ALLOWFADEOUT);
            }
            whisperPlaying = false;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to stop whisper loop: {e.Message}");
        }
    }

    // Helpers
    private IEnumerator FadeOutAndStop(EventInstance inst, float duration)
    {
        if (inst.handle == null) yield break;

        float t = 0f;
        float startVol = 1f;
        if (duration <= 0f) duration = 0.01f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float v = Mathf.Lerp(startVol, 0f, t / duration);
            SetInstanceVolume(inst, v);
            yield return null;
        }

        try
        {
            SetInstanceVolume(inst, 0f);
            inst.stop(STOP_MODE.ALLOWFADEOUT);
            inst.release();
        }
        catch { }
    }

    private IEnumerator CrossfadeMusicCoroutine(EventInstance from, EventInstance to, float duration)
    {
        if (duration <= 0f) duration = 0.01f;

        float t = 0f;
        SetInstanceVolume(to, 0f);

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float alpha = Mathf.Clamp01(t / duration);
            float volTo = Mathf.Lerp(0f, 1f, alpha);
            float volFrom = Mathf.Lerp(1f, 0f, alpha);
            SetInstanceVolume(to, volTo);
            if (from.handle != null)
                SetInstanceVolume(from, volFrom);
            yield return null;
        }

        SetInstanceVolume(to, 1f);
        if (from.handle != null)
        {
            try
            {
                SetInstanceVolume(from, 0f);
                from.stop(STOP_MODE.ALLOWFADEOUT);
                from.release();
            }
            catch { }
        }
        crossfadeCoroutine = null;
    }

    private void SetInstanceVolume(EventInstance inst, float volume)
    {
        if (inst.handle == null) return;
        try
        {
            inst.setVolume(volume);
        }
        catch { }
    }

    // AI Voice Muting
    public void SetAIVoicesMute(bool mute)
    {
        if (aiVoicesMuted == mute) return;
        aiVoicesMuted = mute;

        foreach (var kv in stateSoundInstances)
        {
            try
            {
                kv.Value.setPaused(mute);
            }
            catch { }
        }

        foreach (var kv in resumableOneShots)
        {
            try
            {
                kv.Value.setPaused(mute);
            }
            catch { }
        }

        try
        {
            PauseHeartbeat(mute);
        }
        catch { }

        if (!string.IsNullOrEmpty(aiBusPath))
        {
            try
            {
                var bus = RuntimeManager.GetBus(aiBusPath);
                bus.setMute(mute);
            }
            catch { }
        }
    }

    public bool AreAIVoicesMuted() => aiVoicesMuted;
    public bool IsNeonAudioActive() => neonAudioActive;
    public bool IsWhisperPlaying() => whisperPlaying;
    public bool IsGlitchPlaying() => glitchPlaying;

    public string GetUniqueIdentifier()
    {
        return "AudioManager";
    }

    public object CaptureState()
    {
        return new SaveData
        {
            currentMusicTrackId = currentMusicTrackId != null ? (int)currentMusicTrackId : -1
        };
    }

    public void RestoreState(object state)
    {
        string json = state as string;
        SaveData data = JsonUtility.FromJson<SaveData>(json);
        currentMusicTrackId = data.currentMusicTrackId;
        if (currentMusicTrackId != null || currentMusicTrackId != -1)
        {
            PlayMusic((int)currentMusicTrackId, 0.1f, true);
        }
    }

    public class SaveData
    {
        public int currentMusicTrackId;
    }
}
