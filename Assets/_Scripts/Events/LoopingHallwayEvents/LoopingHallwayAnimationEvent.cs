using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class LoopingHallwayAnimationEvent : MonoBehaviour
{
    [SerializeField] private GameObject light1;
    [SerializeField] private GameObject light2;
    [SerializeField] private GameObject TeleportarActivater;
    [SerializeField] private GameObject PlayerArm;

    [Header("2D One-shot Sound")]
    [SerializeField] private EventReference oneShotSound;

    public void ActivateTeleporter()
    {
        TeleportarActivater.SetActive(true);
    }

    public void DisableArm()
    {
        PlayerArm.SetActive(false);
    }

    public void LightsOn()
    {
        light1.SetActive(true);
        light2.SetActive(true);
    }

    public void PlayOneShotSound()
    {
        if (oneShotSound.IsNull)
        {
            Debug.LogWarning("FMOD one-shot sound event is not assigned!");
            return;
        }

        if (RuntimeManager.StudioSystem.getEvent(oneShotSound.Path, out EventDescription eventDescription) != FMOD.RESULT.OK)
        {
            Debug.LogWarning("Failed to get FMOD event description for: " + oneShotSound.Path);
            return;
        }

        eventDescription.is3D(out bool is3D);

        if (is3D)
        {
            RuntimeManager.PlayOneShotAttached(oneShotSound, gameObject);
        }
        else
        {
            RuntimeManager.PlayOneShot(oneShotSound);
        }
    }
}