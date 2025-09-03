using System.Collections;
using UnityEngine;

public class PeepingHoleEvent : MonoBehaviour, ISaveable
{
    [SerializeField] private GameObject abominationOfFlesh;
    [SerializeField] private GameObject parasiteBreedingBody;

    private bool hasBeenActivated = false;

    private void Start()
    {
        abominationOfFlesh.SetActive(false);
    }

    public void StartPeepingHoleEvent()
    {
        if (hasBeenActivated) return;
        hasBeenActivated = true;
        abominationOfFlesh.SetActive(true);
        parasiteBreedingBody.SetActive(true);
        abominationOfFlesh.GetComponent<Animator>().CrossFade("PlantingParasite", 0f);
        parasiteBreedingBody.GetComponent<Animator>().SetBool("IsShaking", true);
        parasiteBreedingBody.GetComponent<Animator>().CrossFade("BodyShaking", 0f);
    }

    public void StopPeepingHoleEvent()
    {
        StartCoroutine(EndAfterDuration());
    }

    private IEnumerator EndAfterDuration()
    {
        yield return new WaitForSeconds(3);
        abominationOfFlesh.SetActive(false);
        parasiteBreedingBody.GetComponent<Animator>().SetBool("IsShaking", false);
        parasiteBreedingBody.GetComponent<PeepingHoleParasiteBreedingBody>().StopBlood();
    }

    public string GetUniqueIdentifier()
    {
        return GetComponent<SaveableEntity>().UniqueId;
    }

    public object CaptureState()
    {
        return new SaveData
        {
            hasBeenActivated = hasBeenActivated
        };
    }

    public void RestoreState(object state)
    {
        string json = state as string;
        SaveData data = JsonUtility.FromJson<SaveData>(json);
        hasBeenActivated = data.hasBeenActivated;
    }

    public class SaveData
    {
        public bool hasBeenActivated;
    }
}
