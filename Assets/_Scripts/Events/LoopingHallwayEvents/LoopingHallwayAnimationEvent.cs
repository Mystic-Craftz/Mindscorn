using UnityEngine;

public class LoopingHallwayAnimationEvent : MonoBehaviour
{

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
}
