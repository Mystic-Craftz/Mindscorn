using System.Collections.Generic;
using UnityEngine;

public class LoopingHallwayManager : MonoBehaviour
{
    [Tooltip("Item IDs required to activate certain events")]
    [SerializeField] private List<int> requiredItemIDs = new List<int>();

    [SerializeField] private bool debugLogs = true;
    [SerializeField] private int loopCounter = 0;


    //triggers
    [SerializeField] private GameObject room1;
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
        room1.SetActive(true);
    }

    public void EnterDimensionTriggerEnabler()
    {
        if (InventoryManager.Instance.HasItem(requiredItemIDs[0]))
        {
            dimensionTriggerEnter.SetActive(true);
            isEnterDimensionTriggerEnabled = true;
        }
    }

}
