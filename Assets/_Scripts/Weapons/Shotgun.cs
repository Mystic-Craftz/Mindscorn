using Unity.Cinemachine;
using UnityEngine;
using FMODUnity;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using DG.Tweening;

[RequireComponent(typeof(SaveableEntity))]
public class Shotgun : MonoBehaviour, IAmAWeapon, ISaveable
{
    /*
        7 bullets are max for this weapon
    */

    [Header("Effects")]
    [SerializeField] private GameObject bloodVFX;

    [Header("Meshes")]
    [SerializeField] private Transform bulletModel;

    [Header("Melee")]
    [SerializeField] private float meleeDamage = 10f;
    [SerializeField] private float meleeRange = 1.5f;
    [SerializeField] private float meleeRadius = 0.8f;

    [SerializeField, Range(0f, 1f)]
    private float meleeStunChance = 0.6f;
    [SerializeField] private LayerMask meleeLayer;
    [SerializeField] private bool drawMeleeGizmo = true;
    private HashSet<AIHealth> meleeHitTargets = new HashSet<AIHealth>();
    private bool meleeHasImpacted = false;


    [Header("Shooting")]
    [SerializeField] private float damagePerHit = 10f;

    [SerializeField, Range(0f, 1f)]
    private float stunChance = 0.2f;

    [SerializeField, Range(0f, 1f)]
    private float critChance = 0.1f;
    [SerializeField] private float critMultiplier = 2f;
    [SerializeField] private float maxDistanceForHardShot = 3.0f;
    [SerializeField] private int shotsPerBullet = 7;
    [SerializeField] private float shootingDelayMax = 1f;
    [SerializeField] private float range = 25f;
    [SerializeField] private float spread = 10f;
    [SerializeField] private float movingSpread = 1.2f;
    [SerializeField] private GameObject bulletTrial;
    [SerializeField] private GameObject bulletHoleDecal;
    [SerializeField] private Transform shootingPoint;
    [SerializeField] private GameObject smokeParticle;
    [SerializeField] private GameObject muzzleFlash;
    [SerializeField] private GameObject muzzleAshes;
    [SerializeField] private WeaponAmmoUI weaponAmmoUI;
    [SerializeField] private GameObject bulletPumpParticle;
    [SerializeField] private Transform bulletPumpParticleLocation;
    [SerializeField] private InventoryItemSO ammoType;
    [SerializeField] private LayerMask shootingLayer;
    [SerializeField] private CinemachineImpulseSource impulseSource;
    [SerializeField] private EventReference shootBlankSound;

    private const string GUN_SHOOT = "Shotgun_shoot";
    private const string GUN_RELOAD_INIT_EMPTY = "Shotgun_reload_init_empty";
    private const string GUN_RELOAD_INIT_NOT_EMPTY = "Shotgun_reload_init_not_empty";
    private const string GUN_RELOAD_EXIT_NOT_EMPTY = "Shotgun_reload_exit_not_empty";
    private const string GUN_RELOAD_EXIT_EMPTY = "Shotgun_reload_exit_empty";
    private const string GUN_SWITCH_IDLE_TO_NOT_EMPTY = "Shotgun_switch_idle_to_not_empty";
    private const string GUN_IS_RELOADING = "IsReloading";
    private const string PLAYER_SHOOT = "Shotgun_shoot";
    private const string PLAYER_RELOAD_FINISH_EMPTY = "Shotgun_reload_finish_empty";
    private const string PLAYER_RELOAD_FINISH_NOT_EMPTY = "Shotgun_reload_finish_not_empty";
    private const string PLAYER_RELOAD_INIT_EMPTY = "Shotgun_reload_init_empty";
    private const string PLAYER_RELOAD_INIT_NOT_EMPTY = "Shotgun_reload_init_not_empty";
    private const string MELEE_ANIMATION = "Shotgun_melee";

    private enum ShotgunState { Idle, ReloadIdle }
    private Animator shotgunAnimator;
    private PlayerAnimations playerAnimations;
    private ShotgunState shotgunState = ShotgunState.Idle;
    private float shootingDelay = 0f;
    private bool canShoot = true;
    private bool reloadFinished = true;
    private bool isSafeToFinishReload = true;
    private int bulletsInReserve = 0;
    private Camera fpsCamera;
    private bool isReloadInitInProgress = false;
    private int monsterLayerIndex;
    private Volume globalVolume;
    private DepthOfField depthOfField;

    private void Start()
    {
        monsterLayerIndex = LayerMask.NameToLayer("Monster");
        shotgunAnimator = GetComponent<Animator>();
        playerAnimations = PlayerAnimations.Instance;
        fpsCamera = Camera.main;
        if (bulletsInReserve < 1)
        {
            bulletModel.gameObject.SetActive(false);
            playerAnimations.GetAnimator().SetBool(PlayerConstants.IS_GUN_EMPTY, true);
        }
        globalVolume = GameObject.Find("Global Volume").GetComponent<Volume>();
        if (globalVolume.profile.TryGet(out DepthOfField dof))
            depthOfField = dof;
    }

    private void Update()
    {
        if (!canShoot)
        {
            if (shootingDelay <= shootingDelayMax)
            {
                shootingDelay += Time.deltaTime;
            }
            else
            {
                canShoot = true;
                shootingDelay = 0f;
            }
        }
    }

    public void Fire(PlayerWeapons playerWeapons)
    {
        switch (shotgunState)
        {
            case ShotgunState.Idle:
                Shooting(playerWeapons);
                break;
            case ShotgunState.ReloadIdle:
                InsertBullet();
                break;
        }
    }

    private void InsertBullet()
    {
        InventoryItem ammoInInventory = InventoryManager.Instance.GetItemByID(ammoType.itemID);
        if (ammoInInventory == null) return;
        if (ammoInInventory.quantity > 0)
            playerAnimations.ShotgunInsertBullet();
    }

    private void Shooting(PlayerWeapons playerWeapons)
    {
        Animator playerAnim = playerAnimations.GetAnimator();
        bool isSprinting = playerAnim.GetBool(PlayerConstants.IS_SPRINTING);

        if (canShoot && !isSprinting && reloadFinished)
        {
            if (bulletsInReserve > 0)
            {

                playerAnim.SetLayerWeight(1, 1);
                playerAnim.CrossFade(PLAYER_SHOOT, 0f, 1);
                shotgunAnimator.CrossFade(GUN_SHOOT, 0f);
                canShoot = false;
                PerformRaycast(playerWeapons);
                bulletsInReserve--;
                GameObject smoke = Instantiate(smokeParticle, shootingPoint.position, shootingPoint.rotation, shootingPoint);
                GameObject flash = Instantiate(muzzleFlash, shootingPoint.position, shootingPoint.rotation, shootingPoint);
                GameObject ashes = Instantiate(muzzleAshes, shootingPoint.position, shootingPoint.rotation, shootingPoint);
                Destroy(smoke, 1f);
                Destroy(flash, 0.1f);
                Destroy(ashes, 10f);
                impulseSource.GenerateImpulse();
                if (bulletsInReserve < 1)
                {
                    playerAnim.SetBool(PlayerConstants.IS_GUN_EMPTY, true);
                    bulletModel.gameObject.SetActive(false);
                }
            }
            else
            {
                AudioManager.Instance.PlayOneShot(shootBlankSound, transform.position);
                canShoot = false;
            }

        }
    }

    private void PerformRaycast(PlayerWeapons playerWeapons)
    {
        for (int index = 1; index < shotsPerBullet; ++index)
        {
            RaycastHit hit;
            GameObject trail = Instantiate(bulletTrial);
            bool isPlayerMoving = playerAnimations.GetAnimator().GetBool(PlayerConstants.IS_WALKING);
            spread = isPlayerMoving ? movingSpread : spread;

            float offsetX = Random.Range(-spread, spread);
            float offsetY = Random.Range(-spread * 2, spread * 2);
            Vector3 rightOffset = fpsCamera.transform.right * offsetX;
            Vector3 upOffset = fpsCamera.transform.up * offsetY;

            Vector3 shootDirection = (fpsCamera.transform.forward + rightOffset + upOffset).normalized;

            LineRenderer lineRenderer = trail.GetComponent<LineRenderer>();
            lineRenderer.SetPosition(0, shootingPoint.position);

            if (Physics.Raycast(fpsCamera.transform.position, shootDirection, out hit, range, shootingLayer))
            {
                lineRenderer.SetPosition(1, hit.point);

                if (hit.collider.gameObject.layer == monsterLayerIndex)
                {
                    GameObject blood = Instantiate(bloodVFX, hit.point, Quaternion.LookRotation(hit.normal));
                    Destroy(blood, 2f);
                }

                // Only spawn a decal if we don’t hit a monster
                if (hit.collider.gameObject.layer != monsterLayerIndex)
                {
                    GameObject decal = Instantiate(
                        bulletHoleDecal,
                        hit.point,
                        Quaternion.LookRotation(hit.normal)
                    );
                    Destroy(decal, 5f);
                }

                AIHealth aiHealth = hit.collider.GetComponentInParent<AIHealth>();
                if (aiHealth != null)
                {
                    float dist = hit.distance;
                    bool isHard = dist <= maxDistanceForHardShot;
                    bool isCrit = Random.value < critChance;

                    bool isStun = !isHard && (Random.value < stunChance);

                    float finalDamage = isCrit
                        ? damagePerHit * critMultiplier
                        : damagePerHit;

                    aiHealth.TakeDamage(finalDamage, fpsCamera.transform.position, isHard, isStun);

                    if (isCrit)
                    {
                        // Play crit effect
                    }
                    if (isStun)
                    {
                        // Play stun effect
                    }
                }


                Debug.DrawLine(fpsCamera.transform.position, hit.point, Color.red, 5f);
                Rat rat = hit.collider.GetComponentInParent<Rat>();
                //? Check if hit is a Rat
                if (rat != null)
                {
                    rat.TakeDamage(1f);
                }

                Parasite parasite = hit.collider.GetComponentInParent<Parasite>();
                //? Check if hit is a Parasite
                if (parasite != null)
                {
                    parasite.Damage();
                }

                DirectorBoss director = hit.collider.GetComponentInParent<DirectorBoss>();
                //? Check if hit is the Director Boss
                if (director != null)
                {
                    bool isCrit = Random.value < critChance;
                    bool isStunned = Random.value < stunChance / 2;

                    float totalDamage = isCrit
                        ? damagePerHit * critMultiplier
                        : damagePerHit;

                    director.Damage(totalDamage, hit.collider.gameObject, isStunned);
                }

                ThrowableLimb limb = hit.collider.GetComponent<ThrowableLimb>();
                if (limb != null)
                {
                    limb.Damage(hit, PlayerController.Instance.transform);
                }

                BossHealth bossHealth = hit.collider.GetComponentInParent<BossHealth>();
                if (bossHealth != null)
                {
                    bossHealth.TakeDamage(damagePerHit);
                }

            }
            else
            {
                lineRenderer.SetPosition(1, fpsCamera.transform.position + shootDirection * range);
            }
        }
    }

    public void Reload(PlayerWeapons playerWeapons)
    {
        switch (shotgunState)
        {
            case ShotgunState.Idle:
                ReloadStart();
                break;
            case ShotgunState.ReloadIdle:
                ReloadFinish();
                break;
        }
    }

    public void FinishReload(PlayerWeapons playerWeapons)
    {
        ReloadFinish();
    }

    private void ReloadFinish()
    {
        if (isReloadInitInProgress || !isSafeToFinishReload) return;

        Animator playerAnim = playerAnimations.GetAnimator();
        bool isGunEmpty = playerAnim.GetBool(PlayerConstants.IS_GUN_EMPTY);
        if (isGunEmpty)
        {
            playerAnim.CrossFade(PLAYER_RELOAD_FINISH_EMPTY, 0f, 1);
            shotgunAnimator.CrossFade(GUN_RELOAD_EXIT_EMPTY, 0);
        }
        else
        {
            playerAnim.CrossFade(PLAYER_RELOAD_FINISH_NOT_EMPTY, 0f, 1);
            shotgunAnimator.CrossFade(GUN_RELOAD_EXIT_NOT_EMPTY, 0);
        }
        shotgunAnimator.SetBool(GUN_IS_RELOADING, false);
        shotgunState = ShotgunState.Idle;
        weaponAmmoUI.SetShouldShow(false);
        DOTween.To(() => depthOfField.focalLength.value, x => depthOfField.focalLength.value = x, 1f, .5f);
    }

    private void ReloadStart()
    {
        if (!canShoot || !isSafeToFinishReload) return;
        Animator playerAnim = PlayerAnimations.Instance.GetAnimator();
        bool isGunEmpty = playerAnim.GetBool(PlayerConstants.IS_GUN_EMPTY);
        playerAnim.SetLayerWeight(1, 1);
        if (isGunEmpty)
        {
            playerAnim.CrossFade(PLAYER_RELOAD_INIT_EMPTY, 0f, 1);
            shotgunAnimator.CrossFade(GUN_RELOAD_INIT_EMPTY, 0);
        }
        else
        {
            playerAnim.CrossFade(PLAYER_RELOAD_INIT_NOT_EMPTY, 0f, 1);
            shotgunAnimator.CrossFade(GUN_RELOAD_INIT_NOT_EMPTY, 0);
        }

        shotgunAnimator.SetBool(GUN_IS_RELOADING, true);
        shotgunState = ShotgunState.ReloadIdle;
        reloadFinished = false;
        isReloadInitInProgress = true;
        weaponAmmoUI.SetShouldShow(true);
        DOTween.To(() => depthOfField.focalLength.value, x => depthOfField.focalLength.value = x, 40f, .7f);
    }

    public void Melee(PlayerWeapons playerWeapons)
    {
        meleeHitTargets.Clear();
        meleeHasImpacted = false;

        Animator playerAnim = playerAnimations.GetAnimator();
        bool isSprinting = playerAnim.GetBool(PlayerConstants.IS_SPRINTING);

        if (!isSprinting && reloadFinished)
        {
            playerAnim.SetLayerWeight(1, 1);
            playerAnim.CrossFade(MELEE_ANIMATION, 0f, 1);
            canShoot = false;
        }
    }


    public void OnMeleeImpact()
    {
        if (meleeHasImpacted) return;

        Vector3 origin = fpsCamera.transform.position;
        Vector3 dir = fpsCamera.transform.forward;
        Vector3 center = origin + dir * meleeRange;

        Collider[] hits = Physics.OverlapSphere(center, meleeRadius, meleeLayer);
        foreach (var col in hits)
        {
            var ai = col.GetComponentInParent<AIHealth>();
            if (ai != null && !meleeHitTargets.Contains(ai))
            {
                // CHANGED: roll for stun chance instead of always true
                bool isStunned = UnityEngine.Random.value < meleeStunChance;
                ai.TakeDamage(meleeDamage, transform.position, isHard: false, isStun: isStunned);
                meleeHitTargets.Add(ai);
            }
        }
        meleeHasImpacted = true;
    }


    private void OnDrawGizmosSelected()
    {
        if (!drawMeleeGizmo || fpsCamera == null) return;

        Gizmos.color = Color.red;
        Vector3 origin = fpsCamera.transform.position;
        Vector3 dir = fpsCamera.transform.forward;
        Vector3 center = origin + dir * meleeRange;

        Gizmos.DrawWireSphere(center, meleeRadius);
        Gizmos.DrawRay(origin, dir * meleeRange);
    }


    //! Bullet is added here
    public void AddBullet()
    {
        if (bulletsInReserve > 6) return;
        bulletsInReserve++;
        bulletModel.gameObject.SetActive(true);
        playerAnimations.GetAnimator().SetBool(PlayerConstants.IS_GUN_EMPTY, false);
        InventoryManager.Instance.DeductItemQuantity(ammoType.itemID);
    }

    public void ShitOutBullet()
    {
        GameObject bulletParticle = Instantiate(bulletPumpParticle, bulletPumpParticleLocation);
        Destroy(bulletParticle, 3f);
    }

    public void PlayShotgunSwitchReloadIdleAnimation()
    {
        shotgunAnimator.CrossFade(GUN_SWITCH_IDLE_TO_NOT_EMPTY, 0f);
    }
    public void SetReloadFinished() => reloadFinished = true;

    public void SetReloadInitInProgress(bool value) => isReloadInitInProgress = value;

    public bool HasSpaceInCylinder() => bulletsInReserve < 7;

    public bool IsReloadInProgress() => !reloadFinished;

    public void SetCritChance(float value) => critChance = value;

    public void SetFireRateBuff(float multiplier)
    {
        Animator playerAnim = playerAnimations.GetAnimator();
        float oldShootingDelayMax = shootingDelayMax;
        shootingDelayMax *= multiplier;
        float newShootAnimationMP = playerAnim.GetFloat("ShotgunShootMP") + oldShootingDelayMax - shootingDelayMax + .1f;
        playerAnim.SetFloat("ShotgunShootMP", newShootAnimationMP);
        shotgunAnimator.SetFloat("ShootMP", newShootAnimationMP);
    }

    public void SetSafeToFinishReload(bool value) => isSafeToFinishReload = value;

    public string GetUniqueIdentifier()
    {
        return GetComponent<SaveableEntity>().UniqueId;
    }

    public object CaptureState()
    {
        return new SaveData
        {
            bulletsInReserve = bulletsInReserve,
            criticalChance = critChance,
            shootingDelayMax = shootingDelayMax
        };
    }

    public void RestoreState(object state)
    {
        Animator playerAnim = PlayerAnimations.Instance.GetAnimator();
        string json = state as string;
        SaveData data = JsonUtility.FromJson<SaveData>(json);
        bulletsInReserve = data.bulletsInReserve;
        critChance = data.criticalChance;
        float oldShootingDelayMax = shootingDelayMax;
        shootingDelayMax = data.shootingDelayMax;
        float newShootAnimationMP = playerAnim.GetFloat("ShotgunShootMP") + oldShootingDelayMax - shootingDelayMax + .1f;
        playerAnim.SetFloat("ShotgunShootMP", newShootAnimationMP);
        GetComponent<Animator>().SetFloat("ShootMP", newShootAnimationMP);
        if (bulletsInReserve < 1)
        {
            bulletModel.gameObject.SetActive(false);
        }
    }

    class SaveData
    {
        public int bulletsInReserve;
        public float criticalChance;
        public float shootingDelayMax;
    }
}
