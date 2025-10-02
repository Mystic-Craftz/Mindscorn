using System.Collections.Generic;
using UnityEngine;

public class LoopingHallwayManager : MonoBehaviour
{
    [SerializeField] private List<int> requiredItemIDs = new List<int>();

    [SerializeField] private List<Light> pointLights = new List<Light>();

    [SerializeField] private List<Light> spotLights = new List<Light>();

    [SerializeField] private bool debugLogs = true;
    [SerializeField] private int loopCounter = 0;

    //for material
    [SerializeField] private Material wallsMaterial;
    private float wearAsecond = 0.83f;
    private float wearBsecond = 0.2f;
    private float wearAlast = 1f;
    private float wearBlast = 1f;

    //triggers
    [SerializeField] private GameObject room1Trigger;
    [SerializeField] private GameObject dimensionTriggerEnter;
    [SerializeField] private GameObject dimensionTriggerExit;

    private bool isEnterDimensionTriggerEnabled = false;


    public void IncreaseLoopCounter()
    {
        loopCounter++;

        if (debugLogs)
            Debug.Log("Loop Counter: " + loopCounter);
    }

    private void Update()
    {
        if (!isEnterDimensionTriggerEnabled)
        {
            EnterDimensionTriggerEnabler();
        }

        if (loopCounter == 2)
        {
            OpenDoor1();
        }
    }

    public void OpenDoor1()
    {
        room1Trigger.SetActive(true);
    }

    public void EnterDimensionTriggerEnabler()
    {
        if (InventoryManager.Instance.HasItem(requiredItemIDs[0]))
        {
            dimensionTriggerEnter.SetActive(true);
            isEnterDimensionTriggerEnabled = true;
        }
    }

    // Material
    public void ApplySecondWear()
    {
        SetWearValuesOnOriginal(wearAsecond, wearBsecond);
    }

    public void ApplyLastWear()
    {
        SetWearValuesOnOriginal(wearAlast, wearBlast);
    }

    private void SetWearValuesOnOriginal(float wearA, float wearB)
    {
        if (wallsMaterial == null)
        {
            Debug.LogWarning("LoopingHallwayManager: wallsMaterial not assigned.");
            return;
        }

        // Candidate property names — expand if your shader uses different names
        string[] candidatesA = { "_WearA", "WearA", "_Wear_A", "Wear_A", "wearA" };
        string[] candidatesB = { "_WearB", "WearB", "_Wear_B", "Wear_B", "wearB" };

        bool setA = TrySetFloatProperty(wallsMaterial, candidatesA, wearA);
        bool setB = TrySetFloatProperty(wallsMaterial, candidatesB, wearB);

        if (debugLogs)
            Debug.Log($"SetWearValuesOnOriginal called. wearA set: {setA}, wearB set: {setB} (values: {wearA}, {wearB})");
    }

    private bool TrySetFloatProperty(Material mat, IEnumerable<string> candidateNames, float value)
    {
        foreach (var name in candidateNames)
        {
            if (mat.HasProperty(name))
            {
                mat.SetFloat(name, value);
                return true;
            }
        }
        return false;
    }

}
