using UnityEngine;
using System.Collections;

public class BossJumpscareEvents : MonoBehaviour
{
    public Animator animator;
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
}
