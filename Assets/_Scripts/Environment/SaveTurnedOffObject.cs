using UnityEngine;

[RequireComponent(typeof(SaveableEntity))]
public class SaveTurnedOffObject : MonoBehaviour, ISaveable
{
    public object CaptureState()
    {
        return new SaveData { isObjectActive = gameObject.activeSelf };
    }

    public string GetUniqueIdentifier()
    {
        return GetComponent<SaveableEntity>().UniqueId;
    }

    public void RestoreState(object state)
    {
        string json = state as string;
        SaveData data = JsonUtility.FromJson<SaveData>(json);
        gameObject.SetActive(data.isObjectActive);
    }

    public class SaveData
    {
        public bool isObjectActive;
    }
}
