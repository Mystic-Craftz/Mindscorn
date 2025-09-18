using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using FMODUnity;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(SaveableEntity))]
public class Rifle : MonoBehaviour, IAmAWeapon, ISaveable
{
    /*
        5 bullets are max for this weapon
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
    [SerializeField] private float damagePerHit = 90f;

    [SerializeField, Range(0f, 1f)]
    private float stunChance = 0.2f;

    [SerializeField, Range(0f, 1f)]
    private float critChance = 0.1f;
    [SerializeField] private float critMultiplier = 2f;
    [SerializeField] private float maxDistanceForHardShot = 3f;
    [SerializeField] private float distance = 0f;
    [SerializeField] private float shootingDelayMax = 1f;
    [SerializeField] private float range = 100f;
    [SerializeField] private float spread = 0.03f;
    [SerializeField] private float movingSpread = 0.04f;
    [SerializeField] private GameObject bulletTrial;
    [SerializeField] private GameObject bulletHoleDecal;
    [SerializeField] private Transform shootingPoint;
    [SerializeField] private GameObject smokeParticle;
    [SerializeField] private GameObject muzzleFlash;
    [SerializeField] private GameObject bulletPumpParticle;
    [SerializeField] private InventoryItemSO ammoType;
    [SerializeField] private WeaponAmmoUI weaponAmmoUI;
    [SerializeField] private Transform bulletPumpParticleLocation;
    [SerializeField] private LayerMask shootingLayer;
    [SerializeField] private CinemachineImpulseSource impulseSource;
    [SerializeField] private EventReference shootBlankSound;

    private const string SHOOT_GUN_MESH = "Rifle_shoot";
    private const string RELOAD_FINISH_GUN_MESH = "Finish_empty";
    private const string IS_RELOADING = "IsReloading";
    private const string SHOOT = "Rifle_shoot";
    private const string RELOAD_FINISH = "Rifle_reload_finish";
    private const string RELOAD_FINISH_EMPTY = "Rifle_reload_finish_empty";
    private const string RELOAD_INIT = "Rifle_reload_init";
    private const string MELEE_ANIMATION = "Rifle_melee";
    private const string IS_RIFLE_EMPTY = "IsRifleEmpty";

    private enum RifleState { Idle, ReloadIdle }
    private Animator rifleAnimator;
    private PlayerAnimations playerAnimations;
    private RifleState rifleState = RifleState.Idle;
    private float shootingDelay = 0f;
    private bool canShoot = true;
    private bool reloadFinished = true;
    private int bulletsInReserve = 0;
    private Camera fpsCamera;
    private bool isReloadInitInProgress;
    private int monsterLayerIndex;
    private Volume globalVolume;
    private DepthOfField depthOfField;


    private void Start()
    {
        monsterLayerIndex = LayerMask.NameToLayer("Monster");
        rifleAnimator = GetComponent<Animator>();
        playerAnimations = PlayerAnimations.Instance;
        fpsCamera = Camera.main;
        if (bulletsInReserve < 1)
        {
            bulletModel.gameObject.SetActive(false);
            Animator playerAnim = playerAnimations.GetAnimator();
            playerAnim.SetBool(IS_RIFLE_EMPTY, true);
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
        switch (rifleState)
        {
            case RifleState.Idle:
                Shooting(playerWeapons);
                break;
            case RifleState.ReloadIdle:
                InsertBullet();
                break;
        }
    }

    private void InsertBullet()
    {
        InventoryItem ammoInInventory = InventoryManager.Instance.GetItemByID(ammoType.itemID);
        if (ammoInInventory == null) return;
        if (ammoInInventory.quantity > 0)
            playerAnimations.RifleInsertBullet();
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
                playerAnim.CrossFade(SHOOT, 0f, 1);
                rifleAnimator.CrossFade(SHOOT_GUN_MESH, 0f);
                canShoot = false;
                PerformRaycast(playerWeapons);
                bulletsInReserve--;
                GameObject smoke = Instantiate(smokeParticle, shootingPoint.position, shootingPoint.rotation, shootingPoint);
                GameObject flash = Instantiate(muzzleFlash, shootingPoint.position, shootingPoint.rotation, shootingPoint);
                Destroy(smoke, 1f);
                Destroy(flash, 0.1f);
                impulseSource.GenerateImpulse();
                if (bulletsInReserve < 1)
                {
                    bulletModel.gameObject.SetActive(false);
                    playerAnim.SetBool(IS_RIFLE_EMPTY, true);
                }
            }
            else
            {
                AudioManager.Instance.PlayOneShot(shootBlankSound, transform.position);
            }
        }
    }

    private void PerformRaycast(PlayerWeapons playerWeapons)
    {
        RaycastHit hit;
        GameObject trail = Instantiate(bulletTrial);
        bool isPlayerMoving = playerAnimations.GetAnimator().GetBool(PlayerConstants.IS_WALKING);
        spread = isPlayerMoving ? movingSpread : spread;
        float spreadX = Random.Range(-spread, spread);
        float spreadY = Random.Range(-spread * 2, spread * 2);
        Vector3 shootDirection;

        if (playerWeapons.IsAiming() && isPlayerMoving)
        {
            float adjustedSpreadX = spreadX * 0.5f;
            float adjustedSpreadY = spreadY * 0.5f;
            shootDirection = (fpsCamera.transform.forward +
                              fpsCamera.transform.right * adjustedSpreadX +
                              fpsCamera.transform.up * adjustedSpreadY).normalized;
        }
        else if (playerWeapons.IsAiming() && !isPlayerMoving)
        {
            shootDirection = fpsCamera.transform.forward;
        }
        else if (!playerWeapons.IsAiming() && isPlayerMoving)
        {
            shootDirection = (fpsCamera.transform.forward +
                              fpsCamera.transform.right * spreadX +
                              fpsCamera.transform.up * spreadY).normalized;
        }
        else
        {
            shootDirection = (fpsCamera.transform.forward +
                              fpsCamera.transform.right * spreadX +
                              fpsCamera.transform.up * spreadY).normalized;
        }

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

            //Only spawn a decal if we donot hit a monster
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
            // Debug.Log(hit.transform.name);
        }
        else
        {
            lineRenderer.SetPosition(1, fpsCamera.transform.position + shootDirection * range);
        }
    }

    public void Reload(PlayerWeapons playerWeapons)
    {
        switch (rifleState)
        {
            case RifleState.Idle:
                ReloadStart();
                break;
            case RifleState.ReloadIdle:
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
        if (isReloadInitInProgress) return;

        Animator playerAnim = playerAnimations.GetAnimator();
        if (playerAnim.GetBool(IS_RIFLE_EMPTY) && bulletsInReserve > 0)
        {
            playerAnim.CrossFade(RELOAD_FINISH_EMPTY, 0.1f, 1);
            rifleAnimator.CrossFade(RELOAD_FINISH_GUN_MESH, 0.1f, 1);
            playerAnim.SetBool(IS_RIFLE_EMPTY, false);
        }
        else
        {
            playerAnim.CrossFade(RELOAD_FINISH, 0.1f, 1);
        }
        rifleAnimator.SetBool(IS_RELOADING, false);
        rifleState = RifleState.Idle;
        weaponAmmoUI.SetShouldShow(false);
        DOTween.To(() => depthOfField.focalLength.value, x => depthOfField.focalLength.value = x, 1f, .5f);
    }

    private void ReloadStart()
    {
        if (!canShoot) return;
        Animator playerAnim = playerAnimations.GetAnimator();
        playerAnim.SetLayerWeight(1, 1);
        playerAnim.CrossFade(RELOAD_INIT, 0f, 1);
        rifleAnimator.SetBool(IS_RELOADING, true);
        rifleState = RifleState.ReloadIdle;
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
        if (bulletsInReserve > 4) return;
        bulletsInReserve++;
        bulletModel.gameObject.SetActive(true);
        InventoryManager.Instance.DeductItemQuantity(ammoType.itemID);
    }

    public void ChamberBullet()
    {
        GameObject bulletParticle = Instantiate(bulletPumpParticle, bulletPumpParticleLocation);
        Destroy(bulletParticle, 3f);
    }

    public void SetReloadInitInProgress(bool value) => isReloadInitInProgress = value;

    public void SetReloadFinished() => reloadFinished = true;

    public bool HasSpaceInCylinder() => bulletsInReserve < 5;

    public bool IsReloadInProgress() => !reloadFinished;

    public void SetCritChance(float value) => critChance = value;

    public void SetFireRateBuff(float multiplier)
    {
        Animator playerAnim = playerAnimations.GetAnimator();
        float oldShootingDelayMax = shootingDelayMax;
        shootingDelayMax *= multiplier;
        float newShootAnimationMP = playerAnim.GetFloat("RifleShootMP") + oldShootingDelayMax - shootingDelayMax + .1f;
        playerAnim.SetFloat("RifleShootMP", newShootAnimationMP);
        rifleAnimator.SetFloat("ShootMP", newShootAnimationMP);
    }

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
        float newShootAnimationMP = playerAnim.GetFloat("RifleShootMP") + oldShootingDelayMax - shootingDelayMax + .1f;
        playerAnim.SetFloat("RifleShootMP", newShootAnimationMP);
        GetComponent<Animator>().SetFloat("ShootMP", newShootAnimationMP);
        if (bulletsInReserve < 1)
        {
            bulletModel.gameObject.SetActive(false);
        }
        gameObject.SetActive(true);
    }

    class SaveData
    {
        public int bulletsInReserve;
        public float criticalChance;
        public float shootingDelayMax;
    }
}
