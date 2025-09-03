using FMODUnity;
using UnityEngine;

public class PeepingHoleParasiteBreedingBody : MonoBehaviour
{
    [SerializeField] private ParticleSystem bloodEffect1;
    [SerializeField] private ParticleSystem bloodEffect2;
    [SerializeField] private EventReference bloodSound;

    private void Start()
    {
        StopBlood();
    }

    public void StopBlood()
    {
        bloodEffect1.Stop();
        bloodEffect2.Stop();
    }

    public void PlayBloodEffects()
    {
        bloodEffect1.Play();
        bloodEffect2.Play();
    }

    public void PlayBloodSound()
    {
        AudioManager.Instance.PlayOneShot(bloodSound, transform.position);
    }
}
