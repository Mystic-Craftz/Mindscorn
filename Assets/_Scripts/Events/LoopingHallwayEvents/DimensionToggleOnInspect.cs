using System.Collections;
using UnityEngine;

public class DimensionToggleOnInspect : MonoBehaviour, IAmInteractable
{
    [SerializeField] private GameObject eyes;
    [SerializeField] private GameObject newspapers;
    [SerializeField] private GameObject bottles;
    [SerializeField] private GameObject light1;
    [SerializeField] private GameObject light2;

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
            return;

        if (NeonDimensionController.Instance.IsInNeonDimension())
            ExitDimensionInstant();
        else
            EnterDimensionInstant();
    }

    public void EnterDimensionInstant()
    {
        NeonDimensionController.Instance.EnterNeonDimension();
        StartCoroutine(EnableEyesAndLight2WithDelay(0.5f));
    }

    public void ExitDimensionInstant()
    {
        NeonDimensionController.Instance.ReturnToNormalInstant();

        if (eyes != null)
            eyes.SetActive(false);

        if (light2 != null)
            light2.SetActive(false);

        if (light1 != null)
            light1.SetActive(true);

        if (newspapers != null)
            newspapers.SetActive(true);

        if (bottles != null)
            bottles.SetActive(true);
    }

    private IEnumerator EnableEyesAndLight2WithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (eyes != null)
            eyes.SetActive(true);

        if (light1 != null)
            light1.SetActive(false);

        if (light2 != null)
            light2.SetActive(true);

        if (newspapers != null)
            newspapers.SetActive(false);

        if (bottles != null)
            bottles.SetActive(false);
    }

    public bool ShouldShowInteractionUI()
    {
        return true;
    }
}
