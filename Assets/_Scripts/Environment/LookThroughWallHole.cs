using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(SaveableEntity))]
public class LookThroughWallHole : MonoBehaviour, IAmInteractable, ISaveable
{
    [SerializeField] private UnityEvent onPeepingStart;
    [SerializeField] private UnityEvent onPeepingEnd;
    [SerializeField] private CinemachineCamera cam;
    [SerializeField] private float autoEndAfterSeconds = 3f;
    [SerializeField] private EventReference activateSound;

    private bool isPeeping = false;

    private Camera mainCam;
    private UniversalAdditionalCameraData cameraData;
    private List<Camera> originalCameraStack = new List<Camera>();
    private Coroutine endingCoroutine;
    private bool hasInteracted = false;
    InputManager inputManager;

    private void Start()
    {
        mainCam = Camera.main;
        cameraData = mainCam.GetUniversalAdditionalCameraData();
        cameraData.cameraStack.ForEach(overlayCam => originalCameraStack.Add(overlayCam));
        inputManager = InputManager.Instance;
    }

    private void Update()
    {
        if (isPeeping)
        {
            PlayerController.Instance.SetCanMove(false);
            PlayerWeapons.Instance.DisableWeaponFunctions(true, true);
            InventoryManager.Instance.DisableToggle();
            EscapeMenuUI.Instance.DisableToggle();
            Crosshair.Instance.SetVisibility(false);

            if (inputManager.GetUseItem() || inputManager.GetCloseTriggered())
            {
                StopPeeping();
            }
        }
    }

    public void Interact()
    {
        if (hasInteracted || isPeeping) return;
        isPeeping = true;
        cam.Priority = 100;
        gameObject.SetActive(false);
        gameObject.SetActive(true);
        InteractionUI.Instance.Hide();
        onPeepingStart?.Invoke();
        // cameraData.cameraStack.Clear();
        AudioManager.Instance.PlayOneShot(activateSound, transform.position);
        endingCoroutine = StartCoroutine(EndAfterDuration());
    }

    private IEnumerator EndAfterDuration()
    {
        yield return new WaitForSeconds(autoEndAfterSeconds);
        StopPeeping();
    }

    private void StopPeeping()
    {
        isPeeping = false;
        onPeepingEnd?.Invoke();
        StopCoroutine(endingCoroutine);
        cam.Priority = 0;
        hasInteracted = true;
        PlayerController.Instance.SetCanMove(true);
        PlayerWeapons.Instance.DisableWeaponFunctions(false);
        InventoryManager.Instance.EnableToggle();
        EscapeMenuUI.Instance.EnableToggle();
        Crosshair.Instance.SetVisibility(true);
        DialogUI.Instance.ShowDialog("What the hell was that?");
        // originalCameraStack.ForEach(overlayCam =>
        // {
        //     cameraData.cameraStack.Add(overlayCam);
        // });
    }

    public bool ShouldShowInteractionUI()
    {
        return !isPeeping && !hasInteracted;
    }

    public string GetUniqueIdentifier()
    {
        return GetComponent<SaveableEntity>().UniqueId;
    }

    public object CaptureState()
    {
        return new SaveData { hasInteracted = hasInteracted };
    }

    public void RestoreState(object state)
    {
        string json = state as string;
        SaveData data = JsonUtility.FromJson<SaveData>(json);
        hasInteracted = data.hasInteracted;
    }

    public class SaveData
    {
        public bool hasInteracted;
    }
}
