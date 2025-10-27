using System.Collections.Generic;
using UnityEngine;
using System.IO;
using DG.Tweening;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [SerializeField] private CanvasGroup blackImage;

    private string saveFilePath;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            saveFilePath = Path.Combine(Application.persistentDataPath, "save.json");
        }
        else Destroy(gameObject);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            SaveGame();
        }
        else if (Input.GetKeyDown(KeyCode.K))
        {
            LoadGame();
        }
        else if (Input.GetKeyDown(KeyCode.L))
        {

            File.Delete(saveFilePath);

        }
    }

    private void Start()
    {
        LoadGame();
    }

    public void SaveGame()
    {
        // Debug.Log("Saving game...");
        Dictionary<string, string> stateDict = new();

        foreach (var saveable in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (saveable is ISaveable saveObj)
            {
                string id = saveObj.GetUniqueIdentifier();
                string json = JsonUtility.ToJson(saveObj.CaptureState());
                stateDict[id] = json;
            }
        }

        string jsonFile = JsonUtility.ToJson(new SerializationWrapper(stateDict), true);
        File.WriteAllText(saveFilePath, jsonFile);
        // Debug.Log("Saving complete.");
        // blackImage.DOFade(0, 1f);
    }

    public void LoadGame()
    {
        if (!File.Exists(saveFilePath))
        {
            blackImage.DOFade(1, 0f);
            IntroTalkSlideshowUI.Instance.StartSlideshow(() =>
            {
                blackImage.DOFade(0, 0f);
            });
            return;
        }
        else
        {
            blackImage.DOFade(1, 0f);
        }
        // Debug.Log("Loading game...");

        string json = File.ReadAllText(saveFilePath);
        var wrapper = JsonUtility.FromJson<SerializationWrapper>(json);
        var savedStates = wrapper.ToDictionary();

        foreach (var saveable in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (saveable is ISaveable saveObj)
            {
                string id = saveObj.GetUniqueIdentifier();
                if (savedStates.TryGetValue(id, out var savedJson))
                {
                    saveObj.RestoreState(savedJson);
                }
            }
        }
        blackImage.DOFade(0, 1f).SetDelay(.5f);
        // Debug.Log("Loading complete.");
    }

    [System.Serializable]
    private class SerializationWrapper
    {
        public List<string> keys = new();
        public List<string> jsonValues = new();

        public SerializationWrapper(Dictionary<string, string> dict)
        {
            foreach (var pair in dict)
            {
                keys.Add(pair.Key);
                jsonValues.Add(pair.Value);
            }
        }

        public Dictionary<string, string> ToDictionary()
        {
            var dict = new Dictionary<string, string>();
            for (int i = 0; i < keys.Count; i++)
            {
                dict[keys[i]] = jsonValues[i];
            }
            return dict;
        }
    }
}