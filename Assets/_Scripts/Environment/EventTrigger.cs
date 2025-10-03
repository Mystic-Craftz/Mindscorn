using System;
using System.Collections;
using System.Collections.Generic;
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

    [Header("Trigger Options")]
    [SerializeField] private bool triggerOnce;
    [SerializeField] private bool useDelay;
    [SerializeField] private float triggerDelay;
    [SerializeField] private bool triggerAfterDialog;

    [SerializeField] private List<DialogueData> dialogueLists = new List<DialogueData>();

    [Serializable]
    public class DialogueData
    {
        public string dialogue;
        public float duration;
        public Color color = Color.white;
    }

    private bool isTriggered = false;
    private bool isProcessing = false;

    private void Start()
    {
        onStart?.Invoke();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (triggerOnce && (isTriggered || isProcessing))
            return;

        if (!triggerOnce && isProcessing)
            return;

        StartCoroutine(TriggerSequence());
    }

    private IEnumerator TriggerSequence()
    {
        isProcessing = true;

        if (useDelay && triggerDelay > 0f)
            yield return new WaitForSeconds(triggerDelay);

        if (triggerAfterDialog)
        {
            yield return StartCoroutine(DialogCoRoutine());
            onTrigger?.Invoke();
            afterTrigger?.Invoke();
        }
        else
        {
            onTrigger?.Invoke();
            yield return StartCoroutine(DialogCoRoutine());
            afterTrigger?.Invoke();
        }

        if (triggerOnce)
            isTriggered = true;

        isProcessing = false;
    }

    private IEnumerator DialogCoRoutine()
    {
        if (dialogDelay > 0f)
            yield return new WaitForSeconds(dialogDelay);

        if (!string.IsNullOrEmpty(dialogMessage))
        {
            dialogOnTrigger?.Invoke(new DialogParams { message = dialogMessage, duration = dialogDuration, color = Color.white });
            if (dialogDuration > 0f)
                yield return new WaitForSeconds(dialogDuration);
        }

        if (dialogueLists.Count > 0)
        {
            foreach (var dialogue in dialogueLists)
            {
                // show dialog (assumes DialogUI.ShowDialog is fire-and-forget)
                DialogUI.Instance.ShowDialog(dialogue.dialogue, dialogue.duration, dialogue.color);
                if (dialogue.duration > 0f)
                    yield return new WaitForSeconds(dialogue.duration);
            }
        }

        yield break;
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

    [Serializable]
    public class SaveData
    {
        public bool isTriggered;
    }
}
