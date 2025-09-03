using System;
using TMPro;
using UnityEngine;

public class DeathScreenTextEffectTemplate : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI textTemplate;

    [SerializeField]
    private Color[] colors;

    [SerializeField]
    private string[] textOptions;

    private RectTransform rect;

    [SerializeField]
    private float changeAfter = 1f;

    [SerializeField]
    private float opacityChangeSpeed = 2f;

    [SerializeField]
    private float shouldBeVisibleFor = 1f;

    [SerializeField]
    private CanvasGroup canvasGroup;

    private float changeTimer = 0f;

    private float elapsedTime = 0f;

    private Action<DeathScreenTextEffectTemplate> whenDestroy;

    public void Init(Action<DeathScreenTextEffectTemplate> destroyAction)
    {
        rect = transform.parent.GetComponent<RectTransform>();

        whenDestroy = destroyAction;

        textTemplate.color = colors[UnityEngine.Random.Range(0, colors.Length)];
        textTemplate.fontSize = UnityEngine.Random.Range(textTemplate.fontSize - 5, textTemplate.fontSize + 5);
        textTemplate.text = textOptions[UnityEngine.Random.Range(0, textOptions.Length)];
        transform.position = new Vector3(UnityEngine.Random.Range(rect.rect.xMin, rect.rect.xMax),
                  UnityEngine.Random.Range(rect.rect.yMin, rect.rect.yMax), 0) + rect.transform.position;
        canvasGroup.alpha = 0;
        elapsedTime = 0f;
    }

    private void Update()
    {
        UpdateColors();
        MangeVisibility();
    }

    private void MangeVisibility()
    {
        if (elapsedTime >= shouldBeVisibleFor)
        {
            canvasGroup.alpha -= Time.deltaTime * opacityChangeSpeed;
            if (canvasGroup.alpha <= 0)
            {
                // Hide the object in pool
                canvasGroup.alpha = 0;
                whenDestroy(this);
            }
        }
        else
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha += Time.deltaTime * opacityChangeSpeed;
            if (canvasGroup.alpha > 1) canvasGroup.alpha = 1;
        }
    }

    private void UpdateColors()
    {
        if (changeTimer >= changeAfter)
        {
            textTemplate.color = colors[UnityEngine.Random.Range(0, colors.Length)];
            textTemplate.fontSize = UnityEngine.Random.Range(textTemplate.fontSize - 5, textTemplate.fontSize + 5);
            transform.position = new Vector3(UnityEngine.Random.Range(transform.position.x + 20, transform.position.x - 20),
                  UnityEngine.Random.Range(transform.position.y + 20, transform.position.y - 20), 0);
            changeTimer = 0f;
        }
        else
        {
            changeTimer += Time.deltaTime;
        }
    }
}
