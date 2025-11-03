using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using DG.Tweening;
using System;
using FMODUnity;

[RequireComponent(typeof(SaveableEntity))]
public class InventoryManager : MonoBehaviour, ISaveable
{
    public static InventoryManager Instance { get; private set; }

    [Header("Inventory Lists")]
    [SerializeField] private List<InventoryItem> inventoryItems = new List<InventoryItem>();
    [SerializeField] private List<InventoryItem> keyItems = new List<InventoryItem>();
    [SerializeField] private List<InventoryItem> fileItems = new List<InventoryItem>();
    [SerializeField] private List<InventoryItemSO> everyAvailableItemIngame = new List<InventoryItemSO>();

    [Header("UI References")]
    [SerializeField] private GameObject crosshairUI;
    [SerializeField] private GameObject inventoryUI;
    [SerializeField] private RectTransform carouselContent;
    [SerializeField] private GameObject itemSlotPrefab;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;
    [SerializeField] private TextMeshProUGUI inventoryTabText, keyTabText, fileTabText, objectiveTabText;
    [SerializeField] private GameObject slot1Image, slot2Image, slot3Image;
    [SerializeField] private GameObject carouselObject, objectivesObject, objectivesList, currentObjectiveTextTemplate, useItemHint;


    [Header("Visual Settings")]
    [SerializeField] private float smoothTime = 0.2f;
    [SerializeField] private float selectedScale = 1.2f;
    [SerializeField] private float unselectedScale = 0.8f;
    [SerializeField] private float selectedAlpha = 1f;
    [SerializeField] private float unselectedAlpha = 0.4f;
    [SerializeField] private EventReference openSound, closeSound, buttonSound, scrollSound, popSound;

    [Header("Layout Settings (Manual)")]
    [SerializeField] private float slotSpacing = 20f;
    [SerializeField] private float defaultSlotWidth = 100f;

    private int selectedItemIndex = 0;
    private int selectedTab = 0;
    private bool isInventoryOpen = false;
    private CanvasGroup inventoryUIAlpha = null;
    private List<GameObject> currentSlots = new List<GameObject>();
    private bool isDisabled = false;
    [HideInInspector] public bool IsInventoryOpen => isInventoryOpen;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        UpdateUI();
        inventoryUIAlpha = inventoryUI.GetComponent<CanvasGroup>();
        inventoryUIAlpha.alpha = 0f;
        inventoryUI.transform.DOScale(new Vector3(1, 0, 1), 0f);
    }

    private void Update()
    {

        if (InputManager.Instance.GetInventoryOpen())
            ToggleInventory();

        if (isInventoryOpen)
        {
            if (InputManager.Instance.GetNavigateRightTriggered()) { NextItem(); }
            if (InputManager.Instance.GetNavigateLeftTriggered()) { PreviousItem(); }
            if (InputManager.Instance.GetNavigateUpTriggered()) { PreviousTab(); }
            if (InputManager.Instance.GetNavigateDownTriggered()) { NextTab(); }
            if (InputManager.Instance.GetUseItem()) { UseItem(); }

            UpdateCarousel();
        }
    }

    /// <summary>
    /// Combines smooth adjustments of both slot positions and visual transitions.
    /// </summary>
    private void UpdateCarousel()
    {
        float delta = Time.unscaledDeltaTime * 10f;
        for (int i = 0; i < currentSlots.Count; i++)
        {
            GameObject slot = currentSlots[i];
            if (slot == null)
                continue;

            // Get the RectTransform and compute target position.
            RectTransform slotRect = slot.GetComponent<RectTransform>();
            float slotWidth = slotRect.rect.width > 0 ? slotRect.rect.width : defaultSlotWidth;
            float targetX = (i - selectedItemIndex) * (slotWidth + slotSpacing);
            Vector2 targetPos = new Vector2(targetX, slotRect.anchoredPosition.y);
            slotRect.anchoredPosition = Vector2.Lerp(slotRect.anchoredPosition, targetPos, smoothTime * delta);

            // Determine animation properties based on selection.
            bool isSelected = i == selectedItemIndex;
            float targetScale = isSelected ? selectedScale : unselectedScale;
            float targetAlpha = isSelected ? selectedAlpha : unselectedAlpha;

            // Smoothly update scale.
            slot.transform.localScale = Vector3.Lerp(slot.transform.localScale, Vector3.one * targetScale, smoothTime * delta);

            // Smoothly update alpha for the slot's Image.
            Image slotImage = slot.GetComponent<Image>();
            if (slotImage != null)
            {
                Color currentColor = slotImage.color;
                currentColor.a = Mathf.Lerp(currentColor.a, targetAlpha, smoothTime * delta);
                slotImage.color = currentColor;
            }
        }
    }

    private void ToggleInventory()
    {
        if (isDisabled) return;

        isInventoryOpen = !isInventoryOpen;
        // inventoryUI.SetActive(isInventoryOpen);
        crosshairUI.SetActive(!isInventoryOpen);
        Time.timeScale = isInventoryOpen ? 0f : 1f;
        if (isInventoryOpen)
            UpdateUI();

        if (isInventoryOpen)
        {
            inventoryUI.transform.DOScale(new Vector3(1, 1, 1), 0.2f);
            inventoryUIAlpha.DOFade(1f, 0.2f);
            AudioManager.Instance.PlayOneShot(openSound, transform.position);
            PlayerController.Instance.SetCanMove(false);
            PlayerWeapons.Instance.DisableWeaponFunctions(true);
            EscapeMenuUI.Instance.DisableToggle();
            PlayerInteraction.Instance.SetDisabled(true);
        }
        else
        {
            inventoryUI.transform.DOScale(new Vector3(1, 0, 1), 0.2f);
            inventoryUIAlpha.DOFade(0f, 0.2f);
            AudioManager.Instance.PlayOneShot(closeSound, transform.position);
            PlayerController.Instance.SetCanMove(true);
            PlayerWeapons.Instance.DisableWeaponFunctions(false);
            EscapeMenuUI.Instance.EnableToggle();
            PlayerInteraction.Instance.SetDisabled(false);
        }
    }

    public void AddItem(InventoryItem newItem)
    {
        List<InventoryItem> itemList = GetSelectedList(newItem.data.itemType);

        if (newItem.data.isStackable)
        {
            foreach (InventoryItem item in itemList)
            {
                if (item.data.itemID == newItem.data.itemID)
                {
                    item.quantity += newItem.quantity;
                    UpdateUI();
                    return;
                }
            }
        }

        itemList.Insert(0, newItem);
        UpdateUI();
    }

    public void RemoveItem(InventoryItem item)
    {
        List<InventoryItem> itemList = GetSelectedList(item.data.itemType);
        itemList.Remove(item);
        UpdateUI();
    }

    private void NextItem()
    {
        List<InventoryItem> currentList = GetCurrentList();
        if (currentList.Count == 0) return;
        selectedItemIndex = (selectedItemIndex + 1) % currentList.Count;
        Transform playerTransform = PlayerController.Instance.transform;
        if (currentList.Count > 1)
            AudioManager.Instance.PlayOneShot(scrollSound, PlayerController.Instance.transform.position);
        UpdateSelection();
    }

    private void PreviousItem()
    {
        List<InventoryItem> currentList = GetCurrentList();
        if (currentList.Count == 0) return;
        selectedItemIndex = (selectedItemIndex - 1 + currentList.Count) % currentList.Count;
        Transform playerTransform = PlayerController.Instance.transform;
        if (currentList.Count > 1)
            AudioManager.Instance.PlayOneShot(scrollSound, PlayerController.Instance.transform.position);
        UpdateSelection();
    }

    private void NextTab()
    {
        selectedTab = (selectedTab + 1) % 4;
        selectedItemIndex = 0;
        UpdateUI();
        AudioManager.Instance.PlayOneShot(buttonSound, transform.position);
    }

    private void PreviousTab()
    {
        selectedTab = (selectedTab + (4 - (1 % 4))) % 4;
        selectedItemIndex = 0;
        UpdateUI();
        AudioManager.Instance.PlayOneShot(buttonSound, transform.position);
    }

    private void UpdateSelection()
    {
        UpdateItemInfo();
    }

    private List<InventoryItem> GetCurrentList()
    {
        return selectedTab switch
        {
            0 => inventoryItems,
            1 => keyItems,
            _ => fileItems,
        };
    }

    private List<InventoryItem> GetSelectedList(ItemType type)
    {
        return type switch
        {
            ItemType.Inventory => inventoryItems,
            ItemType.KeyItem => keyItems,
            _ => fileItems,
        };
    }

    /// <summary>
    /// Refreshes the inventory slots and updates the visual data.
    /// </summary>
    private void UpdateUI()
    {
        // Destroy all old slots.
        foreach (GameObject slot in currentSlots)
        {
            Destroy(slot);
        }
        currentSlots.Clear();

        List<InventoryItem> currentList = GetCurrentList();

        // Adjust the container width based on the number of items.
        float containerWidth = currentList.Count * (defaultSlotWidth + slotSpacing) - slotSpacing;
        carouselContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, containerWidth);

        // Create new slots.
        for (int i = 0; i < currentList.Count; i++)
        {
            InventoryItem item = currentList[i];
            GameObject slot = Instantiate(itemSlotPrefab, carouselContent);
            RectTransform slotRect = slot.GetComponent<RectTransform>();

            slotRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, defaultSlotWidth);
            slotRect.anchoredPosition = Vector2.zero;

            Image iconImage = slot.GetComponent<Image>();
            if (iconImage != null)
                iconImage.sprite = item.data.itemIcon;

            currentSlots.Add(slot);
        }

        // Clamp the selected index to be within bounds.
        if (currentList.Count > 0)
        {
            selectedItemIndex = Mathf.Clamp(selectedItemIndex, 0, currentList.Count - 1);
            currentSlots[selectedItemIndex].transform.localScale = Vector3.one * selectedScale;
            currentSlots[selectedItemIndex].GetComponent<Image>().color = new Color(1, 1, 1, selectedAlpha);
        }

        UpdateItemInfo();
        UpdateTabColors();
        UpdateSlotImages();
        UpdateCurrentObjective();
    }

    private void UpdateItemInfo()
    {
        List<InventoryItem> currentList = GetCurrentList();

        if (currentList.Count > 0 && selectedItemIndex < currentList.Count)
        {
            InventoryItem currentItem = currentList[selectedItemIndex];
            // Append the quantity if the item is stackable.
            string displayName = currentItem.data.itemName;
            if (currentItem.data.isStackable && currentItem.quantity > 1)
            {
                displayName += $" ({currentItem.quantity})";
            }
            itemNameText.text = displayName;

            if (currentItem.data.isUseable)
            {
                useItemHint.SetActive(true);
            }
            else
            {
                useItemHint.SetActive(false);
            }

            itemDescriptionText.text = currentItem.data.itemDescription;
        }
        else
        {
            useItemHint.SetActive(false);
            itemNameText.text = "No Items";
            itemDescriptionText.text = "";
        }
    }


    private void UpdateTabColors()
    {
        inventoryTabText.color = selectedTab == 0 ? Color.white : Color.gray;
        keyTabText.color = selectedTab == 1 ? Color.white : Color.gray;
        fileTabText.color = selectedTab == 2 ? Color.white : Color.gray;
        objectiveTabText.color = selectedTab == 3 ? Color.white : Color.gray;
        if (selectedTab == 3)
        {
            objectivesObject.SetActive(true);
            carouselObject.SetActive(false);
        }
        else
        {
            objectivesObject.SetActive(false);
            carouselObject.SetActive(true);
        }
    }

    public void DeductItemQuantity(int itemID, int amount = 1)
    {
        List<InventoryItem> allItems = inventoryItems.Concat(keyItems).Concat(fileItems).ToList();

        foreach (InventoryItem item in allItems)
        {
            if (item.data.itemID == itemID)
            {
                item.quantity -= amount;

                if (item.quantity <= 0)
                {
                    RemoveItem(item);
                }
                else
                {
                    UpdateUI();
                }

                return;
            }
        }

        Debug.LogWarning($"Item with ID '{itemID}' not found in inventory.");
    }


    private void UseItem()
    {
        List<InventoryItem> currentList = GetCurrentList();
        if (currentList.Count == 0 || selectedItemIndex >= currentList.Count)
            return;

        InventoryItem currentItem = currentList[selectedItemIndex];

        if (currentItem.data.isUseable)
        {
            if (currentItem.data.itemType == ItemType.File)
            {
                ToggleInventory();
                AudioManager.Instance.PlayOneShot(currentItem.data.useSound, PlayerController.Instance.transform.position);
                NoteContentUI.Instance.ShowContentFromList(currentItem.data.text);
                return;
            }
            switch (currentItem.data.itemID)
            {
                case 0: // Revolver
                    PlayerWeapons.Instance.EquipRevolver();
                    ToggleInventory();
                    break;
                case 1: // Shotgun
                    PlayerWeapons.Instance.EquipShotgun();
                    ToggleInventory();
                    break;
                case 2: // Rifle
                    PlayerWeapons.Instance.EquipRifle();
                    ToggleInventory();
                    break;
                case 3: // Bandage
                    if (PlayerHealth.Instance.GetCurrentHealth() >= 100) return;
                    PlayerHealth.Instance.Heal(50);
                    DeductItemQuantity(currentItem.data.itemID);
                    break;
                case 4: // Syringe
                    if (PlayerHealth.Instance.GetCurrentHealth() >= 100) return;
                    PlayerHealth.Instance.Heal(100);
                    DeductItemQuantity(currentItem.data.itemID);
                    break;
                case 15: // Revolver Damage Upgrade
                    PlayerWeapons.Instance.RevolverDamageBuff();
                    DeductItemQuantity(currentItem.data.itemID);
                    SteamAchievementsManager.Instance.CompleteAchievement(16);
                    break;
                case 16: // Revolver Fire Rate Upgrade
                    PlayerWeapons.Instance.RevolverFireRateBuff();
                    DeductItemQuantity(currentItem.data.itemID);
                    SteamAchievementsManager.Instance.CompleteAchievement(17);
                    break;
                case 17: // Shotgun Critical Hit Upgrade
                    PlayerWeapons.Instance.ShotgunCritChanceBuff();
                    DeductItemQuantity(currentItem.data.itemID);
                    SteamAchievementsManager.Instance.CompleteAchievement(19);
                    break;
                case 19: // Shotgun Fire Rate Upgrade
                    PlayerWeapons.Instance.ShotgunFireRateBuff();
                    DeductItemQuantity(currentItem.data.itemID);
                    SteamAchievementsManager.Instance.CompleteAchievement(18);
                    break;
                case 18: // Rifle Critical Hit Upgrade
                    PlayerWeapons.Instance.RifleCritChanceBuff();
                    DeductItemQuantity(currentItem.data.itemID);
                    SteamAchievementsManager.Instance.CompleteAchievement(21);
                    break;
                case 20: // Rifle Fire Rate Upgrade
                    PlayerWeapons.Instance.RifleFireRateBuff();
                    DeductItemQuantity(currentItem.data.itemID);
                    SteamAchievementsManager.Instance.CompleteAchievement(20);
                    break;
                default:
                    break;
            }
            AudioManager.Instance.PlayOneShot(popSound, transform.position);
        }
    }


    /// <summary>
    /// Checks if the inventory contains an item with the specified ID.
    /// </summary>
    public bool HasItem(int itemID)
    {
        List<InventoryItem> allItems = inventoryItems.Concat(keyItems).Concat(fileItems).ToList();

        foreach (InventoryItem item in allItems)
        {
            if (item.data.itemID == itemID)
            {
                return true;
            }
        }

        return false;
    }

    public InventoryItem GetItemByID(int itemID)
    {
        List<InventoryItem> allItems = inventoryItems.Concat(keyItems).Concat(fileItems).ToList();

        foreach (InventoryItem item in allItems)
        {
            if (item.data.itemID == itemID)
            {
                return item;
            }
        }

        return null;
    }

    private void UpdateCurrentObjective()
    {
        if (CheckpointManager.Instance != null && CheckpointManager.Instance.currentObjIndex != -1)
        {
            List<Objective> objectives = CheckpointManager.Instance.GetObjectivesByCurrentObjId();

            foreach (Transform child in objectivesList.transform)
            {
                if (child != currentObjectiveTextTemplate.transform)
                {
                    Destroy(child.gameObject);
                }
            }
            int index = 1;
            foreach (Objective objective in objectives)
            {
                GameObject objectiveText = Instantiate(currentObjectiveTextTemplate, objectivesList.transform);
                TextMeshProUGUI objTmp = objectiveText.GetComponent<TextMeshProUGUI>();
                objTmp.text = index.ToString() + " - " + objective.data.objective;
                if (objective.completed)
                {
                    objTmp.text = "<s>" + objTmp.text + "</s>";
                }
                objectiveText.SetActive(true);
                index++;
            }
        }
    }

    /// <summary>
    /// Checks for items with IDs 0, 1, and 2 and enables/disables
    /// the corresponding slot images (slot1Image, slot2Image, slot3Image).
    /// </summary>
    private void UpdateSlotImages()
    {
        slot1Image.SetActive(HasItem(0));
        slot2Image.SetActive(HasItem(1));
        slot3Image.SetActive(HasItem(2));
    }

    public bool IsOpen() => isInventoryOpen;

    public string GetUniqueIdentifier()
    {
        return GetComponent<SaveableEntity>().UniqueId;
    }

    public object CaptureState()
    {
        return new SaveData(inventoryItems, keyItems, fileItems);
    }

    public void RestoreState(object state)
    {
        string json = state as string;
        SaveData data = JsonUtility.FromJson<SaveData>(json);
        data.inventoryItemsSave.ForEach(item =>
        {
            int itemID = int.Parse(item.Split(',')[0]);
            int itemQuantity = int.Parse(item.Split(',')[1]);
            InventoryItemSO existingItemSO = FindItemByID(everyAvailableItemIngame, itemID);
            if (existingItemSO != null)
            {
                InventoryItem existingItem = new InventoryItem
                {
                    data = existingItemSO,
                    quantity = itemQuantity
                };
                inventoryItems.Add(existingItem);
            }

        });
        data.keyItemsSave.ForEach(item =>
        {
            int itemID = int.Parse(item.Split(',')[0]);
            int itemQuantity = int.Parse(item.Split(',')[1]);
            InventoryItemSO existingItemSO = FindItemByID(everyAvailableItemIngame, itemID);
            if (existingItemSO != null)
            {
                InventoryItem existingItem = new InventoryItem
                {
                    data = existingItemSO,
                    quantity = itemQuantity
                };
                keyItems.Add(existingItem);
            }

        });
        data.fileItemsSave.ForEach(item =>
        {
            int itemID = int.Parse(item.Split(',')[0]);
            int itemQuantity = int.Parse(item.Split(',')[1]);
            InventoryItemSO existingItemSO = FindItemByID(everyAvailableItemIngame, itemID);
            if (existingItemSO != null)
            {
                InventoryItem existingItem = new InventoryItem
                {
                    data = existingItemSO,
                    quantity = itemQuantity
                };
                fileItems.Add(existingItem);
            }

        });
    }

    public void DisableToggle()
    {
        isDisabled = true;
    }
    public void EnableToggle()
    {
        isDisabled = false;
    }

    private InventoryItemSO FindItemByID(List<InventoryItemSO> items, int itemID)
    {
        foreach (InventoryItemSO item in items)
        {
            if (item.itemID == itemID)
            {
                return item;
            }
        }
        return null;
    }

    [Serializable]
    class SaveData
    {
        public class InventoryItemSaveData
        {
            public int itemID;
            public int quantity;
        }
        public List<string> inventoryItemsSave = new List<string>();
        public List<string> keyItemsSave = new List<string>();
        public List<string> fileItemsSave = new List<string>();

        public SaveData(List<InventoryItem> inventoryItems, List<InventoryItem> keyItems, List<InventoryItem> fileItems)
        {
            inventoryItems.ForEach(item => this.inventoryItemsSave.Add($"{item.data.itemID},{item.quantity}"));
            keyItems.ForEach(item => this.keyItemsSave.Add($"{item.data.itemID},{item.quantity}"));
            fileItems.ForEach(item => this.fileItemsSave.Add($"{item.data.itemID},{item.quantity}"));
        }

    }
}
