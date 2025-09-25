using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(SaveableEntity))]
public class EventTrigger : MonoBehaviour, ISaveable
{
    [SerializeField] private UnityEvent onStart;
    [SerializeField] private UnityEvent onTrigger;
    [SerializeField] private UnityEvent afterTrigger;
    [SerializeField] private DialogEvent dialogOnTrigger;
    [SerializeField] private string dialogMessage;
    [SerializeField] private float dialogDuration = 2;
    [SerializeField] private float dialogDelay = 0;
    [SerializeField] private bool triggerOnce;

    private bool isTriggered = false;

    private void Start()
    {
        onStart?.Invoke();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
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
        dialogOnTrigger?.Invoke(new DialogParams { message = dialogMessage, duration = dialogDuration });
    }

    public object CaptureState()
    {
        return new SaveData { isTriggered = isTriggered };
    }

    public string GetUniqueIdentifier()
    {
        return "EventTrigger" + GetComponent<SaveableEntity>().UniqueId;
    }

    public void RestoreState(object state)
    {
        string json = state as string;
        SaveData data = JsonUtility.FromJson<SaveData>(json);
        isTriggered = data.isTriggered;
    }

    public class SaveData
    {
        public bool isTriggered;
    }
}