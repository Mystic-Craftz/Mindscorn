using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(SaveableEntity))]
public class EventTrigger : MonoBehaviour, ISaveable
{
    [SerializeField] private UnityEvent onStart;
    [SerializeField] private UnityEvent onTrigger;
    [SerializeField] private UnityEvent afterTrigger;
    [SerializeField] private string tagToCompare = "Player";

    [Header("Dialog Options")]
    [SerializeField] private bool showDialogs = true; // show dialogs at all
    [SerializeField] private bool waitForDialogsBeforeInvokingEvents = false; // when checked, events wait until dialog finishes
    [SerializeField] private string dialogMessage;
    [SerializeField] private float dialogDuration = 2f;
    [SerializeField] private float dialogDelay = 0f;

    [Header("Trigger Options")]
    [SerializeField] private bool triggerOnce;
    [SerializeField] private bool useDelay;
    [SerializeField] private float triggerDelay;

    [SerializeField] private List<DialogueData> dialogueLists = new List<DialogueData>();

    [Serializable]
    public class DialogueData
    {
        public string dialogue;
        public float duration = 2f;
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
        if (!other.CompareTag(tagToCompare)) return;


        // prevent duplicate processing / re-entry
        if (triggerOnce && (isTriggered || isProcessing))
            return;

        if (!triggerOnce && isProcessing)
            return;

        if (triggerOnce)
            isTriggered = true;

        StartCoroutine(TriggerSequence());
    }

    private IEnumerator TriggerSequence()
    {
        isProcessing = true;

        if (useDelay && triggerDelay > 0f)
            yield return new WaitForSeconds(triggerDelay);

        // If we're showing dialogs and waiting for them to finish before firing events:
        if (showDialogs && waitForDialogsBeforeInvokingEvents)
        {
            yield return StartCoroutine(EnqueueDialogsAndWaitForCompletion());
            // now fire events
            onTrigger?.Invoke();
            afterTrigger?.Invoke();
        }
        else
        {
            // If we're showing dialogs but NOT waiting, start them in background
            if (showDialogs)
                StartCoroutine(EnqueueDialogsAndWaitForCompletion());

            // Immediately fire events
            onTrigger?.Invoke();
            afterTrigger?.Invoke();
        }

        isProcessing = false;
    }

    private IEnumerator EnqueueDialogsAndWaitForCompletion()
    {
        if (dialogDelay > 0f)
            yield return new WaitForSeconds(dialogDelay);

        bool anyEnqueued = false;

        void Enqueue(string message, float duration, Color color)
        {
            if (DialogUI.Instance != null)
                DialogUI.Instance.ShowDialog(message, duration, color);
            anyEnqueued = true;
        }

        // Optional single message
        if (!string.IsNullOrEmpty(dialogMessage))
            Enqueue(dialogMessage, dialogDuration, Color.white);

        // Dialogue list
        if (dialogueLists != null && dialogueLists.Count > 0)
        {
            foreach (var d in dialogueLists)
            {
                if (string.IsNullOrEmpty(d.dialogue))
                    continue;
                Enqueue(d.dialogue, d.duration, d.color);
            }
        }

        if (!anyEnqueued)
            yield break;

        // If DialogUI exists, try to wait until it's done
        if (DialogUI.Instance != null)
        {
            yield return StartCoroutine(WaitForDialogUIToBecomeIdle(DialogUI.Instance));
            yield break;
        }

        // Fallback wait if DialogUI is not present
        float fallbackWait = 0f;
        if (!string.IsNullOrEmpty(dialogMessage)) fallbackWait += dialogDuration;
        foreach (var d in dialogueLists)
            fallbackWait += Mathf.Max(0f, d.duration);
        fallbackWait += 0.1f;
        if (fallbackWait > 0f)
            yield return new WaitForSeconds(fallbackWait);
    }

    private IEnumerator WaitForDialogUIToBecomeIdle(DialogUI ui)
    {
        Type uiType = ui.GetType();
        FieldInfo queueField = uiType.GetField("dialogQueue", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo isShowingField = uiType.GetField("isShowingDialog", BindingFlags.Instance | BindingFlags.NonPublic);

        if (queueField == null || isShowingField == null)
        {
            while (ui.transform.childCount > 0)
                yield return null;
            yield break;
        }

        while (true)
        {
            object queueObj = queueField.GetValue(ui);
            int count = 0;
            if (queueObj is System.Collections.ICollection coll)
                count = coll.Count;

            bool isShowing = false;
            object val = isShowingField.GetValue(ui);
            if (val is bool b) isShowing = b;

            if (count == 0 && !isShowing)
                break;

            yield return null;
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

    [Serializable]
    public class SaveData
    {
        public bool isTriggered;
    }
}
