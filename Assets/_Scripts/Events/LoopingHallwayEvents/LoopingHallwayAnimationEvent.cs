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

        try
        {
            EventInstance instance = RuntimeManager.CreateInstance(oneShotSound);

            if (!instance.isValid())
            {
                Debug.LogWarning("PlayOneShotSound: could not create instance. Playing attached as fallback.");
                RuntimeManager.PlayOneShotAttached(oneShotSound, gameObject);
                return;
            }

            if (instance.getDescription(out EventDescription desc) == FMOD.RESULT.OK)
            {
                desc.is3D(out bool is3D);
                instance.release();

                if (is3D)
                    RuntimeManager.PlayOneShotAttached(oneShotSound, gameObject);
                else
                    RuntimeManager.PlayOneShot(oneShotSound);
                return;
            }
            instance.release();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"PlayOneShotSound: failed to query event description or play event. Exception: {e}");
        }

        // fallback
        RuntimeManager.PlayOneShotAttached(oneShotSound, gameObject);
    }
}
