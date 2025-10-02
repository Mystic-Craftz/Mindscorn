using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple one-shot dialog trigger:
/// - Edit the "Sequence" list in the inspector. Use the small '+' to add entries.
/// - Each entry: message, delay (before showing), duration (how long it stays), color.
/// - When player (tag "Player") enters the trigger, the sequence plays one-by-one and won't repeat.
/// - This script calls DialogUI.Instance.ShowDialogTrigger(...) if DialogUI is present; otherwise it logs.
/// </summary>
[RequireComponent(typeof(Collider))]
[AddComponentMenu("Triggers/Multi Dialog Trigger (Simple)")]
public class MultiDialogTrigger : MonoBehaviour
{
    [System.Serializable]
    public class DialogSequenceItem
    {
        [TextArea(2, 5)]
        public string message;
        [Tooltip("Time to wait BEFORE showing this dialog (relative to sequence).")]
        public float delay = 0f;
        [Tooltip("How long the dialog should remain visible.")]
        public float duration = 2f;
        public Color color = Color.white;
    }

    [Header("Sequence (edit in inspector)")]
    [Tooltip("Use the + button (right of the list header) to add new dialog entries.")]
    [SerializeField] private List<DialogSequenceItem> sequence = new List<DialogSequenceItem>();

    [Header("Trigger Settings")]
    [SerializeField] private string playerTag = "Player";
    [Tooltip("Run the sequence only once (true).")]
    [SerializeField] private bool singleUse = true;

    // internal state
    private bool hasTriggered = false;
    private Coroutine runningCoroutine = null;

    private void Reset()
    {
        // ensure collider is trigger by default when attached
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        TryTrigger();
    }

    private void TryTrigger()
    {
        if (singleUse && hasTriggered) return;
        if (runningCoroutine != null) return;

        // mark early to avoid race conditions / multiple enters
        if (singleUse) hasTriggered = true;

        runningCoroutine = StartCoroutine(RunSequence());
    }

    private IEnumerator RunSequence()
    {
        if (sequence == null || sequence.Count == 0)
        {
            runningCoroutine = null;
            yield break;
        }

        foreach (var item in sequence)
        {
            if (item == null) continue;

            if (item.delay > 0f)
                yield return new WaitForSeconds(item.delay);

            SendDialog(item);

            if (item.duration > 0f)
                yield return new WaitForSeconds(item.duration);
        }

        runningCoroutine = null;
    }

    private void SendDialog(DialogSequenceItem item)
    {
        // Build DialogParams instance (keeps compatibility with your EventTrigger)
        var p = new DialogParams
        {
            message = item.message,
            duration = item.duration,
            color = item.color
        };

        if (DialogUI.Instance != null)
        {
            DialogUI.Instance.ShowDialogTrigger(p);
        }
        else
        {
            Debug.Log("[MultiDialogTrigger] DialogUI.Instance not found. Dialog: \"" + item.message + "\"");
        }
    }

    /// <summary>
    /// Optional: call this from other scripts to force trigger.
    /// </summary>
    public void TriggerSequence()
    {
        TryTrigger();
    }

    /// <summary>
    /// Optional: cancel running sequence (not typically needed for one-shot).
    /// </summary>
    public void CancelSequence()
    {
        if (runningCoroutine != null)
        {
            StopCoroutine(runningCoroutine);
            runningCoroutine = null;
        }
    }
}
