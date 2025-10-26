using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(TMP_Text))]
public class ClickableText : MonoBehaviour
{

    private TMP_Text tmpTextBox;
    private Canvas canvasToCheck;
    private Camera cameraToUse;

    public delegate void ClickOnLinkEvent(string keyword);
    public static event ClickOnLinkEvent OnClickedOnLinkEvent;

    private void Awake()
    {
        tmpTextBox = GetComponent<TMP_Text>();
        canvasToCheck = GetComponentInParent<Canvas>();

        if (canvasToCheck.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            cameraToUse = null;
        }
        else
        {
            cameraToUse = canvasToCheck.worldCamera;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Vector3 mousePosition = new Vector3(eventData.position.x, eventData.position.y, 0);
        var linkTaggedText = TMP_TextUtilities.FindIntersectingLink(tmpTextBox, mousePosition, cameraToUse);

        if (linkTaggedText == -1)
        {
            return;
        }

        TMP_LinkInfo linkInfo = tmpTextBox.textInfo.linkInfo[linkTaggedText];

        string linkID = linkInfo.GetLinkID();
        if (linkID.Contains("wwww"))
        {
            Application.OpenURL(linkID);
            return;
        }

        OnClickedOnLinkEvent(linkInfo.GetLinkID());

    }

}
