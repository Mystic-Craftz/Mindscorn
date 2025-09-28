using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class ParasiteBody : MonoBehaviour
{
    [SerializeField] private Animator bodyAnimator;
    [SerializeField] private Parasite parasite;
    [SerializeField] private float startingDelay = 0f;
    [SerializeField] private float durationBeforeGettingOut = 2f;
    [SerializeField] private UnityEvent onGetOut;

    private const string CAN_SWITCH = "CanSwitch";
    private const string BODY_SHAKING_1 = "BodyShaking1";

    private void Start()
    {
        bodyAnimator.SetBool(CAN_SWITCH, false);
    }

    public void TriggerParasiteGettingOut()
    {
        StartCoroutine(ParasiteGettingOut());
    }

    private IEnumerator ParasiteGettingOut()
    {
        yield return new WaitForSeconds(startingDelay);
        bodyAnimator.CrossFade(BODY_SHAKING_1, 0.1f);
        yield return new WaitForSeconds(durationBeforeGettingOut);
        bodyAnimator.SetTrigger(CAN_SWITCH);
        parasite.GetOut();
        onGetOut.Invoke();
    }
}
