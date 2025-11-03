using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectiveProgressionManager : MonoBehaviour, ISaveable
{
    public static ObjectiveProgressionManager Instance { get; private set; }

    [Tooltip("Item IDs required to activate certain events")]
    [SerializeField] private List<int> requiredItemIDs = new List<int>();

    [Tooltip("List of events that will be activated when certain items are collected")]
    [SerializeField] private List<GameObject> eventObjectsToActivate = new List<GameObject>();
    [SerializeField] private InventoryItemSO revolverManual;

    //Event Flags 
    private bool isFirstEncounter = false;

    private bool[] bodiesInpected = { false, false, false };


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        eventObjectsToActivate[0].SetActive(false);
        eventObjectsToActivate[1].SetActive(false);
    }

    public void FinishFindMainEntranceKeyKey()
    {
        CheckpointManager.Instance.CompleteObjectiveById(0.1f);
        SaveManager.Instance.SaveGame();
        NotificationUI.Instance.ShowNotification("Objective Updated");
    }

    public void FinishInvestigatingHotel()
    {
        CheckpointManager.Instance.CompleteObjectiveById(0.2f);
        SaveManager.Instance.SaveGame();
        NotificationUI.Instance.ShowNotification("Objective Updated");
    }

    public void FinishInvestigateBasement()
    {
        CheckpointManager.Instance.CompleteObjectiveById(0.2f);
        CheckpointManager.Instance.CompleteObjectiveById(1.1f);
        SaveManager.Instance.SaveGame();
        NotificationUI.Instance.ShowNotification("Objective Updated");
    }

    public void FinishCheckNoiseUpstairs()
    {
        CheckpointManager.Instance.CompleteObjectiveById(2.1f);
        CheckpointManager.Instance.CompleteObjectiveById(2.2f);
        SaveManager.Instance.SaveGame();
        NotificationUI.Instance.ShowNotification("Objective Updated");
    }

    public void Finish2o2InvestigateBasement()
    {
        if (CheckpointManager.Instance.CheckIfObjectiveIsCompletedById(2.2f)) return;
        CheckpointManager.Instance.CompleteObjectiveById(2.2f);
        SaveManager.Instance.SaveGame();
        NotificationUI.Instance.ShowNotification("Objective Updated");
    }

    public void FinishGoToReception()
    {
        if (CheckpointManager.Instance.CheckIfObjectiveIsCompletedById(3.1f)) return;
        CheckpointManager.Instance.CompleteObjectiveById(2.1f);
        CheckpointManager.Instance.CompleteObjectiveById(2.2f);
        CheckpointManager.Instance.CompleteObjectiveById(3.1f);
        SaveManager.Instance.SaveGame();
        NotificationUI.Instance.ShowNotification("Objective Updated");
    }

    public void FinishGetFuelFromReception()
    {
        CheckpointManager.Instance.CompleteObjectiveById(4.1f);
        SaveManager.Instance.SaveGame();
        NotificationUI.Instance.ShowNotification("Objective Updated");
    }

    public void FinishRefuelTheGenerator()
    {
        CheckpointManager.Instance.CompleteObjectiveById(5.1f);
        SaveManager.Instance.SaveGame();
        NotificationUI.Instance.ShowNotification("Objective Updated");
    }

    public void FinishInvestigateTheSoundUpstairs()
    {
        CheckpointManager.Instance.CompleteObjectiveById(6.1f);
        // SaveManager.Instance.SaveGame();
        NotificationUI.Instance.ShowNotification("Objective Updated");
    }

    public void FinishGetBackFromLoopingHallway()
    {
        CheckpointManager.Instance.CompleteObjectiveById(7.1f);
        NotificationUI.Instance.ShowNotification("Objective Updated");
    }

    public void FinishEscapeFromMainEntrance()
    {
        CheckpointManager.Instance.CompleteObjectiveById(8.1f);
        NotificationUI.Instance.ShowNotification("Objective Updated");
    }

    public void Finish9o1()
    {
        CheckpointManager.Instance.CompleteObjectiveById(9.1f);
        NotificationUI.Instance.ShowNotification("Objective Updated");
        SaveManager.Instance.SaveGame();
    }

    public void Finish10o1()
    {
        CheckpointManager.Instance.CompleteObjectiveById(10.1f);
        NotificationUI.Instance.ShowNotification("Objective Updated");
    }

    public void Finish11o1()
    {
        if (CheckpointManager.Instance.CheckIfObjectiveIsCompletedById(11.1f)) return;
        CheckpointManager.Instance.CompleteObjectiveById(11.1f);
        NotificationUI.Instance.ShowNotification("Objective Updated");
    }

    public void Finish12o1()
    {
        if (CheckpointManager.Instance.CheckIfObjectiveIsCompletedById(12.1f)) return;
        CheckpointManager.Instance.CompleteObjectiveById(12.1f);
        NotificationUI.Instance.ShowNotification("Objective Updated");
    }

    public void Finish12o2()
    {
        CheckpointManager.Instance.CompleteObjectiveById(12.1f);
        CheckpointManager.Instance.CompleteObjectiveById(12.2f);
        NotificationUI.Instance.ShowNotification("Objective Updated");
    }

    public void Finish13o1()
    {
        CheckpointManager.Instance.CompleteObjectiveById(13.1f);
        NotificationUI.Instance.ShowNotification("Objective Updated");
        SaveManager.Instance.SaveGame();
    }

    public void Finish14o1()
    {
        CheckpointManager.Instance.CompleteObjectiveById(14.1f);
        NotificationUI.Instance.ShowNotification("Objective Updated");
    }

    public void Finish15o1()
    {
        CheckpointManager.Instance.CompleteObjectiveById(15.1f);
        NotificationUI.Instance.ShowNotification("Objective Updated");
        SaveManager.Instance.SaveGame();
    }

    public void Finish16o1()
    {
        CheckpointManager.Instance.CompleteObjectiveById(16.1f);
        NotificationUI.Instance.ShowNotification("Objective Updated");
        SaveManager.Instance.SaveGame();
    }

    public void Finish17o1()
    {
        CheckpointManager.Instance.CompleteObjectiveById(17.1f);
        NotificationUI.Instance.ShowNotification("Objective Updated");
    }

    public void Finish18o1()
    {
        CheckpointManager.Instance.CompleteObjectiveById(18.1f);
        NotificationUI.Instance.ShowNotification("Objective Updated");
    }

    private IEnumerator DemoEndCoRoutine()
    {
        PlayerHealth.Instance.DropPlayerEyeObject();
        yield return new WaitForSeconds(2f);
        DemoEndScreenUI.Instance.Show();
    }

    public void RestrictPlayerForFinalBossEvent()
    {
        PlayerController.Instance.SetDisableSprint(true);
        PlayerWeapons.Instance.SetDisableTorch(true);
        PlayerWeapons.Instance.DisableWeaponForASection(true, true);
        EscapeMenuUI.Instance.DisableToggle();
        InventoryManager.Instance.DisableToggle();
    }

    public void UnRestrictPlayerForFinalBossEvent()
    {
        PlayerController.Instance.SetDisableSprint(false);
        PlayerWeapons.Instance.SetDisableTorch(false);
        PlayerWeapons.Instance.DisableWeaponForASection(false);
        EscapeMenuUI.Instance.EnableToggle();
        InventoryManager.Instance.EnableToggle();
    }

    public void DisablePlayerWeaponsForGiftShopMaze()
    {
        PlayerWeapons.Instance.DisableWeaponForASection(true, true);
    }

    public void EnablePlayerWeaponsForGiftShopMaze()
    {
        PlayerWeapons.Instance.DisableWeaponForASection(false);
    }

    public void DisableSprint()
    {
        PlayerController.Instance.SetDisableSprint(true);

    }

    public void EnableSprint()
    {
        PlayerController.Instance.SetDisableSprint(false);
    }

    public void KillPlayer()
    {
        PlayerHealth.Instance.TakeDamage(1000f);
    }

    public void InspectBody(int index)
    {
        if (bodiesInpected[0] && bodiesInpected[1] && bodiesInpected[2]) return;
        bodiesInpected[index] = true;
        if (bodiesInpected[0] && bodiesInpected[1] && bodiesInpected[2])
        {
            StartCoroutine(DialogCoRoutine());
        }
    }

    private IEnumerator DialogCoRoutine()
    {
        yield return new WaitForSeconds(4f);
        DialogUI.Instance.ShowDialog("None of the bodies had any wounds", 3f);
        yield return new WaitForSeconds(3f);
        DialogUI.Instance.ShowDialog("What is going on here?", 2.5f);
    }

    public string GetUniqueIdentifier()
    {
        return "ObjectiveProgressionManager";
    }

    public object CaptureState()
    {
        return new SaveData
        {
            isFirstEncounter = isFirstEncounter,
            firstBodyInpsected = bodiesInpected[0],
            secondBodyInpsected = bodiesInpected[1],
            thirdBodyInpsected = bodiesInpected[2]
        };
    }

    public void RestoreState(object state)
    {
        string json = state as string;
        SaveData data = JsonUtility.FromJson<SaveData>(json);
        isFirstEncounter = data.isFirstEncounter;
        bodiesInpected[0] = data.firstBodyInpsected;
        bodiesInpected[1] = data.secondBodyInpsected;
        bodiesInpected[2] = data.thirdBodyInpsected;
    }

    public class SaveData
    {
        public bool isFirstEncounter = false;
        public bool firstBodyInpsected;
        public bool secondBodyInpsected;
        public bool thirdBodyInpsected;
    }

    public void OpenRevolverManual()
    {
        if (revolverManual != null)
        {
            InventoryManager.Instance.AddItem(new InventoryItem { data = revolverManual, quantity = 1 });
            NoteContentUI.Instance.ShowContentFromList(revolverManual.text);
            NotificationUI.Instance.ShowNotification(new InventoryItem { data = revolverManual, quantity = 1 });
        }
    }
}



