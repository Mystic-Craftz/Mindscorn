using UnityEngine;
using System.Collections;

public class BossJumpscareEvents : MonoBehaviour
{
    public GameObject eventTriggerEnabler;
    public Animator animator;
    public Unity.Cinemachine.CinemachineImpulseSource impulseSource;
    public float impulseInterval = 0.25f;
    private Coroutine _impulseCoroutine;
    private string nextTrigger = "StartSquash";

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
}
