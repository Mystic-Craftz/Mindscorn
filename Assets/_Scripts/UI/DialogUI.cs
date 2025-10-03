using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using NUnit.Framework.Constraints;
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
    HorizontalLayoutGroup horizontalLayoutGroup;
    Transform dialogObj;
    CanvasGroup canvasGroup;

    private bool canShowDoorDialog = true;

    private void Awake() => Instance = this;

    private void Start()
    {
        dialogTextTemplate.SetActive(false);
    }

    private void Update()
    {
        if (isShowingDialog)
        {
            dialogObj.gameObject.SetActive(false);
            horizontalLayoutGroup.CalculateLayoutInputHorizontal();
            horizontalLayoutGroup.CalculateLayoutInputVertical();
            horizontalLayoutGroup.SetLayoutVertical();
            horizontalLayoutGroup.SetLayoutHorizontal();
            dialogObj.gameObject.SetActive(true);
        }
    }

    public void ShowDialog(string message, float duration = 2f, Color? color = null)
    {
        Color finalColor = Color.white;
        if (color != null) finalColor = (Color)color;
        EnqueueDialog(new DialogParams { message = message, duration = duration, color = finalColor });
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

        string[] words = currentDialog.message.Split(' ');
        float wordDelay = 0.15f; // delay between each word animation
        float animDuration = 0.2f;

        dialogObj = Instantiate(dialogParent, transform);
        dialogObj.gameObject.SetActive(false);
        horizontalLayoutGroup = dialogObj.GetComponent<HorizontalLayoutGroup>();

        canvasGroup = dialogObj.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 1;

        for (int i = 0; i < words.Length; i++)
        {
            var wordObj = Instantiate(dialogTextTemplate, dialogObj.transform);
            var wordCanvasGroup = wordObj.GetComponent<CanvasGroup>();
            wordCanvasGroup.alpha = 0;
            wordObj.SetActive(true);

            var text = wordObj.GetComponent<TextMeshProUGUI>();
            text.color = currentDialog.color;
            text.text = words[i];
            wordCanvasGroup.DOFade(1, animDuration)
            .SetDelay(i * wordDelay);

            RectTransform rt = wordObj.GetComponent<RectTransform>();


            // Reset scale/position
            rt.localScale = Vector3.one;
            text.margin = new Vector4(0, 198.2f, 0, 0);

            DOTween.To(() => text.margin, x => text.margin = x, new Vector4(0, 0, 0, 0), animDuration);
        }
        // After all words + duration → fade out
        float totalAnimTime = (words.Length * wordDelay) + currentDialog.duration;
        canvasGroup.DOFade(0, 0.5f).SetDelay(totalAnimTime).OnComplete(() =>
        {
            Destroy(dialogObj.gameObject);
            PlayNextDialog();
        });
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