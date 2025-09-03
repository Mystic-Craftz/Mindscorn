using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(SaveableEntity))]
public class MusicTrigger : MonoBehaviour, ISaveable
{
    [SerializeField] private int musicTrackId = 1;
    [SerializeField] private float crossfadeValue = 1f;
    [SerializeField] private bool triggerOnce = true;
    [SerializeField] private UnityEvent afterTrigger;

    private bool hasTriggeredMusic = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (triggerOnce && hasTriggeredMusic) return;

            AudioManager.Instance.PlayMusic(musicTrackId, crossfadeValue);
            hasTriggeredMusic = true;
            afterTrigger?.Invoke();
        }
    }

    public object CaptureState()
    {
        return new SaveData
        {
            hasTriggeredMusic = hasTriggeredMusic
        };
    }

    public string GetUniqueIdentifier()
    {
        return "Music" + GetComponent<SaveableEntity>().UniqueId;
    }

    public void RestoreState(object state)
    {
        string json = state as string;
        SaveData data = JsonUtility.FromJson<SaveData>(json);
        hasTriggeredMusic = data.hasTriggeredMusic;
    }

    private class SaveData
    {
        public bool hasTriggeredMusic;
    }
}
