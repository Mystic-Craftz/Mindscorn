using Unity.Cinemachine.Samples;
using UnityEngine;

public class LoopingHallwayAnimationEvent : MonoBehaviour
{
    [SerializeField] private GameObject light1;
    [SerializeField] private GameObject light2;
    [SerializeField] private GameObject TeleportarActivater;
    [SerializeField] private GameObject PlayerArm;


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
}
