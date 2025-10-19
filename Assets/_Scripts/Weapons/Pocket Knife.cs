using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

public class PocketKnife : MonoBehaviour, IAmAWeapon
{
    [Header("Effects")]
    [SerializeField] private GameObject bloodVFX;
    [Header("Components")]
    [SerializeField] private Animator knifeAnimator;

    [Header("Melee")]
    [SerializeField] private float lightAttackDamage = 10f;
    [SerializeField] private float heavyAttackDamage = 51f;
    [SerializeField] private float meleeRange = 2f;
    [SerializeField] private float meleeRadius = 0.5f;
    [SerializeField] private float attackCooldownMax = 0.5f;
    [SerializeField] private LayerMask monsterLayerMask;
    [SerializeField] private EventReference goreSound;

    [Header("Debug")]
    [SerializeField] private bool drawMeleeGizmo = true;

    private const string OPENING_KNIFE_ANIM = "opening";
    private const string Closing_KNIFE_ANIM = "closing";
    private const string KNIFE_SLASH_1 = "Knife_Slash_1";
    private const string KNIFE_SLASH_2 = "Knife_Slash_2";
    private const string KNIFE_HEAVY = "Knife_Heavy";

    private bool doingFirstAttack = true;
    private bool canAttack = true;
    private float attackTimer = 0f;
    private Camera fpsCamera;
    private HashSet<AIHealth> meleeHitTargets = new HashSet<AIHealth>();
    private bool meleeHasImpacted = false;
    private bool isDoingHeavyAttack = false;

    private void Start()
    {
        fpsCamera = Camera.main;
    }

    private void Update()
    {
        if (!canAttack)
        {
            attackTimer += Time.deltaTime;
            if (attackTimer >= attackCooldownMax)
            {
                canAttack = true;
                attackTimer = 0f;
            }
        }
    }

    public void FinishReload(PlayerWeapons playerWeapons)
    {
        //* Nothing here
    }

    public void Fire(PlayerWeapons playerWeapons)
    {
        var playerAnim = PlayerAnimations.Instance.GetAnimator();
        bool isSprinting = playerAnim.GetBool(PlayerConstants.IS_SPRINTING);
        if (canAttack && !isSprinting)
        {
            meleeHitTargets.Clear();
            meleeHasImpacted = false;
            canAttack = false;
            isDoingHeavyAttack = false;
            attackTimer = 0f;
            playerAnim.SetLayerWeight(1, 1);
            playerAnim.CrossFade(doingFirstAttack ? KNIFE_SLASH_1 : KNIFE_SLASH_2, 0f, 1);
            doingFirstAttack = !doingFirstAttack;
        }
    }

    public bool IsReloadInProgress()
    {
        return !canAttack;
    }

    public void Reload(PlayerWeapons playerWeapons)
    {
        //* Nothing here
    }

    public void DoHeavyAttack()
    {
        var playerAnim = PlayerAnimations.Instance.GetAnimator();
        bool isSprinting = playerAnim.GetBool(PlayerConstants.IS_SPRINTING);
        if (canAttack && !isSprinting)
        {
            meleeHitTargets.Clear();
            meleeHasImpacted = false;
            canAttack = false;
            isDoingHeavyAttack = true;
            attackTimer = -1f;
            playerAnim.SetLayerWeight(1, 1);
            playerAnim.CrossFade(KNIFE_HEAVY, 0f, 1);
            doingFirstAttack = !doingFirstAttack;
        }
    }

    public void OnMeleeImpact()
    {
        if (meleeHasImpacted) return;

        Vector3 dir = fpsCamera.transform.forward;

        RaycastHit[] hits = Physics.SphereCastAll(fpsCamera.transform.position, meleeRadius, dir, meleeRange, monsterLayerMask);
        foreach (var hit in hits)
        {
            var ai = hit.collider.GetComponentInParent<AIHealth>();
            if (ai != null && !meleeHitTargets.Contains(ai))
            {
                ai.TakeDamage(isDoingHeavyAttack ? heavyAttackDamage : lightAttackDamage, transform.position, isHard: false, isStun: false);
                GameObject blood = Instantiate(bloodVFX, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(blood, 2f);
                AudioManager.Instance.PlayOneShot(goreSound, hit.point);
                meleeHitTargets.Add(ai);
            }

            Rat rat = hit.collider.GetComponentInParent<Rat>();
            //? Check if hit is a Rat
            if (rat != null)
            {
                rat.TakeDamage(lightAttackDamage);
                GameObject blood = Instantiate(bloodVFX, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(blood, 2f);
                AudioManager.Instance.PlayOneShot(goreSound, hit.point);
            }

            Parasite parasite = hit.collider.GetComponentInParent<Parasite>();
            //? Check if hit is a Parasite
            if (parasite != null)
            {
                GameObject blood = Instantiate(bloodVFX, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(blood, 2f);
                parasite.Damage();
                AudioManager.Instance.PlayOneShot(goreSound, hit.point);
            }

            DirectorBoss director = hit.collider.GetComponentInParent<DirectorBoss>();
            //? Check if hit is the Director Boss
            if (director != null)
            {
                GameObject blood = Instantiate(bloodVFX, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(blood, 2f);
                AudioManager.Instance.PlayOneShot(goreSound, hit.point);
                director.Damage(isDoingHeavyAttack ? heavyAttackDamage : lightAttackDamage, hit.collider.gameObject, false);
            }

            ThrowableLimb limb = hit.collider.GetComponent<ThrowableLimb>();
            if (limb != null)
            {
                GameObject blood = Instantiate(bloodVFX, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(blood, 2f);
                AudioManager.Instance.PlayOneShot(goreSound, hit.point);
                limb.Damage(hit, PlayerController.Instance.transform);
            }

            BossHealth bossHealth = hit.collider.GetComponentInParent<BossHealth>();
            if (bossHealth != null)
            {
                GameObject blood = Instantiate(bloodVFX, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(blood, 2f);
                AudioManager.Instance.PlayOneShot(goreSound, hit.point);
                bossHealth.TakeDamage(isDoingHeavyAttack ? heavyAttackDamage : lightAttackDamage);
            }
        }
        meleeHasImpacted = true;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawMeleeGizmo || fpsCamera == null) return;

        Gizmos.color = Color.green;
        Vector3 origin = fpsCamera.transform.position;
        Vector3 dir = fpsCamera.transform.forward;
        Vector3 center = origin + dir * meleeRange;

        Gizmos.DrawWireSphere(center, meleeRadius);
        Gizmos.DrawRay(origin, dir * meleeRange);
    }

    public void PlayEnterAnimation()
    {
        knifeAnimator.Play(OPENING_KNIFE_ANIM);
    }

    public void PlayExitAnimation()
    {
        knifeAnimator.Play(Closing_KNIFE_ANIM);
    }
}
