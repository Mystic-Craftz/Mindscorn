using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public class LoopingHallwayManager : MonoBehaviour
{
    [SerializeField] private List<int> requiredItemIDs = new List<int>();

    [SerializeField] private List<GameObject> lights = new List<GameObject>();

    [SerializeField] private bool debugLogs = true;
    [SerializeField] private int loopCounter = 0;

    //for material
    [SerializeField] private Renderer wallsRenderer;
    private Material[] wallsMaterials;
    private float wearAstart = -6.6f;
    private float wearBstart = 1f;
    private float wearAsecond = -0.5f;
    private float wearBsecond = 1f;
    private float wearAlast = 1f;
    private float wearBlast = 1f;

    //triggers
    [SerializeField] private GameObject room1Trigger;
    [SerializeField] private GameObject room3Trigger;
    [SerializeField] private GameObject room4Trigger;
    [SerializeField] private GameObject dimensionTriggerEnter;
    [SerializeField] private GameObject jumpscareTrigger;
    [SerializeField] private GameObject lightsOffTrigger;

    //Gameobject
    [SerializeField] private GameObject movingBodies;
    [SerializeField] private GameObject PlayerArm;
    [SerializeField] private GameObject Revolver;
    [SerializeField] private GameObject theThing;
    [SerializeField] private GameObject giantThing;
    [SerializeField] private CinemachineCamera cam;




    //extra stuff
    [SerializeField] private float movingBodiesEnableDelay = 5f;


    //Flags
    private bool isEnterDimensionTriggerEnabled = false;
    private bool isDoor1Open = false;
    private bool isDoor3Open = false;
    private bool isDoor4Open = false;
    private bool isLightsTrigger = false;
    private bool triggerTheThing = false;

    private void Start()
    {
        wallsMaterials = wallsRenderer.materials;
        SetWearValuesOnOriginal(wearAstart, wearBstart);
    }

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

        if (!triggerTheThing)
        {
            TriggerTheThing();
        }

        // its value is 2
        if (loopCounter == 2 && !isDoor1Open)
        {
            OpenDoor1();
        }

        // its value is 4
        if (loopCounter == 4 && !isLightsTrigger)
        {
            LightsOffTrigger();
        }

        // its value is 6
        if (loopCounter == 6 && !isDoor3Open)
        {
            OpenDoor3();
        }

        // its value is 7
        if (loopCounter == 7 && !isDoor4Open)
        {
            OpenDoor4();
        }

    }

    //Doors
    public void OpenDoor1()
    {
        room1Trigger.SetActive(true);
        isDoor1Open = true;
    }

    public void OpenDoor3()
    {
        room3Trigger.SetActive(true);
        isDoor3Open = true;
    }

    public void OpenDoor4()
    {
        room4Trigger.SetActive(true);
        isDoor4Open = true;
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
        StopMovement();
    }

    public void FlickeringLightOn()
    {
        lights[0].SetActive(false);
        lights[1].SetActive(false);

        lights[2].SetActive(true);
        lights[3].SetActive(true);
    }

    public void LightsOff()
    {
        lights[2].SetActive(false);
        lights[3].SetActive(false);
    }

    public void LightsOn()
    {
        lights[0].SetActive(true);
        lights[1].SetActive(true);
    }

    public void LightsOffTrigger()
    {
        jumpscareTrigger.SetActive(true);
        lightsOffTrigger.SetActive(true);
        isLightsTrigger = true;
    }

    private void TriggerTheThing()
    {
        if (InventoryManager.Instance.HasItem(requiredItemIDs[1]))
        {
            StopMovement();
            StartCoroutine(HandleItem31Sequence());
            triggerTheThing = true;
        }
    }

    public void TurnOffAllLights()
    {
        for (int i = 0; i < lights.Count; i++)
        {
            if (lights[i] == null) continue;
            lights[i].SetActive(false);
        }
    }

    public void TriggerGiantThing()
    {
        cam.Priority = 100;
        StartCoroutine(EnableTheThing());
    }


    private IEnumerator EnableTheThing()
    {
        yield return new WaitForSeconds(1f);
        giantThing.SetActive(true);
    }

    public void StopMovement()
    {
        PlayerController.Instance.SetCanMove(false);
    }

    public void StartMovement()
    {
        PlayerController.Instance.SetCanMove(true);
    }

    private IEnumerator HandleItem31Sequence()
    {
        NeonDimensionController.Instance.ReturnToNormalInstant();
        yield return new WaitForSeconds(2f);
        lights[4].SetActive(false);
        lights[5].SetActive(false);
        lights[0].SetActive(false);
        lights[1].SetActive(false);

        yield return new WaitForSeconds(2f);
        theThing.SetActive(true);
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
        if (wallsMaterials.Length < 1)
        {
            Debug.LogWarning("LoopingHallwayManager: wallsMaterial not assigned.");
            return;
        }

        string[] candidatesA = { "_WearA", "WearA", "_Wear_A", "Wear_A", "wearA" };
        string[] candidatesB = { "_WearB", "WearB", "_Wear_B", "Wear_B", "wearB" };

        foreach (var mat in wallsMaterials)
        {
            bool setA = TrySetFloatProperty(mat, candidatesA, wearA);
            bool setB = TrySetFloatProperty(mat, candidatesB, wearB);

            if (debugLogs)
                Debug.Log($"SetWearValuesOnOriginal called. wearA set: {setA}, wearB set: {setB} (values: {wearA}, {wearB})");
        }

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
