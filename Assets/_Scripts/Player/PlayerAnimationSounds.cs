using UnityEngine;
using FMODUnity;

public class PlayerAnimationSounds : MonoBehaviour
{
    public static PlayerAnimationSounds Instance { get; private set; }

    [SerializeField] private Revolver revolver;
    [SerializeField] private Shotgun shotgun;
    [SerializeField] private Rifle rifle;

    [SerializeField] private EventReference revolverCylinderClose;
    [SerializeField] private EventReference revolverCylinderOpen;
    [SerializeField] private EventReference revolverInsertBullet;
    [SerializeField] private EventReference revolverEjectBullets;
    [SerializeField] private EventReference revolverDraw;
    [SerializeField] private EventReference revolverExit;
    [SerializeField] private EventReference revolverMeleeAttack;
    [SerializeField] private EventReference shotgunInsertBullet;
    [SerializeField] private EventReference shotgunPumpOpen;
    [SerializeField] private EventReference shotgunPumpClose;
    [SerializeField] private EventReference shotgunShoot;
    [SerializeField] private EventReference shotgunDrawExit;
    [SerializeField] private EventReference shotgunMelee;
    [SerializeField] private EventReference rifleShoot;
    [SerializeField] private EventReference rifleChamberOpen;
    [SerializeField] private EventReference rifleChamberClose;
    [SerializeField] private EventReference rifleInsertBullet;
    [SerializeField] private EventReference rifleMeleeSound;

    private void Awake()
    {
        Instance = this;
    }

    public void RevolverCylinderClose()
    {
        AudioManager.Instance.PlayOneShot(revolverCylinderClose, revolver.transform.position);
    }

    public void RevolverCylinderOpen()
    {
        AudioManager.Instance.PlayOneShot(revolverCylinderOpen, revolver.transform.position);
    }

    public void RevolverInsertBulletSound()
    {
        AudioManager.Instance.PlayOneShot(revolverInsertBullet, revolver.transform.position);
    }

    public void RevolverEjectBulletSound()
    {
        AudioManager.Instance.PlayOneShot(revolverEjectBullets, revolver.transform.position);
    }

    public void RevolverDrawSound()
    {
        AudioManager.Instance.PlayOneShot(revolverDraw, revolver.transform.position);
    }

    public void RevolverExitSound()
    {
        AudioManager.Instance.PlayOneShot(revolverExit, revolver.transform.position);
    }

    public void RevolverMeleeAttackSound()
    {
        AudioManager.Instance.PlayOneShot(revolverMeleeAttack, revolver.transform.position);
    }

    public void ShotgunInsertBulletSound()
    {
        AudioManager.Instance.PlayOneShot(shotgunInsertBullet, shotgun.transform.position);
    }

    public void ShotgunPumpOpenSound()
    {
        AudioManager.Instance.PlayOneShot(shotgunPumpOpen, shotgun.transform.position);
    }

    public void ShotgunPumpCloseSound()
    {
        AudioManager.Instance.PlayOneShot(shotgunPumpClose, shotgun.transform.position);
    }

    public void ShotgunShootSound()
    {
        AudioManager.Instance.PlayOneShot(shotgunShoot, shotgun.transform.position);
    }

    public void ShotgunDrawExitSound()
    {
        AudioManager.Instance.PlayOneShot(shotgunDrawExit, shotgun.transform.position);
    }

    public void ShotgunMeleeSound()
    {
        AudioManager.Instance.PlayOneShot(shotgunMelee, shotgun.transform.position);
    }

    public void RifleShootSound()
    {
        AudioManager.Instance.PlayOneShot(rifleShoot, rifle.transform.position);
    }

    public void RifleChamberOpenSound()
    {
        AudioManager.Instance.PlayOneShot(rifleChamberOpen, rifle.transform.position);
    }

    public void RifleChamberCloseSound()
    {
        AudioManager.Instance.PlayOneShot(rifleChamberClose, rifle.transform.position);
    }

    public void RifleInsertBulletSound()
    {
        AudioManager.Instance.PlayOneShot(rifleInsertBullet, rifle.transform.position);
    }

    public void RifleMeleeSound()
    {
        AudioManager.Instance.PlayOneShot(rifleMeleeSound, rifle.transform.position);
    }
}
