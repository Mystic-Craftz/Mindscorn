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
    [SerializeField] private GameObject lightsTrigger;
    [SerializeField] private GameObject glitchTrigger;
    [SerializeField] private GameObject movementStopTrigger;
    [SerializeField] private GameObject room1Dialogue;


    //Gameobject
    [SerializeField] private GameObject movingBodies;
    [SerializeField] private GameObject PlayerArm;
    [SerializeField] private GameObject Revolver;
    [SerializeField] private GameObject theThing;
    [SerializeField] private GameObject giantThing;
    [SerializeField] private CinemachineCamera cam;




    //extra stuff
    [SerializeField] private float movingBodiesEnableDelay = 5f;

    //  Light sequence 
    [Header("Auto Lights Sequence")]
    [Tooltip("Indices of the lights that should be toggled.")]
    [SerializeField] private int[] autoLightsIndices = new int[] { 2, 3, 12, 13 };

    [Tooltip("Time between toggling each light off/on (seconds).")]
    [SerializeField] private float autoLightsGap = 0.12f;

    [Tooltip("How long all selected lights stay OFF each cycle (seconds).")]
    [SerializeField] private float autoLightsOffHold = 1.0f;

    [Tooltip("If true, lights are turned ON in reverse order.")]
    [SerializeField] private bool autoLightsTurnOnReverse = true;

    [Tooltip("If true, WaitForSecondsRealtime is used (ignores timescale).")]
    [SerializeField] private bool autoLightsUseRealtime = false;

    [Tooltip("Duration of the whole sequence (seconds).")]
    [SerializeField] private float autoLightsDuration = 5f;

    private bool _autoLightsRunning = false;
    private Dictionary<int, bool> _autoSavedStates = null;



    //Flags
    private bool isEnterDimensionTriggerEnabled = false;
    private bool isDoor1Open = false;
    private bool isDoor3Open = false;
    private bool isDoor4Open = false;
    private bool isLightsTrigger = false;
    private bool triggerTheThing = false;
    private bool isSoundTrigger = false;
    private bool isGlitchTrigger = false;

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

        // its value is 3
        if (loopCounter == 3 && !isSoundTrigger)
        {
            LightsTriggerLoop3();
        }

        // its value is 4
        if (loopCounter == 4 && !isLightsTrigger)
        {
            LightsOffTrigger();
        }

        // its value is 5
        if (loopCounter == 5 && !isGlitchTrigger)
        {
            GlitchTrigger();
        }

        // its value is 6
        if (loopCounter == 6 && !isDoor3Open)
        {
            OpenDoor3();
        }

        // its value is 7
        if (loopCounter == 0 && !isDoor4Open)
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
            room1Dialogue.SetActive(true);
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
            movementStopTrigger.SetActive(true);
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

    public void TurnOnRedLights()
    {
        lights[18].SetActive(true);
        lights[19].SetActive(true);
        lights[20].SetActive(true);
        lights[21].SetActive(true);
        lights[12].SetActive(false);
        lights[13].SetActive(false);
    }

    public void Loop6LightsOn()
    {
        lights[18].SetActive(true);
        lights[19].SetActive(true);
        lights[20].SetActive(true);
        lights[21].SetActive(true);
        lights[6].SetActive(true);
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
        EscapeMenuUI.Instance.DisableToggle();
        InventoryManager.Instance.DisableToggle();
    }

    public void StartMovement()
    {
        PlayerController.Instance.SetCanMove(true);
        EscapeMenuUI.Instance.EnableToggle();
        InventoryManager.Instance.EnableToggle();
    }

    public void LightsTriggerLoop3()
    {
        lightsTrigger.SetActive(true);
        isSoundTrigger = true;
    }

    public void GlitchTrigger()
    {
        glitchTrigger.SetActive(true);
        isGlitchTrigger = true;
    }


    // Auto Lights
    public void TriggerLightsAutoEvent()
    {
        if (_autoLightsRunning) return;
        if (lights == null || lights.Count == 0) { Debug.LogWarning("TriggerLightsAutoEvent: 'lights' list not assigned."); return; }

        var sanitized = new List<int>();
        foreach (var i in autoLightsIndices)
        {
            if (i >= 0 && i < lights.Count && !sanitized.Contains(i))
                sanitized.Add(i);
        }
        if (sanitized.Count == 0) { Debug.LogWarning("TriggerLightsAutoEvent: no valid indices in autoLightsIndices."); return; }

        StartCoroutine(AutoLightsCoroutine(sanitized.ToArray(), Mathf.Max(0f, autoLightsGap), Mathf.Max(0f, autoLightsOffHold), autoLightsTurnOnReverse, autoLightsUseRealtime, autoLightsDuration));
    }


    private IEnumerator AutoLightsCoroutine(int[] indices, float gap, float offHold, bool turnOnReverse, bool useRealtime, float duration)
    {
        _autoLightsRunning = true;

        // save original states
        _autoSavedStates = new Dictionary<int, bool>();
        foreach (var i in indices)
        {
            if (i >= 0 && i < lights.Count)
                _autoSavedStates[i] = (lights[i] != null) ? lights[i].activeSelf : false;
        }

        float start = Time.realtimeSinceStartup;
        bool doRepeat = duration > 0f;

        // run at least one cycle
        do
        {
            // turn OFF one-by-one
            for (int k = 0; k < indices.Length; k++)
            {
                int idx = indices[k];
                if (idx >= 0 && idx < lights.Count && lights[idx] != null)
                    lights[idx].SetActive(false);

                if (gap > 0f)
                {
                    if (useRealtime) yield return new WaitForSecondsRealtime(gap);
                    else yield return new WaitForSeconds(gap);
                }
                else yield return null;
            }

            // hold all-off
            if (offHold > 0f)
            {
                if (useRealtime) yield return new WaitForSecondsRealtime(offHold);
                else yield return new WaitForSeconds(offHold);
            }
            else yield return null;

            // turn ON one-by-one (respect originally saved state)
            int[] onOrder = (int[])indices.Clone();
            if (turnOnReverse) System.Array.Reverse(onOrder);

            for (int k = 0; k < onOrder.Length; k++)
            {
                int idx = onOrder[k];
                if (idx >= 0 && idx < lights.Count && lights[idx] != null)
                {
                    bool originallyOn = _autoSavedStates.ContainsKey(idx) ? _autoSavedStates[idx] : true;
                    lights[idx].SetActive(originallyOn);
                }

                if (gap > 0f)
                {
                    if (useRealtime) yield return new WaitForSecondsRealtime(gap);
                    else yield return new WaitForSeconds(gap);
                }
                else yield return null;
            }

            // if duration <= 0 -> run only once
            if (!doRepeat) break;

            // check elapsed and stop if exceeded
            if (Time.realtimeSinceStartup - start >= duration) break;

            // tiny pause before next cycle (prevents immediate restart)
            if (useRealtime) yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, gap));
            else yield return new WaitForSeconds(Mathf.Max(0.01f, gap));

        } while (Time.realtimeSinceStartup - start < duration);

        // restore saved states (safety)
        if (_autoSavedStates != null)
        {
            foreach (var kv in _autoSavedStates)
            {
                int idx = kv.Key;
                bool state = kv.Value;
                if (idx >= 0 && idx < lights.Count && lights[idx] != null)
                    lights[idx].SetActive(state);
            }
        }

        _autoSavedStates = null;
        _autoLightsRunning = false;
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
