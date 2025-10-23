using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class ParasiteBody : MonoBehaviour, ISaveable
{
    [SerializeField] private Animator bodyAnimator;
    [SerializeField] private Parasite parasite;
    [SerializeField] private float startingDelay = 0f;
    [SerializeField] private float durationBeforeGettingOut = 2f;
    [SerializeField] private UnityEvent onGetOut;

    private const string CAN_SWITCH = "CanSwitch";
    private const string BODY_SHAKING_1 = "BodyShaking1";
    private const string GOTTEN_OUT = "GottenOut";

    private bool hasBeenTriggered = false;

    private void Start()
    {
        bodyAnimator.SetBool(CAN_SWITCH, false);
    }

    public void TriggerParasiteGettingOut()
    {
        if (hasBeenTriggered) return;
        StartCoroutine(ParasiteGettingOut());
    }

    private IEnumerator ParasiteGettingOut()
    {
        hasBeenTriggered = true;
        yield return new WaitForSeconds(startingDelay);
        bodyAnimator.CrossFade(BODY_SHAKING_1, 0.1f);
        yield return new WaitForSeconds(durationBeforeGettingOut);
        bodyAnimator.SetTrigger(CAN_SWITCH);
        parasite.GetOut();
        onGetOut.Invoke();
    }

    public string GetUniqueIdentifier()
    {
        return "ParasiteBody" + gameObject.name;
    }

    public object CaptureState()
    {
        return new SaveData { hasBeenTriggered = hasBeenTriggered };
    }

    public void RestoreState(object state)
    {
        string json = state as string;
        SaveData data = JsonUtility.FromJson<SaveData>(json);
        hasBeenTriggered = data.hasBeenTriggered;
        if (hasBeenTriggered)
        {
            bodyAnimator.Play(GOTTEN_OUT);
        }
    }

    public class SaveData
    {
        public bool hasBeenTriggered;
    }
}
