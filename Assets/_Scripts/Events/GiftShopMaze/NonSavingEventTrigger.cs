using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class NonSavingEventTrigger : MonoBehaviour
{
    [SerializeField] private UnityEvent onStart;
    [SerializeField] private UnityEvent onTrigger;
    [SerializeField] private UnityEvent afterTrigger;
    [SerializeField] private DialogEvent dialogOnTrigger;
    [SerializeField] private string dialogMessage;
    [SerializeField] private float dialogDuration = 2;
    [SerializeField] private float dialogDelay = 0;
    [SerializeField] private bool triggerOnce;
    [SerializeField] private string tagToTrigger = "Player";

    private bool isTriggered = false;

    private void Start()
    {
        onStart?.Invoke();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(tagToTrigger))
        {
            if (triggerOnce)
            {
                if (!isTriggered)
                {
                    onTrigger?.Invoke();
                    StartCoroutine(DialogCoRoutine());
                    isTriggered = true;
                    afterTrigger?.Invoke();
                }
            }
            else
            {
                onTrigger?.Invoke();
            }
        }
    }

    private IEnumerator DialogCoRoutine()
    {
        yield return new WaitForSeconds(dialogDelay);
        DialogUI.Instance.ShowDialog(dialogMessage, dialogDuration, Color.white);
    }
}
