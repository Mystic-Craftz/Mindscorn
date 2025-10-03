using System.Collections;
using UnityEngine;

public class DimensionToggleOnInspect : MonoBehaviour, IAmInteractable
{
    [Header("Dialog (optional)")]
    [SerializeField] private bool showDialogOnInteract = false;
    [SerializeField] private DialogSO dialogSO = null;
    [SerializeField] private float dialogDuration = 2f;

    [Header("Toggle behavior")]
    [Tooltip("If true, toggle happens AFTER dialogDuration. If false, toggle happens immediately on Interact.")]
    [SerializeField] private bool toggleAfterDialog = true;

    public void Interact()
    {
        if (showDialogOnInteract && dialogSO != null)
        {
            if (DialogUI.Instance != null)
                DialogUI.Instance.ShowDialog(dialogSO.text, dialogDuration);

            if (toggleAfterDialog)
            {
                StartCoroutine(DelayedToggleCoroutine(dialogDuration));
                return;
            }
        }

        ToggleDimensionInstant();
    }

    private IEnumerator DelayedToggleCoroutine(float delay)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, delay));
        ToggleDimensionInstant();
    }

    public void ToggleDimensionInstant()
    {
        if (NeonDimensionController.Instance == null)
        {
            return;
        }

        if (NeonDimensionController.Instance.IsInNeonDimension())
            NeonDimensionController.Instance.ReturnToNormalInstant();
        else
            NeonDimensionController.Instance.EnterNeonDimension();
    }

    public void EnterDimensionInstant()
    {

        NeonDimensionController.Instance.EnterNeonDimension();
    }

    public void ExitDimensionInstant()
    {

        NeonDimensionController.Instance.ReturnToNormalInstant();
    }

    public bool ShouldShowInteractionUI()
    {
        return true;
    }
}
