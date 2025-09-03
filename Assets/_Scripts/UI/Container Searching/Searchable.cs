using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(SaveableEntity))]
public class Searchable : MonoBehaviour, IAmInteractable, ISaveable
{
    [SerializeField] private List<InventoryItem> itemsToSearchFor = new List<InventoryItem>();
    [SerializeField] private UnityEvent OnInteract;
    [SerializeField] private UnityEvent OnSearchStart;
    [SerializeField] private UnityEvent OnCancel;
    [SerializeField] private UnityEvent OnSearchComplete;
    [Header("Lock settings")]
    [SerializeField] private bool isLocked = false;
    [SerializeField] private bool shouldGenerateDynamicLoot = true;
    [SerializeField] private string lockedText;
    [SerializeField, Tooltip("Used if Is Locked is true")] private int keyID;

    [Header("Search settings")]
    [SerializeField] private float searchTimerMax = 3f;

    [Header("Random Loot Generation Settings")]
    [SerializeField]
    private ContainerSearchingUI.RandomLootGenerationData randomLootGenerationData = new ContainerSearchingUI.RandomLootGenerationData
    {
        revolverBulletsQuantityMin = 5,
        revolverGainMin = 2,
        revolverGainMax = 7,
        noBulletsRevolverGainMin = 4,
        noBulletsRevolverGainMax = 9,
        shotgunBulletsQuantityMin = 4,
        shotgunGainMin = 2,
        shotgunGainMax = 5,
        noBulletsShotgunGainMin = 3,
        noBulletsShotgunGainMax = 6,
        rifleBulletsQuantityMin = 3,
        rifleGainMin = 1,
        rifleGainMax = 5,
        noBulletsRifleGainMin = 2,
        noBulletsRifleGainMax = 6
    };

    private bool hasBeenSearched = false;
    private bool searchInitiated = false;
    private float searchTimer = 0f;
    private ContainerSearchingUI searchingUI;

    private void Start()
    {
        searchingUI = ContainerSearchingUI.Instance;
        searchTimer = 0f;
    }

    private void Update()
    {
        if (searchInitiated)
        {
            if (searchTimer >= searchTimerMax)
            {
                hasBeenSearched = true;
                searchInitiated = false;
                PlayerController.Instance.SetCanMove(true);
                PlayerWeapons.Instance.DisableWeaponFunctions(false);
                searchingUI.EndSearch(itemsToSearchFor);
                OnSearchComplete?.Invoke();
            }
            else
            {
                searchTimer += Time.deltaTime;
                searchingUI.UpdateSearchBar(searchTimer / searchTimerMax);
            }
        }
    }

    public void Interact()
    {
        if (hasBeenSearched || searchInitiated) return;

        if (isLocked)
        {
            InventoryItem item = InventoryManager.Instance.GetItemByID(keyID);
            if (item != null)
            {
                //? If user has the key
                ConfirmItemUseUI.Instance.Show(
                    item,
                    () =>
                    {
                        isLocked = false;
                        ConfirmItemUseUI.Instance.Hide();
                        InitiateSearch();
                    },
                    () =>
                    {
                        ConfirmItemUseUI.Instance.Hide();
                    }
                );
            }
            else
            {
                //? If user doesn't have key for the lock
                DialogUI.Instance.ShowDialog(lockedText);
            }
        }
        else
        {
            InitiateSearch();
        }

        OnInteract?.Invoke();
    }

    private void InitiateSearch()
    {
        OnSearchStart?.Invoke();
        searchTimer = 0f;
        searchInitiated = true;
        searchingUI.StartSearch(() =>
        {
            searchInitiated = false;
            searchTimer = 0f;
            PlayerController.Instance.SetCanMove(true);
            PlayerWeapons.Instance.DisableWeaponFunctions(false);
            OnCancel?.Invoke();
        }, shouldGenerateDynamicLoot, randomLootGenerationData);
        PlayerController.Instance.SetCanMove(false);
        PlayerWeapons.Instance.DisableWeaponFunctions(true);
    }

    public bool ShouldShowInteractionUI()
    {
        return !hasBeenSearched;
    }

    public string GetUniqueIdentifier()
    {
        return GetComponent<SaveableEntity>().UniqueId;
    }

    public object CaptureState()
    {
        return new SaveData { hasBeenSearched = hasBeenSearched, isLocked = isLocked };
    }

    public void RestoreState(object state)
    {
        string json = state as string;
        SaveData data = JsonUtility.FromJson<SaveData>(json);
        hasBeenSearched = data.hasBeenSearched;
        isLocked = data.isLocked;
    }

    [Serializable]
    private class SaveData
    {
        public bool hasBeenSearched = false;
        public bool isLocked = false;
    }
}
