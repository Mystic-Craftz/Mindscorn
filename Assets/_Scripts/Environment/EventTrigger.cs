using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.VersionControl;
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
    [SerializeField] private bool useDelay; // New bool to enable/disable delay
    [SerializeField] private float triggerDelay; // New delay time in seconds
    [SerializeField] private List<DialogueData> dialogueLists = new List<DialogueData>();

    [Serializable]
    public class DialogueData
    {
        public string dialogue;
        public float duration;
        public Color color = Color.white;
    }

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
                    if (useDelay)
                    {
                        StartCoroutine(TriggerAfterDelay());
                    }
                    else
                    {
                        TriggerImmediately();
                    }
                    isTriggered = true;
                }
            }
            else
            {
                if (useDelay)
                {
                    StartCoroutine(TriggerAfterDelay());
                }
                else
                {
                    TriggerImmediately();
                }
            }
        }
    }

    private void TriggerImmediately()
    {
        onTrigger?.Invoke();
        StartCoroutine(DialogCoRoutine());
        afterTrigger?.Invoke();
    }

    private IEnumerator TriggerAfterDelay()
    {
        yield return new WaitForSeconds(triggerDelay);
        onTrigger?.Invoke();
        StartCoroutine(DialogCoRoutine());
        afterTrigger?.Invoke();
    }

    private IEnumerator DialogCoRoutine()
    {
        yield return new WaitForSeconds(dialogDelay);
        if (dialogMessage != null || dialogMessage != "")
            dialogOnTrigger?.Invoke(new DialogParams { message = dialogMessage, duration = dialogDuration, color = Color.white });
        if (dialogueLists.Count > 0)
        {
            foreach (var dialogue in dialogueLists)
            {
                DialogUI.Instance.ShowDialog(dialogue.dialogue, dialogue.duration, dialogue.color);
            }
        }
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