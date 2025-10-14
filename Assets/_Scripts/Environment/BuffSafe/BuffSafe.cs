using System.Collections.Generic;
using FMODUnity;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;

public class BuffSafe : MonoBehaviour, IAmInteractable, ISaveable
{
    private const string SAFE_OPENING = "SafeOpening";
    private const string SAFE_OPENED = "SafeOpened";

    [SerializeField] private Dial[] dials;
    [SerializeField] private GameObject lockObject;
    [SerializeField] private GameObject hints;
    [SerializeField] private int key;
    [SerializeField] private CinemachineCamera cam;
    [SerializeField] private LayerMask unInteractiveLayer;
    [SerializeField] private EventReference openSound;
    [SerializeField] private EventReference changeDialSound;
    [SerializeField] private UnityEvent onOpen;

    private int currentValue = 000;
    private bool isOpen = false;
    public bool isInteracting = false;
    InputManager inputManager;
    private Camera mainCam;
    private UniversalAdditionalCameraData cameraData;
    private List<Camera> originalCameraStack = new List<Camera>();
    private int selectedDialIndex = 0;
    private Animator anim;
    private bool wasTorchOn = false;

    [SerializeField] private GameObject eyesObject;

    private void Start()
    {
        mainCam = Camera.main;
        cameraData = mainCam.GetUniversalAdditionalCameraData();
        cameraData.cameraStack.ForEach(overlayCam => originalCameraStack.Add(overlayCam));
        inputManager = InputManager.Instance;
        anim = GetComponent<Animator>();
        hints.SetActive(false);
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

        if (inputManager.GetNavigateLeftTriggered())
        {
            AudioManager.Instance.PlayOneShot(changeDialSound, lockObject.transform.position);
            selectedDialIndex--;
            if (selectedDialIndex < 0) selectedDialIndex = 0;
        }
        if (inputManager.GetNavigateRightTriggered())
        {
            AudioManager.Instance.PlayOneShot(changeDialSound, lockObject.transform.position);
            selectedDialIndex++;
            if (selectedDialIndex > 2) selectedDialIndex = 2;
        }
        if (inputManager.GetCloseTriggered())
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
            gameObject.layer = LayerMask.NameToLayer("Props");
            onOpen?.Invoke();
            EndInteraction();
        }
    }

    private void MakeCurrentValue()
    {
        string val = $"{dials[0].GetValue()}{dials[1].GetValue()}{dials[2].GetValue()}";

        currentValue = int.Parse(val);
    }

    private void SetSelectedDial()
    {
        for (int index = 0; index < dials.Length; index++)
        {
            if (index == selectedDialIndex)
                dials[index].SetSelectedDial(true);
            else
                dials[index].SetSelectedDial(false);
        }
    }

    public void Interact()
    {
        if (isOpen) return;
        isInteracting = true;
        cam.Priority = 100;
        // cameraData.cameraStack.Clear();
        selectedDialIndex = 0;
        hints.SetActive(true);
        InteractionUI.Instance.Hide(true);
        PlayerWeapons playerWeapons = PlayerWeapons.Instance;
        wasTorchOn = playerWeapons.IsTorchOn();
        if (wasTorchOn) playerWeapons.ToggleTorch();
        NeonDimensionController.Instance.ReturnToNormalInstant();
        if (eyesObject != null) eyesObject.SetActive(false);
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
        // originalCameraStack.ForEach(overlayCam =>
        // {
        //     cameraData.cameraStack.Add(overlayCam);
        // });
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
