using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader
{
    public enum Scene
    {
        MainMenuScene,
        DEMOBUILD_MainScene,
        MainScene,
        LoadingScene
    }

    private static Scene targetScene;

    public static void Load(Scene targetScene)
    {
        SceneLoader.targetScene = targetScene;

        SceneManager.LoadScene(Scene.LoadingScene.ToString());
    }

    public static AsyncOperation LoaderCallback()
    {
        var scene = SceneManager.LoadSceneAsync(targetScene.ToString());
        return scene;
    }
}
