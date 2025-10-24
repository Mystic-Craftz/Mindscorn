using System.Collections.Generic;
using FMODUnity;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;

public class MazeSafe : MonoBehaviour, IAmInteractable, ISaveable
{
    private const string SAFE_OPENING = "SafeOpening";
    private const string SAFE_OPENED = "SafeOpened";

    [SerializeField] private RoundDial[] dials;
    [SerializeField] private GameObject lockObject;
    [SerializeField] private GameObject hints;
    [SerializeField] private int key;
    [SerializeField] private CinemachineCamera cam;
    [SerializeField] private EventReference openSound;
    [SerializeField] private EventReference changeDialSound;
    [SerializeField] private UnityEvent onOpen;

    private int currentValue = 000;
    private bool isOpen = false;
    public bool isInteracting = false;
    InputManager inputManager;
    private int selectedDialIndex = -1;
    private Animator anim;
    private bool wasTorchOn = false;
    private int[] inventoryItemIds = { 26, 27, 28 };
    InventoryManager inventory;

    private void Start()
    {
        inputManager = InputManager.Instance;
        inventory = InventoryManager.Instance;
        anim = GetComponent<Animator>();
        hints.SetActive(false);
        HideUnavailableDials();
    }

    private void Update()
    {
        if (!isInteracting || isOpen) return;

        PlayerController.Instance.SetCanMove(false);
        PlayerWeapons.Instance.DisableWeaponFunctions(true, true);
        InventoryManager.Instance.DisableToggle();
        EscapeMenuUI.Instance.DisableToggle();
        Crosshair.Instance.SetVisibility(false);
        SetSelectedDial();

        if (inputManager.GetNavigateUpTriggered())
        {
            if (!dials[selectedDialIndex - 1 < 0 ? 0 : selectedDialIndex - 1].gameObject.activeSelf) return;
            AudioManager.Instance.PlayOneShot(changeDialSound, lockObject.transform.position);
            selectedDialIndex--;
            if (selectedDialIndex < 0) selectedDialIndex = 0;
        }
        if (inputManager.GetNavigateDownTriggered())
        {
            if (!dials[selectedDialIndex + 1 > 2 ? 2 : selectedDialIndex + 1].gameObject.activeSelf) return;
            AudioManager.Instance.PlayOneShot(changeDialSound, lockObject.transform.position);
            selectedDialIndex++;
            if (selectedDialIndex > 2) selectedDialIndex = 2;
        }
        if (inputManager.GetUIBackTriggered())
        {
            EndInteraction();
        }

        MakeCurrentValue();

        if (currentValue == key)
        {
            lockObject.SetActive(false);
            anim.Play(SAFE_OPENING);
            isOpen = true;
            AudioManager.Instance.PlayOneShot(openSound, transform.position);
            gameObject.layer = LayerMask.NameToLayer("Default");
            onOpen?.Invoke();
            EndInteraction();
        }
    }

    private void MakeCurrentValue()
    {
        if (!inventory.HasItem(inventoryItemIds[0]) || !inventory.HasItem(inventoryItemIds[1]) || !inventory.HasItem(inventoryItemIds[2])) return;
        string val = $"{dials[0].GetValue()}{dials[1].GetValue()}{dials[2].GetValue()}";

        currentValue = int.Parse(val);
    }

    private void SetSelectedDial()
    {
        if (selectedDialIndex == -1) return;

        for (int index = 0; index < dials.Length; index++)
        {
            if (index == selectedDialIndex)
            {
                dials[index].SetSelectedDial(true);
            }
            else
            {
                dials[index].SetSelectedDial(false);
            }
        }
    }

    public void Interact()
    {
        if (isOpen) return;
        isInteracting = true;
        cam.Priority = 100;
        HideUnavailableDials();
        SelectInitialDial();
        hints.SetActive(true);
        InteractionUI.Instance.Hide(true);
        PlayerWeapons playerWeapons = PlayerWeapons.Instance;
        wasTorchOn = playerWeapons.IsTorchOn();
        if (wasTorchOn) playerWeapons.ToggleTorch();
        if (!inventory.HasItem(inventoryItemIds[0]) || !inventory.HasItem(inventoryItemIds[1]) || !inventory.HasItem(inventoryItemIds[2]))
        {
            DialogUI.Instance.ShowDialog("It is missing some parts...", 2f);
        }
    }

    public void SelectInitialDial()
    {

        if (inventory.HasItem(inventoryItemIds[0]))
        {
            selectedDialIndex = 0;
        }
        else if (inventory.HasItem(inventoryItemIds[1]))
        {
            selectedDialIndex = 1;
        }
        else if (inventory.HasItem(inventoryItemIds[2]))
        {
            selectedDialIndex = 2;
        }
        else
        {
            selectedDialIndex = -1;
        }
    }

    public void HideUnavailableDials()
    {
        inventory = InventoryManager.Instance;
        for (int index = 0; index < dials.Length; index++)
        {
            if (inventory.HasItem(inventoryItemIds[index]))
            {
                dials[index].gameObject.SetActive(true);
            }
            else
            {
                dials[index].gameObject.SetActive(false);
            }
        }
    }

    private void EndInteraction()
    {
        isInteracting = false;
        cam.Priority = 0;
        PlayerController.Instance.SetCanMove(true);
        PlayerWeapons.Instance.DisableWeaponFunctions(false);
        InventoryManager.Instance.EnableToggle();
        EscapeMenuUI.Instance.EnableToggle();
        Crosshair.Instance.SetVisibility(true);
        InteractionUI.Instance.Hide(false);
        PlayerWeapons playerWeapons = PlayerWeapons.Instance;
        if (wasTorchOn && !playerWeapons.IsTorchOn()) playerWeapons.ToggleTorch();
        hints.SetActive(false);
        for (int index = 0; index < dials.Length; index++)
        {
            dials[index].EndInteraction();
        }
    }

    public bool ShouldShowInteractionUI()
    {
        return !isOpen;
    }

    public string GetUniqueIdentifier()
    {
        return GetComponent<SaveableEntity>().UniqueId;
    }

    public object CaptureState()
    {
        return new SaveData
        {
            isOpen = isOpen
        };
    }

    public void RestoreState(object state)
    {
        string json = state as string;
        SaveData data = JsonUtility.FromJson<SaveData>(json);
        isOpen = data.isOpen;

        if (isOpen)
        {
            lockObject.SetActive(false);
            anim = GetComponent<Animator>();
            anim.Play(SAFE_OPENED);
            gameObject.layer = LayerMask.NameToLayer("Props");
        }
    }

    public class SaveData
    {
        public bool isOpen;
    }
}
