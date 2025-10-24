using System;
using Unity.Cinemachine;
using UnityEngine;
using FMODUnity;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;
using UnityEngine.Events;
using DG.Tweening;

[RequireComponent(typeof(SaveableEntity))]
public class PlayerWeapons : MonoBehaviour, ISaveable
{
    public static PlayerWeapons Instance { get; private set; }

    [Header("Player")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Transform handsMesh;
    [Header("Knife")]
    [SerializeField] private Transform knifeHolder;
    [SerializeField] private PocketKnife knife;

    [Header("Revolver")]
    [SerializeField] private Transform revolverHolder;
    [SerializeField] private Revolver revolver;

    [Header("Shotgun")]
    [SerializeField] private Transform shotgunHolder;
    [SerializeField] private Shotgun shotgun;

    [Header("Rifle")]
    [SerializeField] private Transform rifleHolder;
    [SerializeField] private Rifle rifle;

    [Header("Cameras")]
    [SerializeField] private Camera handCam;
    [SerializeField] private CinemachineCamera playerCam;

    [Header("Aiming")]
    [SerializeField] private float aimSpeed = 10f;
    [SerializeField] private float aimedFOV = 30f;
    [SerializeField] private float unAimedFOV = 45f;

    [Header("Settings")]
    [SerializeField] private float swithDelayMax = 1f;

    [Header("Misc")]
    [SerializeField] private GameObject torchObject;
    [SerializeField] private float torchIntensity;
    [SerializeField] private float torchIntensityWhenCloseToObject;
    [SerializeField] private float torchIntensityChangeSpeed = 1f;
    [SerializeField] private EventReference torchSound;

    private int revolverItemID = 0;
    private int shotgunItemID = 1;
    private int rifleItemID = 2;

    public enum Weapons
    {
        None,
        Knife,
        Revolver,
        Shotgun,
        Rifle
    }

    private InputManager inputManager;
    private Transform cameraTransform;
    private PlayerAnimations playerAnimations;
    private IAmAWeapon currentWeapon = null;
    private bool isAiming = false;
    private Weapons currentWeaponType = Weapons.None;
    private bool isSwitchingWeapons = false;
    private float switchDelay = 0f;
    private bool canSwitch = true;
    private bool disableWeaponFunctions = false;
    private bool disableWeaponForASection = false;

    private bool isFirstFrame = true;

    private bool isTorchDisabled = false;

    public Action OnSafeToChangeWeapon;

    private bool isReloading = false;

    public EventHandler<EventArgs> OnShoot;

    private Weapons lastWeaponBeforeUnEquipping = Weapons.None;

    private float playerCamFovDifference = 0f;
    private float realUnAimedFOV = 0f;
    private Light torchLight;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        inputManager = InputManager.Instance;
        cameraTransform = Camera.main.transform;
        playerAnimations = PlayerAnimations.Instance;
        playerCamFovDifference = unAimedFOV - aimedFOV;
        float fov = PlayerPrefs.GetFloat("fieldofview", 60f);
        realUnAimedFOV = fov;
        torchLight = torchObject.GetComponent<Light>();
    }

    private void Update()
    {
        Torch();
        if (disableWeaponForASection) return;
        if (disableWeaponFunctions) return;
        WeaponCycle();
        WeaponVisuals();
        Aiming();
        Shoot();
        Reload();
        SwitchDelayTimer();
        // Melee();

        isFirstFrame = false;
    }

    private void WeaponCycle()
    {
        if (inputManager.GetQuickSlot1())
        {
            EquipKnife();
        }
        //? Revolver
        if (inputManager.GetQuickSlot2() && InventoryManager.Instance.HasItem(revolverItemID))
        {
            EquipRevolver();
        }
        //? Shotgun
        else if (inputManager.GetQuickSlot3() && InventoryManager.Instance.HasItem(shotgunItemID))
        {
            EquipShotgun();
        }
        //? Rifle
        else if (inputManager.GetQuickSlot4() && InventoryManager.Instance.HasItem(rifleItemID))
        {
            EquipRifle();
        }
    }

    public void EquipKnife()
    {

        if (disableWeaponForASection) return;

        if (currentWeapon != null)
            if (currentWeapon.IsReloadInProgress() || !canSwitch) return;

        if (currentWeapon != null && currentWeapon.Equals(knife)) return;

        Weapons previousWeapon = currentWeaponType;
        currentWeapon = knife;
        currentWeaponType = Weapons.Knife;
        OnSafeToChangeWeapon = () =>
        {
            knifeHolder.gameObject.SetActive(true);
            revolverHolder.gameObject.SetActive(false);
            shotgunHolder.gameObject.SetActive(false);
            rifleHolder.gameObject.SetActive(false);
            isSwitchingWeapons = false;
        };
        handsMesh.gameObject.SetActive(true);
        isSwitchingWeapons = true;
        canSwitch = false;
        if (isFirstFrame) previousWeapon = Weapons.None;
        playerAnimations = PlayerAnimations.Instance;
        playerAnimations.ChangeWeapon(previousWeapon, Weapons.Knife);
    }

    public void EquipRevolver()
    {
        if (disableWeaponForASection) return;
        if (currentWeapon != null)
            if (currentWeapon.IsReloadInProgress() || !canSwitch) return;
        if (currentWeapon != null && currentWeapon.Equals(revolver)) return;
        Weapons previousWeapon = currentWeaponType;
        currentWeapon = revolver;
        currentWeaponType = Weapons.Revolver;
        OnSafeToChangeWeapon = () =>
        {
            revolverHolder.gameObject.SetActive(true);
            knifeHolder.gameObject.SetActive(false);
            shotgunHolder.gameObject.SetActive(false);
            rifleHolder.gameObject.SetActive(false);
            isSwitchingWeapons = false;
        };
        handsMesh.gameObject.SetActive(true);
        isSwitchingWeapons = true;
        canSwitch = false;
        if (isFirstFrame) previousWeapon = Weapons.None;
        playerAnimations = PlayerAnimations.Instance;
        playerAnimations.ChangeWeapon(previousWeapon, Weapons.Revolver);
    }

    public void EquipShotgun()
    {
        if (disableWeaponForASection) return;
        if (currentWeapon != null)
            if (currentWeapon.IsReloadInProgress() || !canSwitch) return;
        if (currentWeapon != null && currentWeapon.Equals(shotgun)) return;

        Weapons previousWeapon = currentWeaponType;
        currentWeapon = shotgun;
        currentWeaponType = Weapons.Shotgun;
        OnSafeToChangeWeapon = () =>
        {
            revolverHolder.gameObject.SetActive(false);
            shotgunHolder.gameObject.SetActive(true);
            knifeHolder.gameObject.SetActive(false);
            rifleHolder.gameObject.SetActive(false);
            isSwitchingWeapons = false;
        };
        isSwitchingWeapons = true;
        handsMesh.gameObject.SetActive(true);
        canSwitch = false;
        if (isFirstFrame) previousWeapon = Weapons.None;
        playerAnimations = PlayerAnimations.Instance;
        playerAnimations.ChangeWeapon(previousWeapon, Weapons.Shotgun);
    }
    public void EquipRifle()
    {
        if (disableWeaponForASection) return;
        if (currentWeapon != null)
            if (currentWeapon.IsReloadInProgress() || !canSwitch) return;
        if (currentWeapon != null && currentWeapon.Equals(rifle)) return;
        Weapons previousWeapon = currentWeaponType;
        currentWeapon = rifle;
        currentWeaponType = Weapons.Rifle;
        OnSafeToChangeWeapon = () =>
        {
            revolverHolder.gameObject.SetActive(false);
            shotgunHolder.gameObject.SetActive(false);
            knifeHolder.gameObject.SetActive(false);
            rifleHolder.gameObject.SetActive(true);
            isSwitchingWeapons = false;
        };
        isSwitchingWeapons = true;
        handsMesh.gameObject.SetActive(true);
        canSwitch = false;
        if (isFirstFrame)
        {
            previousWeapon = Weapons.None;
        }
        playerAnimations = PlayerAnimations.Instance;
        playerAnimations.ChangeWeapon(previousWeapon, Weapons.Rifle);
    }

    public void UnEquipAnyGun()
    {
        StartCoroutine(UnEquipAnyGunCoRoutine());
    }

    private IEnumerator UnEquipAnyGunCoRoutine()
    {
        if (currentWeapon != null)
            if (currentWeapon.IsReloadInProgress())
            {
                while (currentWeapon.IsReloadInProgress())
                {
                    // Debug.Log("Waiting for reload to finish before unequipping");
                    currentWeapon.FinishReload(this);
                    yield return new WaitForEndOfFrame();
                }
                // yield return new WaitUntil(() => !currentWeapon.IsReloadInProgress());
            }
        Weapons previousWeapon = currentWeaponType;
        lastWeaponBeforeUnEquipping = previousWeapon;
        currentWeapon = null;
        OnSafeToChangeWeapon = () =>
        {
            revolverHolder.gameObject.SetActive(false);
            shotgunHolder.gameObject.SetActive(false);
            rifleHolder.gameObject.SetActive(false);
            knifeHolder.gameObject.SetActive(false);
            handsMesh.gameObject.SetActive(false);
            currentWeaponType = Weapons.None;
            isSwitchingWeapons = false;
        };
        isSwitchingWeapons = true;
        canSwitch = false;
        if (isFirstFrame) previousWeapon = Weapons.None;
        playerAnimations.ChangeWeapon(previousWeapon, Weapons.None);
        yield return null;
    }

    private void WeaponVisuals()
    {
        if (currentWeapon != null)
        {
            if (currentWeapon.IsReloadInProgress()) return;
        }

        switch (currentWeaponType)
        {
            case Weapons.Knife:
                break;
            case Weapons.Revolver:
                break;
            case Weapons.Shotgun:
                break;
            case Weapons.Rifle:
                break;
            case Weapons.None:
            default:
                handsMesh.gameObject.SetActive(false);
                revolverHolder.gameObject.SetActive(false);
                shotgunHolder.gameObject.SetActive(false);
                rifleHolder.gameObject.SetActive(false);
                currentWeapon = null;
                break;
        }
    }

    private void Aiming()
    {
        if (inputManager.GetPlayerAim() && !playerController.isSprinting && currentWeaponType != Weapons.Knife)
        {
            handCam.fieldOfView = Mathf.Lerp(handCam.fieldOfView, aimedFOV, Time.deltaTime * aimSpeed);
            playerCam.Lens.FieldOfView = Mathf.Lerp(playerCam.Lens.FieldOfView, realUnAimedFOV - playerCamFovDifference, Time.deltaTime * aimSpeed);
            isAiming = true;
        }
        else
        {
            handCam.fieldOfView = Mathf.Lerp(handCam.fieldOfView, unAimedFOV, Time.deltaTime * aimSpeed);
            playerCam.Lens.FieldOfView = Mathf.Lerp(playerCam.Lens.FieldOfView, realUnAimedFOV, Time.deltaTime * aimSpeed);
            isAiming = false;
        }

        if (inputManager.GetPlayerAim() && !playerController.isSprinting && currentWeaponType == Weapons.Knife)
        {
            knife.DoHeavyAttack();
        }
    }

    private void SwitchDelayTimer()
    {
        if (canSwitch) return;

        if (switchDelay >= swithDelayMax)
        {
            switchDelay = 0f;
            canSwitch = true;
        }
        else
        {
            switchDelay += Time.deltaTime;
        }
    }

    private void Shoot()
    {
        if (inputManager.GetPlayerShoot() && currentWeaponType != Weapons.None && canSwitch)
        {
            currentWeapon.Fire(this);
            OnShoot?.Invoke(this, EventArgs.Empty);
        }
    }

    private void Reload()
    {
        if (inputManager.GetPlayerReload() && currentWeaponType != Weapons.None && canSwitch)
        {
            currentWeapon.Reload(this);
        }
    }

    // private void Melee()
    // {
    //     if (inputManager.GetPlayerMelee() && currentWeaponType != Weapons.None)
    //     {
    //         currentWeapon.Melee(this);
    //     }
    // }

    private void Torch()
    {
        if (inputManager.GetPlayerTorch())
        {
            ToggleTorch();
        }

        if (!torchObject.activeSelf) return;

        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, 2f))
        {
            torchLight.intensity = Mathf.MoveTowards(torchLight.intensity, torchIntensityWhenCloseToObject, Time.deltaTime * torchIntensityChangeSpeed);
        }
        else
        {
            torchLight.intensity = Mathf.MoveTowards(torchLight.intensity, torchIntensity, Time.deltaTime * torchIntensityChangeSpeed);
        }

    }

    public bool IsSwitchingWeaponsInProgress() => isSwitchingWeapons;

    public bool IsAiming() => isAiming;

    public bool IsHoldingWeapon() => currentWeapon != null;

    public void RevolverDamageBuff()
    {
        revolver.SetDamageMultiplier(1.5f);
    }

    public void RevolverFireRateBuff()
    {
        revolver.SetFireRateBuff(0.7f);
    }

    public void ShotgunCritChanceBuff()
    {
        shotgun.SetCritChance(0.4f);
    }

    public void ShotgunFireRateBuff()
    {
        shotgun.SetFireRateBuff(0.9f);
    }

    public void RifleCritChanceBuff()
    {
        rifle.SetCritChance(.3f);
    }

    public void RifleFireRateBuff()
    {
        rifle.SetFireRateBuff(0.9f);
    }

    public void ToggleTorch()
    {
        if (isTorchDisabled)
        {
            torchObject.SetActive(false);
            return;
        }

        AudioManager.Instance.PlayOneShot(torchSound, transform.position);
        if (torchObject.activeSelf)
        {
            //? Turn torch off here
            StartCoroutine(TorchOffCoRoutine());
        }
        else
        {
            //? Turn torch on here
            StartCoroutine(TorchOnCoRoutine());
        }
    }

    private IEnumerator TorchOnCoRoutine()
    {
        torchLight = torchObject.GetComponent<Light>();
        torchLight.intensity = 0f;
        torchObject.SetActive(true);

        float originalIntensity = torchIntensity;

        DOTween.To(() => torchLight.intensity, x => torchLight.intensity = x, originalIntensity + 10f, 0.2f).SetEase(Ease.InOutSine);
        yield return new WaitForSeconds(0.2f);
        DOTween.To(() => torchLight.intensity, x => torchLight.intensity = x, originalIntensity - 15f, 0.1f).SetEase(Ease.InOutSine);
        yield return new WaitForSeconds(0.1f);
        DOTween.To(() => torchLight.intensity, x => torchLight.intensity = x, originalIntensity, 0.1f).SetEase(Ease.InOutSine);
    }


    private IEnumerator TorchOffCoRoutine()
    {
        torchLight = torchObject.GetComponent<Light>();

        DOTween.To(() => torchLight.intensity, x => torchLight.intensity = x, 0, 0.1f).SetEase(Ease.InOutSine);
        yield return new WaitForSeconds(0.1f);
        torchObject.SetActive(false);
    }

    public bool IsTorchOn() => torchObject.activeSelf;

    public void SetDisableTorch(bool value)
    {
        isTorchDisabled = value;
        if (isTorchDisabled)
        {
            torchObject.SetActive(false);
        }
    }

    public void DisableWeaponFunctions(bool value, bool unEquipWeapon = false)
    {
        disableWeaponFunctions = value;
        if (unEquipWeapon) UnEquipAnyGun();
    }

    public void DisableWeaponForASection(bool value, bool unEquipWeapon = false)
    {
        disableWeaponForASection = value;
        if (unEquipWeapon) UnEquipAnyGun();
    }

    public void TorchFlicker(UnityAction onDarkness = null, UnityAction onComplete = null)
    {
        StartCoroutine(TorchFlickerCoRoutine(onDarkness, onComplete));
    }

    private IEnumerator TorchFlickerCoRoutine(UnityAction onDarkness = null, UnityAction onComplete = null)
    {
        torchLight = torchObject.GetComponent<Light>();

        float originalIntensity = torchLight.intensity;

        DOTween.To(() => torchLight.intensity, x => torchLight.intensity = x, 0f, 0.2f).SetEase(Ease.InOutSine);
        yield return new WaitForSeconds(0.2f);
        onDarkness?.Invoke();
        yield return new WaitForSeconds(0.1f);
        DOTween.To(() => torchLight.intensity, x => torchLight.intensity = x, originalIntensity + .5f, 0.2f).SetEase(Ease.InOutSine);
        yield return new WaitForSeconds(0.2f);
        DOTween.To(() => torchLight.intensity, x => torchLight.intensity = x, 0f, 0.1f).SetEase(Ease.InOutSine);
        yield return new WaitForSeconds(0.1f);
        DOTween.To(() => torchLight.intensity, x => torchLight.intensity = x, originalIntensity, 0.2f).SetEase(Ease.InOutSine);
        onComplete?.Invoke();
    }

    public void HideHands() => handsMesh.gameObject.SetActive(false);

    public void SetFOV(float fov)
    {
        realUnAimedFOV = fov;
    }

    public string GetUniqueIdentifier()
    {
        return GetComponent<SaveableEntity>().UniqueId;
    }

    public object CaptureState()
    {
        return new SaveData
        {
            currentWeapon = currentWeaponType.ToString(),
            isTorchActive = torchObject.activeSelf
        };
    }

    public void RestoreState(object state)
    {
        handsMesh.gameObject.SetActive(false);
        string json = state as string;
        SaveData data = JsonUtility.FromJson<SaveData>(json);
        if (data.currentWeapon != "")
            currentWeaponType = (Weapons)Enum.Parse(typeof(Weapons), data.currentWeapon);

        switch (currentWeaponType)
        {
            case Weapons.Knife:
                EquipKnife();
                break;
            case Weapons.Revolver:
                EquipRevolver();
                break;
            case Weapons.Shotgun:
                EquipShotgun();
                break;
            case Weapons.Rifle:
                EquipRifle();
                break;
            default:
                break;
        }

        torchObject.SetActive(data.isTorchActive);
    }

    public Weapons GetCurrectWeaponType() => currentWeaponType;

    class SaveData
    {
        public string currentWeapon;
        public bool isTorchActive;
    }
}
