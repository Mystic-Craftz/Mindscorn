using UnityEngine;
using System.Collections;
using FMODUnity;
using FMOD.Studio;

public class BossJumpscareEvents : MonoBehaviour
{
    public GameObject eventTriggerEnabler;
    public Animator animator;
    public Unity.Cinemachine.CinemachineImpulseSource impulseSource;
    public float impulseInterval = 0.25f;
    private Coroutine _impulseCoroutine;
    private string nextTrigger = "StartSquash";

    [Header("One-shot Sounds (assign any 2D or 3D FMOD events)")]
    [SerializeField] private EventReference oneShotSound1;
    [SerializeField] private EventReference oneShotSound2;

    public void OnAppearingFinished()
    {
        animator.SetTrigger(nextTrigger);
    }

    public void DisableAfterDelay(float delay)
    {
        StartCoroutine(DisableCoroutine(delay));
    }

    private IEnumerator DisableCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        gameObject.SetActive(false);
    }

    public void InstantBlackout()
    {
        NeonDimensionController.Instance.InstantBlackout();
    }

    public void PlayImpulse()
    {
        if (_impulseCoroutine == null)
            _impulseCoroutine = StartCoroutine(ImpulseLoop());
    }

    private IEnumerator ImpulseLoop()
    {
        while (true)
        {
            impulseSource.GenerateImpulse();
            yield return new WaitForSeconds(Mathf.Max(0.01f, impulseInterval));
        }
    }

    public void StopImpulseImmediate()
    {
        if (_impulseCoroutine != null)
        {
            StopCoroutine(_impulseCoroutine);
            _impulseCoroutine = null;
        }

        Unity.Cinemachine.CinemachineImpulseManager.Instance.Clear();
    }

    public void TriggerTeleportation()
    {
        eventTriggerEnabler.SetActive(true);
    }

    public void PlayOneShotSound1()
    {
        PlayEventSmart(oneShotSound1);
    }

    public void PlayOneShotSound2()
    {
        PlayEventSmart(oneShotSound2);
    }

    private void PlayEventSmart(EventReference eventRef)
    {
        if (eventRef.IsNull)
        {
            Debug.LogWarning("Attempted to play a null FMOD event.");
            return;
        }

        try
        {
            EventInstance instance = RuntimeManager.CreateInstance(eventRef);

            if (!instance.isValid())
            {
                Debug.LogWarning("PlayEventSmart: Could not create FMOD instance. Falling back to attached play.");
                RuntimeManager.PlayOneShotAttached(eventRef, gameObject);
                return;
            }

            // Query description from the instance
            if (instance.getDescription(out EventDescription desc) == FMOD.RESULT.OK)
            {
                desc.is3D(out bool is3D);

                instance.release();

                if (is3D)
                    RuntimeManager.PlayOneShotAttached(eventRef, gameObject);
                else
                    RuntimeManager.PlayOneShot(eventRef);

                return;
            }
            else
            {
                instance.release();
                Debug.LogWarning("PlayEventSmart: couldn't get description. Falling back.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"PlayEventSmart: exception while querying event description. Falling back. Exception: {e}");
        }

        // fallback
        RuntimeManager.PlayOneShotAttached(eventRef, gameObject);
    }
}
