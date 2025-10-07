using System.Collections;
using FMODUnity;
using UnityEngine;

public class PhysicsChain : MonoBehaviour
{
    [SerializeField] private EventReference sound;

    private bool canPlaySoundAgain = true;

    public void PlaySound()
    {
        if (canPlaySoundAgain)
        {
            AudioManager.Instance.PlayOneShot(sound, transform.position);
            canPlaySoundAgain = false;
            StartCoroutine(ResetCanPlaySoundAfterDelay(1f));
        }
    }

    private IEnumerator ResetCanPlaySoundAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        canPlaySoundAgain = true;
    }
}
