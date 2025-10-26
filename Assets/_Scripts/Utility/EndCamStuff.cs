using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
public class EndCamStuff : MonoBehaviour
{
    [SerializeField] private Camera mainCam;
    [SerializeField] private CinemachineCamera cam;
    [SerializeField] private float delay = 1f;
    [SerializeField] private int blinks = 3;

    public void SetCamBlend()
    {
        if (mainCam != null)
        {
            var brain = mainCam.GetComponent<CinemachineBrain>();
            if (brain != null)
            {
                brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);
            }
        }
    }

    public void FastCut()
    {
        if (cam != null)
        {
            cam.Priority = 100;
            StartCoroutine(ResetPriorityAfterDelay(delay));
        }
    }

    private IEnumerator ResetPriorityAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        cam.Priority = 0;
        NeonDimensionController.Instance.PlayGlitch(blinks);
        DialogUI.Instance.ShowDialog("Aaaaghhh", 2f);
    }

    public void camSwitch()
    {
        cam.Priority = 100;
    }
}