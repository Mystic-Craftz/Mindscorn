using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenePartLoader : MonoBehaviour
{
    [SerializeField] private bool shouldLog;
    private bool isLoaded;
    private bool shouldLoad;
    private bool isPlayerStaying = false;

    private void Start()
    {
        LoadScene();
        // if (SceneManager.sceneCount > 0)
        // {
        //     for (int i = 0; i < SceneManager.sceneCount; i++)
        //     {
        //         if (SceneManager.GetSceneAt(i).name == gameObject.name)
        //         {
        //             isLoaded = true;
        //         }
        //     }
        // }
    }

    private void Update()
    {
        TriggerCheck();
    }

    private void TriggerCheck()
    {
        if (shouldLoad && isPlayerStaying)
        {
            LoadScene();
        }
        else
        {
            UnloadScene();
        }
    }

    private void LoadScene()
    {
        if (!isLoaded)
        {
            SceneManager.LoadSceneAsync(gameObject.name, LoadSceneMode.Additive);
            isLoaded = true;
        }
        // Debug.Log(isPlayerStaying);
    }

    private void UnloadScene()
    {
        if (isLoaded && !isPlayerStaying)
        {
            SceneManager.UnloadSceneAsync(gameObject.name);
            isLoaded = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (shouldLog)
            Debug.Log("Trigger Enter");
        if (other.CompareTag("Player") && !isPlayerStaying)
        {
            shouldLoad = true;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (shouldLog)
        {

            Debug.Log("Trigger Stay");
            Debug.Log(other.name);
        }
        if (other.CompareTag("Player"))
        {
            if (shouldLog)
                Debug.Log("1");
            isPlayerStaying = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (shouldLog)
            Debug.Log("Trigger Exit");
        if (other.CompareTag("Player"))
        {
            shouldLoad = false;
            isPlayerStaying = false;
        }
    }
}
