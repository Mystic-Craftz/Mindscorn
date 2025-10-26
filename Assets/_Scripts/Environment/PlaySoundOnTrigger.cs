using FMODUnity;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(SaveableEntity))]
public class PlaySoundOnTrigger : MonoBehaviour, ISaveable
{
    [SerializeField] private EventReference soundToPlay;
    [SerializeField] private bool canOnlyPlayOnce = true;
    [SerializeField, Tooltip("If set, the sound will be played from this object's position.")] private Transform soundOrigin;

    [Header("Delay Option")]
    [Tooltip("If true, the sound will be played after DelaySeconds instead of immediately when triggered.")]
    [SerializeField] private bool playAfterDelay = false;
    [Tooltip("Delay in seconds before playing the sound when PlayAfterDelay is enabled.")]
    [SerializeField] private float delaySeconds = 1f;

    [SerializeField] private UnityEvent onPlaySound;

    private bool hasPlayedSound = false;
    private Coroutine delayCoroutine = null;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (canOnlyPlayOnce && hasPlayedSound) return;

        if (playAfterDelay)
        {
            StartDelayedPlay();
        }
        else
        {
            if (soundOrigin == null)
            {
                AudioManager.Instance.PlayOneShot(soundToPlay, transform.position);
                onPlaySound?.Invoke();
            }
            else
            {
                AudioManager.Instance.PlayOneShot(soundToPlay, soundOrigin.position);
                onPlaySound?.Invoke();
            }
            hasPlayedSound = true;
        }
    }

    public void StartDelayedPlay()
    {
        if (canOnlyPlayOnce && hasPlayedSound) return;


        if (delayCoroutine != null) return;

        delayCoroutine = StartCoroutine(DelayedPlayCoroutine());
    }


    public bool TriggerDelayedPlay
    {
        set
        {
            if (value) StartDelayedPlay();
        }
    }

    private IEnumerator DelayedPlayCoroutine()
    {

        float wait = Mathf.Max(0f, delaySeconds);
        yield return new WaitForSeconds(wait);

        if (!(canOnlyPlayOnce && hasPlayedSound))
        {
            if (soundOrigin == null)
            {
                AudioManager.Instance.PlayOneShot(soundToPlay, transform.position);
                onPlaySound?.Invoke();
            }
            else
            {
                AudioManager.Instance.PlayOneShot(soundToPlay, soundOrigin.position);
                onPlaySound?.Invoke();
            }
            hasPlayedSound = true;
        }

        delayCoroutine = null;
    }


    #region Saving
    public object CaptureState()
    {
        return new SaveData
        {
            hasPlayedSound = hasPlayedSound
        };
    }

    public string GetUniqueIdentifier()
    {
        return "SoundTrigger" + GetComponent<SaveableEntity>().UniqueId;
    }

    public void RestoreState(object state)
    {
        string json = state as string;
        SaveData data = JsonUtility.FromJson<SaveData>(json);
        hasPlayedSound = data.hasPlayedSound;
    }

    [System.Serializable]
    public class SaveData
    {
        public bool hasPlayedSound;
    }
    #endregion
}
