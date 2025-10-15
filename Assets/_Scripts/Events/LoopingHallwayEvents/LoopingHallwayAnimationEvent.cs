using UnityEngine;
using FMODUnity;

public class LoopingHallwayAnimationEvent : MonoBehaviour
{
    [SerializeField] private GameObject light1;
    [SerializeField] private GameObject light2;
    [SerializeField] private GameObject TeleportarActivater;
    [SerializeField] private GameObject PlayerArm;

    [Header("2D One-shot Sound")]
    [SerializeField] private EventReference oneShotSound2D;

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
        if (oneShotSound2D.IsNull)
        {
            Debug.LogWarning("2D one-shot sound event is not assigned!");
            return;
        }

        RuntimeManager.PlayOneShot(oneShotSound2D);
    }
}