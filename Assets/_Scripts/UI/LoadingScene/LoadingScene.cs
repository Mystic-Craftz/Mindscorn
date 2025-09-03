using DG.Tweening;
using FMODUnity;
using UnityEngine;
using UnityEngine.UI;

public class LoadingScene : MonoBehaviour
{
    [SerializeField] private Image progressbar;
    [SerializeField] private CanvasGroup blackImage;
    [SerializeField] private EventReference typewriterScrollSound;

    private AsyncOperation scene;
    private void Start()
    {
        progressbar.fillAmount = 0;
        blackImage.DOFade(0f, 1f).OnComplete(() =>
        {
            scene = SceneLoader.LoaderCallback();
            scene.allowSceneActivation = false;

            do
            {
                progressbar.fillAmount = Mathf.Clamp01(scene.progress / 0.9f);
            } while (scene.progress < 0.9f);

            blackImage.DOFade(1f, 1f).OnPlay(() => { AudioManager.Instance.PlayOneShot(typewriterScrollSound, transform.position); }).OnComplete(() => scene.allowSceneActivation = true);
        });
    }
}
