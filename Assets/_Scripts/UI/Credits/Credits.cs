using UnityEngine;
using UnityEngine.UI;

public class Credits : MonoBehaviour
{
    [SerializeField] private Button exitButton;

    private void Start()
    {
        AudioManager.Instance.PlayMusic(12);

        exitButton.onClick.AddListener(() =>
        {
            GoToMainMenu();
        });
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void GoToMainMenu()
    {
        AudioManager.Instance.StopAllMusicImmediate();
        SceneLoader.Load(SceneLoader.Scene.MainMenuScene);
    }
}
