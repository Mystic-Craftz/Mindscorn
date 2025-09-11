using FMODUnity;
using UnityEngine;

public class NonSavingSoundTrigger : MonoBehaviour
{
    [SerializeField] private EventReference soundToPlay;
    [SerializeField] private bool canOnlyPlayOnce = true;
    [SerializeField] private Transform soundLocation;

    private bool hasPlayedSound = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (!canOnlyPlayOnce || !hasPlayedSound)
            {
                if (soundLocation != null)
                {
                    AudioManager.Instance.PlayOneShot(soundToPlay, soundLocation.position);
                }
                else
                {
                    AudioManager.Instance.PlayOneShot(soundToPlay, transform.position);
                }
                hasPlayedSound = true;
            }
        }
    }
}
