using FMODUnity;
using UnityEngine;

[RequireComponent(typeof(SaveableEntity))]
public class PlaySoundOnTrigger : MonoBehaviour, ISaveable
{
    [SerializeField] private EventReference soundToPlay;
    [SerializeField] private bool canOnlyPlayOnce = true;

    private bool hasPlayedSound = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (!canOnlyPlayOnce || !hasPlayedSound)
            {
                AudioManager.Instance.PlayOneShot(soundToPlay, transform.position);
                hasPlayedSound = true;
            }
        }
    }

    public object CaptureState()
    {
        return new SaveData
        {
            hasPlayedSound = hasPlayedSound
        };
    }

    public string GetUniqueIdentifier()
    {
        return GetComponent<SaveableEntity>().UniqueId;
    }

    public void RestoreState(object state)
    {
        string json = state as string;
        SaveData data = JsonUtility.FromJson<SaveData>(json);
        hasPlayedSound = data.hasPlayedSound;
    }

    public class SaveData
    {
        public bool hasPlayedSound;
    }
}
