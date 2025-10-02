using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoopingHallwayManager : MonoBehaviour
{
    [SerializeField] private List<int> requiredItemIDs = new List<int>();

    [SerializeField] private List<GameObject> lights = new List<GameObject>();

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
    [SerializeField] private GameObject jumpscareTrigger;
    [SerializeField] private GameObject lightsOffTrigger;

    //Gameobject
    [SerializeField] private GameObject movingBodies;
    [SerializeField] private GameObject PlayerArm;
    [SerializeField] private GameObject Revolver;




    //extra stuff
    [SerializeField] private float movingBodiesEnableDelay = 5f;


    //Flags
    private bool isEnterDimensionTriggerEnabled = false;
    private bool isDoor1Open = false;

    private bool isLightsTrigger = false;

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

        //for testing it is 0 but its value is 2
        if (loopCounter == 2 && !isDoor1Open)
        {
            OpenDoor1();
        }

        //for testing it is gonna be 0 but its value is 4
        if (loopCounter == 4 && !isLightsTrigger)
        {
            LightsOffTrigger();
        }

    }

    //Doors
    public void OpenDoor1()
    {
        room1Trigger.SetActive(true);
        isDoor1Open = true;
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

    //other stuff
    public void EnterDimensionTriggerEnabler()
    {
        if (InventoryManager.Instance.HasItem(requiredItemIDs[0]))
        {
            dimensionTriggerEnter.SetActive(true);
            isEnterDimensionTriggerEnabled = true;
            StartCoroutine(EnableMovingBodiesAfterDelay(movingBodiesEnableDelay));
        }
    }

    public void PlayDeathByRevolverAnimation()
    {
        PlayerArm.SetActive(true);
        Revolver.SetActive(false);
    }

    public void FlickeringLightOn()
    {
        lights[0].SetActive(false);
        lights[1].SetActive(false);

        lights[3].SetActive(true);
        lights[4].SetActive(true);
    }

    public void LightsOff()
    {
        lights[3].SetActive(false);
        lights[4].SetActive(false);
    }

    public void LightsOffTrigger()
    {
        jumpscareTrigger.SetActive(true);
        lightsOffTrigger.SetActive(true);
        isLightsTrigger = true;
    }


    //Helper and Courroutines
    private IEnumerator EnableMovingBodiesAfterDelay(float delay)
    {
        if (debugLogs) Debug.Log($"EnableMovingBodiesAfterDelay started, waiting {delay} seconds...");
        yield return new WaitForSeconds(delay);

        if (movingBodies == null)
        {
            Debug.LogWarning("LoopingHallwayManager: movingBodies not assigned.");
            yield break;
        }

        movingBodies.SetActive(true);
    }


    private void SetWearValuesOnOriginal(float wearA, float wearB)
    {
        if (wallsMaterial == null)
        {
            Debug.LogWarning("LoopingHallwayManager: wallsMaterial not assigned.");
            return;
        }

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
