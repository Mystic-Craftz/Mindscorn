using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using FMOD.Studio;
using FMODUnity;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ContainerSearchingUI : MonoBehaviour
{
    public static ContainerSearchingUI Instance { get; private set; }
    [SerializeField] private GameObject searchLoaderUI;
    [SerializeField] private GameObject searchedLootUI;
    [SerializeField] private Image searchBar;
    [SerializeField] private GameObject searchLootItemTemplate;
    [SerializeField] private Transform searchLootItemGrid;
    [SerializeField] private float fadeSpeed = 5f;
    [SerializeField] private GameObject didNotFindText;
    [SerializeField] private EventReference openSound;
    [SerializeField] private EventReference searchingSound;
    [SerializeField] private EventReference confirmSound;
    [SerializeField] private EventReference cancelSound;
    [SerializeField] private EventReference popSound;

    [Header("Condition based loot")]
    [SerializeField] private InventoryItemSO revolverBulletsSO;
    [SerializeField] private InventoryItemSO revolverSO;
    [SerializeField] private InventoryItemSO shotgunBulletsSO;
    [SerializeField] private InventoryItemSO shotgunSO;
    [SerializeField] private InventoryItemSO rifleBulletsSO;
    [SerializeField] private InventoryItemSO rifleSO;
    [SerializeField] private InventoryItemSO bandageSO;
    [SerializeField] private InventoryItemSO syringeSO;

    private enum SearchState { Idle, Searching, Searched }

    private CanvasGroup searchLoaderCanvasGroup;
    private CanvasGroup searchedLootCanvasGroup;
    private VerticalLayoutGroup searchedLootLayoutGroup;
    private CanvasGroup gameObjectCanvasGroup;
    private SearchState state = SearchState.Searching;

    private bool isOpen = false;

    private UnityAction onCancel;

    private List<InventoryItem> lootToRender = new List<InventoryItem>();

    private EventInstance searchingSoundInstance;

    private bool shouldGenerateRandomLoot = true;

    [System.Serializable]
    public class RandomLootGenerationData
    {
        [Tooltip("If quantity is less than this value, the revolver bullets will be given")]
        public int revolverBulletsQuantityMin;
        [Tooltip("Min(Inclusive) revolver bullets given when bullets are less than the above quantity")]
        public int revolverGainMin;
        [Tooltip("Max(Exclusive) revolver bullets given when bullets are less than the above quantity")]
        public int revolverGainMax;
        [Tooltip("Min(Inclusive) revolver bullets given when player has no revolver bullets")]
        public int noBulletsRevolverGainMin;
        [Tooltip("Max(Exclusive) revolver bullets given when player has no revolver bullets")]
        public int noBulletsRevolverGainMax;
        [Tooltip("If quantity is less than this value, the shotgun bullets will be given")]
        public int shotgunBulletsQuantityMin;
        [Tooltip("Min(Inclusive) shotgun bullets given when bullets are less than the above quantity")]
        public int shotgunGainMin;
        [Tooltip("Max(Exclusive) shotgun bullets given when bullets are less than the above quantity")]
        public int shotgunGainMax;
        [Tooltip("Min(Inclusive) shotgun bullets given when player has no shotgun bullets")]
        public int noBulletsShotgunGainMin;
        [Tooltip("Max(Exclusive) shotgun bullets given when player has no shotgun bullets")]
        public int noBulletsShotgunGainMax;
        [Tooltip("If quantity is less than this value, the shotgun bullets will be given")]
        public int rifleBulletsQuantityMin;
        [Tooltip("Min(Inclusive) shotgun bullets given when bullets are less than the above quantity")]
        public int rifleGainMin;
        [Tooltip("Max(Exclusive) shotgun bullets given when bullets are less than the above quantity")]
        public int rifleGainMax;
        [Tooltip("Min(Inclusive) shotgun bullets given when player has no shotgun bullets")]
        public int noBulletsRifleGainMin;
        [Tooltip("Max(Exclusive) shotgun bullets given when player has no shotgun bullets")]
        public int noBulletsRifleGainMax;
    }

    private RandomLootGenerationData randomLootGenerationData = new RandomLootGenerationData();

    private void Awake() => Instance = this;

    private void Start()
    {
        searchLoaderCanvasGroup = searchLoaderUI.GetComponent<CanvasGroup>();
        searchedLootCanvasGroup = searchedLootUI.GetComponent<CanvasGroup>();
        gameObjectCanvasGroup = GetComponent<CanvasGroup>();
        searchLoaderCanvasGroup.alpha = 0;
        searchedLootCanvasGroup.alpha = 0;
        gameObjectCanvasGroup.alpha = 0;
        transform.DOScale(new Vector3(1, 0, 1), 0f);
        searchedLootLayoutGroup = searchedLootUI.GetComponent<VerticalLayoutGroup>();
        searchLootItemTemplate.SetActive(false);
        searchingSoundInstance = AudioManager.Instance.CreateInstance(searchingSound);
    }

    private void Update()
    {
        switch (state)
        {
            case SearchState.Searching:
                searchLoaderCanvasGroup.alpha = Mathf.MoveTowards(searchLoaderCanvasGroup.alpha, 1, fadeSpeed * Time.deltaTime);
                searchedLootCanvasGroup.alpha = Mathf.MoveTowards(searchedLootCanvasGroup.alpha, 0, fadeSpeed * Time.deltaTime);
                searchLoaderUI.SetActive(true);
                if (searchedLootCanvasGroup.alpha <= 0)
                    searchedLootUI.SetActive(false);
                break;
            case SearchState.Searched:
                searchLoaderCanvasGroup.alpha = Mathf.MoveTowards(searchLoaderCanvasGroup.alpha, 0, fadeSpeed * Time.deltaTime);
                searchedLootCanvasGroup.alpha = Mathf.MoveTowards(searchedLootCanvasGroup.alpha, 1, fadeSpeed * Time.deltaTime);
                searchedLootUI.SetActive(true);
                searchedLootLayoutGroup.CalculateLayoutInputHorizontal();
                searchedLootLayoutGroup.CalculateLayoutInputVertical();
                searchedLootLayoutGroup.SetLayoutVertical();
                searchedLootLayoutGroup.SetLayoutHorizontal();
                searchingSoundInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                break;
            case SearchState.Idle:
                break;
        }

        //? Cancel logic
        if (isOpen)
        {
            switch (state)
            {
                case SearchState.Searching:
                    if (InputManager.Instance.GetCloseTriggered())
                    {
                        onCancel?.Invoke();
                        state = SearchState.Idle;
                    }
                    break;
                case SearchState.Searched:
                    if (InputManager.Instance.GetUseItem())
                    {
                        state = SearchState.Idle;
                        gameObjectCanvasGroup.DOFade(0, 0.2f);
                        transform.DOScale(new Vector3(1, 0, 1), 0.2f);
                        isOpen = false;
                        AudioManager.Instance.PlayOneShot(confirmSound, transform.position);
                    }
                    break;
                case SearchState.Idle:
                    break;
            }
        }
    }

    public void StartSearch(UnityAction OnCancel, bool shouldGenerateDynamicLoot = true, RandomLootGenerationData randomLootGenerationData = null)
    {
        state = SearchState.Searching;
        searchLoaderCanvasGroup.alpha = 0;
        searchedLootCanvasGroup.alpha = 0;
        shouldGenerateRandomLoot = shouldGenerateDynamicLoot;
        this.randomLootGenerationData = randomLootGenerationData;
        gameObjectCanvasGroup.DOFade(1, 0.2f);
        transform.DOScale(new Vector3(1, 1, 1), 0.2f);
        gameObject.SetActive(true);
        searchBar.fillAmount = 0;
        isOpen = true;
        didNotFindText.SetActive(false);
        AudioManager.Instance.PlayOneShot(openSound, transform.position);
        searchingSoundInstance.start();
        InventoryManager.Instance.DisableToggle();
        EscapeMenuUI.Instance.DisableToggle();
        onCancel = () =>
        {
            gameObjectCanvasGroup.DOFade(0, 0.2f);
            transform.DOScale(new Vector3(1, 0, 1), 0.2f);
            OnCancel?.Invoke();
            isOpen = false;
            AudioManager.Instance.PlayOneShot(cancelSound, transform.position);
            searchingSoundInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            InventoryManager.Instance.EnableToggle();
            EscapeMenuUI.Instance.EnableToggle();
        };
    }

    public void InterruptSearch()
    {
        onCancel?.Invoke();
    }

    public void UpdateSearchBar(float value)
    {
        searchBar.fillAmount = value;
    }

    public void EndSearch(List<InventoryItem> items)
    {
        lootToRender = items;
        if (shouldGenerateRandomLoot)
            GenerateConditionBasedLoot();
        InventoryManager.Instance.EnableToggle();
        EscapeMenuUI.Instance.EnableToggle();
        items.ForEach(item =>
        {
            InventoryManager.Instance.AddItem(item);
        });
        ReRenderLootItemsInGrid();
        state = SearchState.Searched;
        if (lootToRender.Count == 0)
            didNotFindText.SetActive(true);
    }

    private void GenerateConditionBasedLoot()
    {
        InventoryManager inventoryManager = InventoryManager.Instance;

        float health = PlayerHealth.Instance.GetCurrentHealth();

        bool criticalHealth = health < 15;
        bool lowHealth = health < 30;

        if (lowHealth && !inventoryManager.HasItem(bandageSO.itemID))
        {
            InventoryItemSO itemToAdd = bandageSO;

            if (criticalHealth)
            {
                float chance = Random.Range(0f, 100f);
                if (chance > 50f)
                    itemToAdd = syringeSO;
            }

            lootToRender.Add(new InventoryItem { data = itemToAdd, quantity = 1 });
        }

        if (inventoryManager.HasItem(revolverSO.itemID))
        {
            if (inventoryManager.HasItem(revolverBulletsSO.itemID))
            {
                float revolverBulletsQuantity = inventoryManager.GetItemByID(revolverBulletsSO.itemID).quantity;
                if (revolverBulletsQuantity < randomLootGenerationData.revolverBulletsQuantityMin)
                {
                    int randomQuantity = Random.Range(randomLootGenerationData.revolverGainMin, randomLootGenerationData.revolverGainMax);
                    lootToRender.Add(new InventoryItem { data = revolverBulletsSO, quantity = randomQuantity });

                }
            }
            else
            {
                int randomQuantity = Random.Range(randomLootGenerationData.noBulletsRevolverGainMin, randomLootGenerationData.noBulletsRevolverGainMax);
                lootToRender.Add(new InventoryItem { data = revolverBulletsSO, quantity = randomQuantity });
            }
        }

        if (inventoryManager.HasItem(shotgunSO.itemID))
        {
            if (inventoryManager.HasItem(shotgunBulletsSO.itemID))
            {
                float shotgunShellsQuantity = inventoryManager.GetItemByID(shotgunBulletsSO.itemID).quantity;
                if (shotgunShellsQuantity < randomLootGenerationData.shotgunBulletsQuantityMin)
                {
                    int randomQuantity = Random.Range(randomLootGenerationData.shotgunGainMin, randomLootGenerationData.shotgunGainMax);
                    lootToRender.Add(new InventoryItem { data = shotgunBulletsSO, quantity = randomQuantity });
                }
            }
            else
            {
                int randomQuantity = Random.Range(randomLootGenerationData.noBulletsShotgunGainMin, randomLootGenerationData.noBulletsShotgunGainMax);
                lootToRender.Add(new InventoryItem { data = shotgunBulletsSO, quantity = randomQuantity });
            }
        }

        if (inventoryManager.HasItem(rifleSO.itemID))
        {
            if (inventoryManager.HasItem(rifleBulletsSO.itemID))
            {
                float rifleBulletsQuantity = inventoryManager.GetItemByID(rifleBulletsSO.itemID).quantity;
                if (rifleBulletsQuantity < randomLootGenerationData.rifleBulletsQuantityMin)
                {
                    int randomQuantity = Random.Range(randomLootGenerationData.rifleGainMin, randomLootGenerationData.rifleGainMax);
                    lootToRender.Add(new InventoryItem { data = rifleBulletsSO, quantity = randomQuantity });
                }
            }
            else
            {
                int randomQuantity = Random.Range(randomLootGenerationData.noBulletsRifleGainMin, randomLootGenerationData.noBulletsRifleGainMax);
                lootToRender.Add(new InventoryItem { data = rifleBulletsSO, quantity = randomQuantity });
            }
        }
    }

    private void ReRenderLootItemsInGrid()
    {
        foreach (Transform child in searchLootItemGrid)
        {
            if (child.gameObject != searchLootItemTemplate)
                Destroy(child.gameObject);
        }
        int index = 0;
        foreach (InventoryItem item in lootToRender)
        {
            GameObject newItem = Instantiate(searchLootItemTemplate, searchLootItemGrid);
            newItem.GetComponent<SearchLootItemUI>().SetItem(item, index);
            newItem.SetActive(true);
            index++;
        }
    }

    public bool IsOpen() => isOpen;
}