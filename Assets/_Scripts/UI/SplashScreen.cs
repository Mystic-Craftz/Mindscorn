using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SplashScreen : MonoBehaviour
{
    [SerializeField] private CanvasGroup blackImg;
    [SerializeField] private Animator logoAnim;
    [SerializeField] private GameObject logoObj;
    [SerializeField] private GameObject warningObj;
    [SerializeField] private SceneLoader.Scene nextScene;

    private void Awake()
    {
        Time.timeScale = 1;
        blackImg.alpha = 1;
        logoAnim.enabled = false;
        logoObj.SetActive(true);
        warningObj.SetActive(false);
    }

    private void Start()
    {
        StartCoroutine(SplashScreenCoRoutine());
    }

    private IEnumerator SplashScreenCoRoutine()
    {
        yield return new WaitForSeconds(.5f);
        blackImg.DOFade(0, 1f).OnComplete(() =>
        {
            logoAnim.enabled = true;
        });
        yield return new WaitForSeconds(2f);
        blackImg.DOFade(1, 1f).OnComplete(() =>
        {
            logoObj.SetActive(false);
            warningObj.SetActive(true);
            // SceneManager.LoadScene(SceneLoader.Scene.MainMenuScene.ToString());
        });
        yield return new WaitForSeconds(1f);
        blackImg.DOFade(0, 1f);
        yield return new WaitForSeconds(2f);
        blackImg.DOFade(1, 1f).OnComplete(() =>
        {
            SceneManager.LoadScene(nextScene.ToString());
        });

    }
}
