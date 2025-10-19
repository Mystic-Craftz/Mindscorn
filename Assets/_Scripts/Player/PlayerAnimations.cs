using UnityEngine;

public class PlayerAnimations : MonoBehaviour
{
    public static PlayerAnimations Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Animator animator;

    [SerializeField] private GameObject revolverBullet;
    [SerializeField] private GameObject rifleBullet;
    [SerializeField] private GameObject shotgunBullet;

    [SerializeField] private PocketKnife knife;
    [SerializeField] private Revolver revolver;
    [SerializeField] private Shotgun shotgun;
    [SerializeField] private Rifle rifle;

    private bool canInsertBullet = true;
    private PlayerWeapons.Weapons targetWeaponType;
    private PlayerWeapons playerWeapons;

    /*
    * Utility Functions
    */

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        playerWeapons = PlayerWeapons.Instance;
    }

    public Animator GetAnimator()
    {
        return animator;
    }

    public void ChangeWeapon(PlayerWeapons.Weapons currentType, PlayerWeapons.Weapons targetType)
    {
        targetWeaponType = targetType;
        if (currentType != PlayerWeapons.Weapons.None)
        {
            animator.CrossFade($"Arms_{currentType}_exit", 0f);
            if (currentType == PlayerWeapons.Weapons.Knife) knife.PlayExitAnimation();
        }
        else
        {
            playerWeapons = PlayerWeapons.Instance;
            playerWeapons.OnSafeToChangeWeapon?.Invoke();
            animator.CrossFade($"Arms_{targetWeaponType}_enter", 0f);
            if (targetWeaponType == PlayerWeapons.Weapons.Knife) knife.PlayEnterAnimation();
        }
    }

    public void SafeToChangeLayer()
    {
        if (targetWeaponType == PlayerWeapons.Weapons.None)
        {
            playerWeapons.OnSafeToChangeWeapon?.Invoke();

        }
        else
        {
            playerWeapons.OnSafeToChangeWeapon?.Invoke();
            animator.CrossFade($"Arms_{targetWeaponType}_enter", 0f);
            if (targetWeaponType == PlayerWeapons.Weapons.Knife) knife.PlayEnterAnimation();
        }
    }

    public void ResetInsertBullet()
    {
        canInsertBullet = true;
    }

    /*
    ***************************************************************************************
    */

    /*
    * Knife Functions
    */

    public void KnifeMelee() => knife.OnMeleeImpact();

    /*
    ***************************************************************************************
    */

    /*
    * Revolver Functions
    */

    public void RevolverInsertBullet()
    {
        if (revolver.HasSpaceInCylinder() && canInsertBullet)
        {
            animator.SetTrigger(PlayerConstants.INSERT_BULLET);
            canInsertBullet = false;
        }
    }

    public void EmptyCylinder()
    {
        revolver.HideUsedBullets();
    }

    public void SetRevolverReloadInitInProgressFinished() => revolver.SetReloadInitInProgress(false);

    public void UpdateRevolverBullets()
    {
        revolver.AddBulletToCylinder();
        PlayerAnimationSounds.Instance.RevolverInsertBulletSound();
    }

    public void SetRevolverReloadFinished()
    {
        canInsertBullet = true;
        revolver.SetReloadFinished();
    }

    public void TurnRevolverBulletOn() => revolverBullet.SetActive(true);

    public void TurnRevolverBulletOff() => revolverBullet.SetActive(false);

    public void RevolverMelee()
    {
        revolver.OnMeleeImpact();
    }


    /*
    ***************************************************************************************
    */

    /*
    * Shotgun Functions
    */

    public void SetShotgunReloadInitInProgressFinished() => shotgun.SetReloadInitInProgress(false);

    public void ShotgunInsertBullet()
    {
        if (shotgun.HasSpaceInCylinder() && canInsertBullet)
        {
            animator.SetTrigger(PlayerConstants.INSERT_BULLET);
            canInsertBullet = false;
        }
    }

    public void SetShotgunReloadFinished()
    {
        canInsertBullet = true;
        shotgun.SetReloadFinished();
    }

    public void AddBulletInShotgun()
    {
        shotgun.AddBullet();
        PlayerAnimationSounds.Instance.ShotgunInsertBulletSound();
    }

    public void PumpOutBullet()
    {
        shotgun.ShitOutBullet();
    }

    public void SetSafeToFinishReloadShotgun() => shotgun.SetSafeToFinishReload(true);
    public void SetUnsafeToFinishReloadShotgun() => shotgun.SetSafeToFinishReload(false);

    public void SwitchShotgunIdleToEmpty()
    {
        shotgun.PlayShotgunSwitchReloadIdleAnimation();
        shotgun.SetSafeToFinishReload(false);
    }

    public void TurnShotgunBulletOn() => shotgunBullet.SetActive(true);

    public void TurnShotgunBulletOff() => shotgunBullet.SetActive(false);

    public void ShotgunMelee()
    {
        shotgun.OnMeleeImpact();
    }

    /*
    ***************************************************************************************
    */

    /*
    * Rifle Functions
    */

    public void RifleInsertBullet()
    {
        if (rifle.HasSpaceInCylinder() && canInsertBullet)
        {
            animator.SetTrigger(PlayerConstants.INSERT_BULLET);
            canInsertBullet = false;
        }
    }

    public void SetRifleReloadInitInProgressFinished() => rifle.SetReloadInitInProgress(false);

    public void AddBulletInRifle()
    {
        rifle.AddBullet();
        PlayerAnimationSounds.Instance.RifleInsertBulletSound();
    }

    public void SetRifleReloadFinished()
    {
        canInsertBullet = true;
        rifle.SetReloadFinished();
    }

    public void RifleChamberBullet()
    {
        rifle.ChamberBullet();
    }

    public void TurnRifleBulletOn() => rifleBullet.SetActive(true);

    public void TurnRifleBulletOff() => rifleBullet.SetActive(false);

    public void RifleMelee()
    {
        rifle.OnMeleeImpact();
    }

    /*
    ***************************************************************************************
    */
}
