using UnityEngine;
using System.Collections;

public class BossJumpscareEvents : MonoBehaviour
{
    public void DisableAfterDelay(float delay)
    {
        StartCoroutine(DisableCoroutine(delay));
    }

    private IEnumerator DisableCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        gameObject.SetActive(false);
    }
}
