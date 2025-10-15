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
    [SerializeField] private DialogEvent dialogOnTrigger;
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

        yield return StartCoroutine(EnqueueDialogsAndWaitForCompletion());

        onTrigger?.Invoke();
        afterTrigger?.Invoke();

        if (triggerOnce)
            isTriggered = true;

        isProcessing = false;
    }

    private IEnumerator EnqueueDialogsAndWaitForCompletion()
    {
        if (dialogDelay > 0f)
            yield return new WaitForSeconds(dialogDelay);

        bool anyEnqueued = false;

        void Enqueue(DialogParams p)
        {
            dialogOnTrigger?.Invoke(p);

            if (DialogUI.Instance != null)
                DialogUI.Instance.ShowDialog(p.message, p.duration, p.color);

            anyEnqueued = true;
        }

        if (!string.IsNullOrEmpty(dialogMessage))
        {
            Enqueue(new DialogParams { message = dialogMessage, duration = dialogDuration, color = Color.white });
        }

        if (dialogueLists != null && dialogueLists.Count > 0)
        {
            foreach (var d in dialogueLists)
            {
                if (string.IsNullOrEmpty(d.dialogue))
                    continue;

                Enqueue(new DialogParams { message = d.dialogue, duration = d.duration, color = d.color });
            }
        }

        if (!anyEnqueued)
            yield break;

        if (DialogUI.Instance != null)
        {
            yield return StartCoroutine(WaitForDialogUIToBecomeIdle(DialogUI.Instance));
            yield break;
        }

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
        return JsonUtility.ToJson(new SaveData { isTriggered = isTriggered });
    }

    public string GetUniqueIdentifier()
    {
        return "EventTrigger" + GetComponent<SaveableEntity>().UniqueId;
    }

    public void RestoreState(object state)
    {
        string json = state as string;
        if (string.IsNullOrEmpty(json)) return;
        SaveData data = JsonUtility.FromJson<SaveData>(json);
        isTriggered = data.isTriggered;
    }

    [Serializable]
    public class SaveData
    {
        public bool isTriggered;
    }
}
