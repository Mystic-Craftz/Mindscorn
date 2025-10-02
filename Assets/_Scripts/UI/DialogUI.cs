using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class DialogUI : MonoBehaviour
{
    public static DialogUI Instance { get; private set; }

    [SerializeField] private GameObject dialogTextTemplate;
    [SerializeField] private Transform dialogParent;

    private Queue<DialogParams> dialogQueue = new Queue<DialogParams>();
    private bool isShowingDialog = false;
    private HorizontalLayoutGroup horizontalLayoutGroup;
    private Transform dialogObj;
    private CanvasGroup canvasGroup;

    private bool canShowDoorDialog = true;

    private void Awake() => Instance = this;

    private void Start()
    {
        if (dialogTextTemplate != null) dialogTextTemplate.SetActive(false);
    }

    private void Update()
    {

        if (isShowingDialog && dialogObj != null && horizontalLayoutGroup != null)
        {

            dialogObj.gameObject.SetActive(false);
            horizontalLayoutGroup.CalculateLayoutInputHorizontal();
            horizontalLayoutGroup.CalculateLayoutInputVertical();
            horizontalLayoutGroup.SetLayoutVertical();
            horizontalLayoutGroup.SetLayoutHorizontal();
            dialogObj.gameObject.SetActive(true);
        }
    }

    public void ShowDialog(string message, float duration = 2f)
    {

        EnqueueDialog(new DialogParams { message = message, duration = duration, color = Color.white });
    }

    public void ShowDoorDialog(string message, float duration = .7f)
    {
        if (canShowDoorDialog)
        {
            EnqueueDialog(new DialogParams { message = message, duration = duration, color = Color.white });
            canShowDoorDialog = false;
            StartCoroutine(ResetCanShowDoorDialog());
        }
    }

    private IEnumerator ResetCanShowDoorDialog()
    {
        yield return new WaitForSeconds(5f);
        canShowDoorDialog = true;
    }


    public void ShowDialogTrigger(DialogParams dialogParams)
    {

        if (dialogParams == null) return;
        if (dialogParams.color == default) dialogParams.color = Color.white;
        EnqueueDialog(dialogParams);
    }

    private void EnqueueDialog(DialogParams dialogParams)
    {
        dialogQueue.Enqueue(dialogParams);
        if (!isShowingDialog)
        {
            PlayNextDialog();
        }
    }

    private void PlayNextDialog()
    {
        if (dialogQueue.Count == 0)
        {
            isShowingDialog = false;
            return;
        }

        isShowingDialog = true;
        var currentDialog = dialogQueue.Dequeue();
        if (currentDialog == null)
        {
            // skip and continue
            PlayNextDialog();
            return;
        }

        string[] words = string.IsNullOrEmpty(currentDialog.message)
            ? new string[0]
            : currentDialog.message.Split(' ');
        float wordDelay = 0.15f;
        float animDuration = 0.2f;


        if (dialogParent == null)
        {
            Debug.LogWarning("[DialogUI] dialogParent is null - cannot show dialog.");

            StartCoroutine(ContinueAfterShortDelay());
            return;
        }

        dialogObj = Instantiate(dialogParent, transform);
        dialogObj.gameObject.SetActive(false);

        horizontalLayoutGroup = dialogObj.GetComponent<HorizontalLayoutGroup>();
        canvasGroup = dialogObj.GetComponent<CanvasGroup>();
        if (canvasGroup != null) canvasGroup.alpha = 1f;

        for (int i = 0; i < words.Length; i++)
        {
            var wordObj = Instantiate(dialogTextTemplate, dialogObj.transform);
            if (wordObj == null) continue;

            var wordCanvasGroup = wordObj.GetComponent<CanvasGroup>();
            if (wordCanvasGroup != null) wordCanvasGroup.alpha = 0f;
            wordObj.SetActive(true);

            var text = wordObj.GetComponent<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = words[i];

                text.color = currentDialog.color;

                var rt = wordObj.GetComponent<RectTransform>();
                if (rt != null) rt.localScale = Vector3.one;

                text.margin = new Vector4(0, 198.2f, 0, 0);

                DOTween.To(() => text.margin, x => text.margin = x, new Vector4(0, 0, 0, 0), animDuration)
                    .SetDelay(i * wordDelay);
            }

            if (wordCanvasGroup != null)
            {
                wordCanvasGroup.DOFade(1f, animDuration).SetDelay(i * wordDelay);
            }
        }

        float totalAnimTime = (words.Length * wordDelay) + currentDialog.duration;
        if (canvasGroup != null)
        {
            canvasGroup.DOFade(0f, 0.5f).SetDelay(totalAnimTime).OnComplete(() =>
            {
                if (dialogObj != null) Destroy(dialogObj.gameObject);
                PlayNextDialog();
            });
        }
        else
        {
            StartCoroutine(DelayedDestroy(dialogObj.gameObject, totalAnimTime + 0.5f));
        }
    }

    private IEnumerator DelayedDestroy(GameObject go, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (go != null) Destroy(go);
        PlayNextDialog();
    }

    private IEnumerator ContinueAfterShortDelay()
    {
        yield return new WaitForSeconds(0.2f);
        PlayNextDialog();
    }
}

[Serializable]
public class DialogParams
{
    public string message;
    public float duration;
    public Color color = Color.white;
}

[Serializable]
public class DialogEvent : UnityEvent<DialogParams> { }
