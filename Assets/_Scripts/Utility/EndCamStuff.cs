using System.Collections;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class EndCamStuff : MonoBehaviour
{
    [SerializeField] private Camera mainCam;
    [SerializeField] private CinemachineCamera cam;
    [SerializeField] private float delay = 1f;
    [SerializeField] private int blinks = 3;
    [SerializeField] private Image fadeImage;
    [SerializeField] private float duration = 1f;

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

    public void StopMovement()
    {
        PlayerController.Instance.SetCanMove(false);
        EscapeMenuUI.Instance.DisableToggle();
        InventoryManager.Instance.DisableToggle();
    }

    public void FadeToBlack()
    {
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            fadeImage.color = new Color(fadeImage.color.r, fadeImage.color.g, fadeImage.color.b, 0f);
            fadeImage.DOFade(1f, duration)
                .SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    LoadCreditsScene();
                });
        }
    }

    private void LoadCreditsScene()
    {
        SceneManager.LoadScene("Credits");
    }

}