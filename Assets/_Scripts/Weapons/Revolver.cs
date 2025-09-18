using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using FMODUnity;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using DG.Tweening;

[RequireComponent(typeof(SaveableEntity))]
public class Revolver : MonoBehaviour, IAmAWeapon, ISaveable
{
    /*
        6 bullets are max for this weapon
    */

    [Header("Effects")]
    [SerializeField] private GameObject bloodVFX;

    [Header("Meshes")]
    [SerializeField] private List<Transform> bulletModels;
    [SerializeField] private Transform cylinder;

    [Header("Melee")]
    [SerializeField] private float meleeDamage = 10f;
    [SerializeField] private float meleeRange = 2f;
    [SerializeField] private float meleeRadius = 0.5f;

    [SerializeField, Range(0f, 1f)]
    private float meleeStunChance = 0.6f;
    [SerializeField] private LayerMask monsterLayerMask;
    private HashSet<AIHealth> meleeHitTargets = new HashSet<AIHealth>();
    private bool meleeHasImpacted = false;

    [Header("Debug")]
    [SerializeField] private bool drawMeleeGizmo = true;

    [Header("Shooting")]
    [SerializeField] private float damage = 10f;

    [SerializeField, Range(0f, 1f)]
    private float stunChance = 0.2f;

    [SerializeField, Range(0f, 1f)]
    private float critChance = 0.1f;
    [SerializeField] private float critMultiplier = 2f;
    [SerializeField] private float shootingDelayMax = 1f;
    [SerializeField] private float range = 25f;
    [SerializeField] private InventoryItemSO ammoType;
    [SerializeField] private float cylinderRotationSpeed = 10f;
    [SerializeField] private float spread = 10f;
    [SerializeField] private float movingSpread = 1.2f;
    [SerializeField] private GameObject bulletTrial;
    [SerializeField] private GameObject bulletHoleDecal;
    [SerializeField] private Transform shootingPoint;
    [SerializeField] private GameObject smokeParticle;
    [SerializeField] private GameObject muzzleFlash;
    [SerializeField] private LayerMask shootingLayer;
    [SerializeField] private WeaponAmmoUI weaponAmmoUI;
    [SerializeField] private CinemachineImpulseSource impulseSource;
    [SerializeField] private EventReference shootSound;
    [SerializeField] private EventReference shootBlankSound;

    private const string SHOOT_GUN_MESH = "shoot";
    private const string RELOAD_INIT_GUN_MESH = "reload_init";
    private const string IS_RELOADING = "IsReloading";
    private const string SHOOT_1 = "Revolver_shoot_1";
    private const string SHOOT_2 = "Revolver_shoot_2";
    private const string RELOAD_INIT = "Revolver_reload_init";
    private const string RELOAD_INIT_DID_NOT_SHOOT = "Revolver_reload_init_did_not_shoot";
    private const string RELOAD_FINISH = "Revolver_reload_finish";
    private const string MELEE_ANIMATION = "Revolver_melee";

    private enum RevolverState { Idle, ReloadIdle }
    private Animator revolverAnimator;
    private PlayerAnimations playerAnimations;
    private RevolverState revolverState = RevolverState.Idle;
    private float shootingDelay = 0f;
    private bool canShoot = true;
    private bool reloadFinished = true;
    private int bulletsInCylinder = 0;
    private int currentBulletIndex = 0;
    private Camera fpsCamera;
    private bool isReloadInitInProgress = false;
    private int monsterLayerIndex;
    private Volume globalVolume;
    private DepthOfField depthOfField;
    private float damageMultiplier = 1f;

    private void Start()
    {
        // Debug.Log("Start");
        monsterLayerIndex = LayerMask.NameToLayer("Monster");
        revolverAnimator = GetComponent<Animator>();
        playerAnimations = PlayerAnimations.Instance;
        fpsCamera = Camera.main;
        UpdateBulletModels();
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
                cylinder.Rotate(Vector3.up * -cylinderRotationSpeed * Time.deltaTime, Space.Self);
                shootingDelay += Time.deltaTime;
            }
            else
            {
                canShoot = true;
                shootingDelay = 0f;
            }
        }
        // Debug.Log($"Bullets in cylinder: {bulletsInCylinder}");
        // Debug.Log($"Current Bullet Index: {currentBulletIndex}");
    }

    public void Fire(PlayerWeapons playerWeapons)
    {
        switch (revolverState)
        {
            case RevolverState.Idle:
                Shooting(playerWeapons);
                break;
            case RevolverState.ReloadIdle:
                InsertBullet();
                break;
        }

    }

    private void Shooting(PlayerWeapons playerWeapons)
    {
        Animator playerAnim = PlayerAnimations.Instance.GetAnimator();
        bool isSprinting = playerAnim.GetBool(PlayerConstants.IS_SPRINTING);

        if (canShoot && !isSprinting && reloadFinished)
        {
            if (bulletsInCylinder > 0)
            {
                float chance = UnityEngine.Random.Range(0f, 1f);
                string shootAnimation = chance > 0.5f ? SHOOT_1 : SHOOT_2;
                playerAnim.SetLayerWeight(1, 1);
                playerAnim.CrossFade(shootAnimation, 0f, 1);
                revolverAnimator.CrossFade(SHOOT_GUN_MESH, 0);
                canShoot = false;
                PerformRaycast(playerWeapons);
                bulletsInCylinder--;
                ReduceBulletIndex();
                GameObject smoke = Instantiate(smokeParticle, shootingPoint.position, shootingPoint.rotation, shootingPoint);
                GameObject flash = Instantiate(muzzleFlash, shootingPoint.position, shootingPoint.rotation, shootingPoint);
                Destroy(smoke, 1f);
                Destroy(flash, 0.1f);
                playerAnim.SetBool(PlayerConstants.DID_SHOOT, true);
                AudioManager.Instance.PlayOneShot(shootSound, transform.position);
                impulseSource.GenerateImpulse();
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
        RaycastHit hit;
        GameObject trail = Instantiate(bulletTrial);
        bool isPlayerMoving = playerAnimations.GetAnimator().GetBool(PlayerConstants.IS_WALKING);
        spread = isPlayerMoving ? movingSpread : spread;
        float spreadX = UnityEngine.Random.Range(-spread, spread);
        float spreadY = UnityEngine.Random.Range(-spread, spread);
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

            // Check if we hit an AI
            AIHealth aiHealth = hit.collider.GetComponentInParent<AIHealth>();
            if (aiHealth != null)
            {
                bool isCrit = Random.value < critChance;
                bool isStunned = Random.value < stunChance;

                float totalDamage = isCrit
                    ? damage * critMultiplier * damageMultiplier
                    : damage * damageMultiplier;

                // Debug.Log($"Total damage: {totalDamage}");
                // Debug.Log($"Damage Multiplier: {damageMultiplier}");
                // Debug.Log($"Crit Multiplier: {critMultiplier}");
                // Debug.Log($"isCrit: {isCrit}");

                aiHealth.TakeDamage(
                    totalDamage,
                    fpsCamera.transform.position,
                    false,
                    isStunned
                );

                // optional: play effects/sound
                if (isCrit)
                {
                    // crit VFX/SFX
                }
                if (isStunned)
                {
                    // stun VFX/SFX
                }
            }
            Debug.DrawLine(fpsCamera.transform.position, hit.point, Color.red, 5f);

            Rat rat = hit.collider.GetComponentInParent<Rat>();
            //? Check if hit is a Rat
            if (rat != null)
            {
                rat.TakeDamage(damage);
            }

            Parasite parasite = hit.collider.GetComponentInParent<Parasite>();
            //? Check if hit is a Parasite
            if (parasite != null)
            {
                parasite.Damage();
            }

        }
        else
        {
            lineRenderer.SetPosition(1, fpsCamera.transform.position + shootDirection * range);
        }
    }

    private void InsertBullet()
    {
        InventoryItem ammoInInventory = InventoryManager.Instance.GetItemByID(ammoType.itemID);
        if (ammoInInventory == null) return;
        if (ammoInInventory.quantity > 0)
            playerAnimations.RevolverInsertBullet();
    }

    public void Reload(PlayerWeapons playerWeapons)
    {
        switch (revolverState)
        {
            case RevolverState.Idle:
                ReloadStart();
                break;
            case RevolverState.ReloadIdle:
                ReloadFinish();
                break;
        }
    }

    private void ReloadStart()
    {
        if (!canShoot) return;
        Animator playerAnim = playerAnimations.GetAnimator();
        bool didShoot = playerAnim.GetBool(PlayerConstants.DID_SHOOT);
        playerAnim.SetLayerWeight(1, 1);
        playerAnim.CrossFade(didShoot ? RELOAD_INIT : RELOAD_INIT_DID_NOT_SHOOT, 0f, 1);
        revolverAnimator.SetBool(IS_RELOADING, true);
        revolverAnimator.CrossFade(RELOAD_INIT_GUN_MESH, 0);
        revolverState = RevolverState.ReloadIdle;
        reloadFinished = false;
        isReloadInitInProgress = true;
        weaponAmmoUI.SetShouldShow(true);
        DOTween.To(() => depthOfField.focalLength.value, x => depthOfField.focalLength.value = x, 40f, .7f);
    }

    public void FinishReload(PlayerWeapons playerWeapons)
    {
        // Debug.Log("FinishReload called from PlayerWeapons");
        ReloadFinish();
        SetReloadFinished();
    }

    private void ReloadFinish()
    {
        if (isReloadInitInProgress) return;

        Animator playerAnim = playerAnimations.GetAnimator();
        playerAnim.CrossFade(RELOAD_FINISH, 0.1f, 1);
        revolverAnimator.SetBool(IS_RELOADING, false);
        revolverState = RevolverState.Idle;
        weaponAmmoUI.SetShouldShow(false);
        playerAnim.SetBool(PlayerConstants.DID_SHOOT, false);
        DOTween.To(() => depthOfField.focalLength.value, x => depthOfField.focalLength.value = x, 1f, .5f);
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

    //used in animation event
    public void OnMeleeImpact()
    {
        if (meleeHasImpacted) return;

        Vector3 origin = fpsCamera.transform.position;
        Vector3 dir = fpsCamera.transform.forward;
        Vector3 center = origin + dir * meleeRange;

        Collider[] hits = Physics.OverlapSphere(center, meleeRadius, monsterLayerMask);
        foreach (var col in hits)
        {
            var ai = col.GetComponentInParent<AIHealth>();
            if (ai != null && !meleeHitTargets.Contains(ai))
            {
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

    public void HideUsedBullets()
    {
        UpdateBulletModels();
    }

    //! Bullet is added here
    public void AddBulletToCylinder()
    {
        if (bulletsInCylinder > 5) return;
        bulletsInCylinder++;
        IncreaseBulletIndex();
        UpdateBulletModels();
        InventoryManager.Instance.DeductItemQuantity(ammoType.itemID);
    }

    private void UpdateBulletModels()
    {
        cylinder.localEulerAngles = Vector3.zero;
        for (int i = 0; i < bulletModels.Count; i++)
        {
            bulletModels[i].gameObject.SetActive(i < bulletsInCylinder);
        }
    }

    public bool HasSpaceInCylinder() => bulletsInCylinder < 6;

    public void SetReloadFinished()
    {
        reloadFinished = true;
    }


    public void SetReloadInitInProgress(bool value) => isReloadInitInProgress = value;

    public bool IsReloadInProgress() => !reloadFinished;

    private void ReduceBulletIndex() => currentBulletIndex = currentBulletIndex == 0 ? 5 : currentBulletIndex - 1;

    private void IncreaseBulletIndex() => currentBulletIndex = currentBulletIndex == 5 ? 0 : currentBulletIndex + 1;

    public void SetDamageMultiplier(float multiplier) => damageMultiplier = multiplier;
    public void SetFireRateBuff(float multiplier) => shootingDelayMax *= multiplier;

    public string GetUniqueIdentifier()
    {
        return GetComponent<SaveableEntity>().UniqueId;
    }

    public object CaptureState()
    {
        return new SaveData
        {
            bulletsInCylinder = bulletsInCylinder,
            currentBulletIndex = currentBulletIndex,
            shootingDelayMax = shootingDelayMax,
            damageMultiplier = damageMultiplier
        };
    }

    public void RestoreState(object state)
    {
        // Debug.Log("Restoring revolver state...");
        string json = state as string;
        SaveData data = JsonUtility.FromJson<SaveData>(json);
        bulletsInCylinder = data.bulletsInCylinder;
        currentBulletIndex = data.currentBulletIndex;
        shootingDelayMax = data.shootingDelayMax;
        damageMultiplier = data.damageMultiplier;
        UpdateBulletModels();
    }

    class SaveData
    {
        public int bulletsInCylinder;
        public int currentBulletIndex;
        public float shootingDelayMax;
        public float damageMultiplier;
    }
}
