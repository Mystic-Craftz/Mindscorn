using FMODUnity;
using Unity.Cinemachine;
using UnityEngine;

public class BossEndsDemo : MonoBehaviour
{
    public ParticleSystem shockwaveParticle;
    public StudioEventEmitter shockwaveEmitter;
    public GameObject teleporter;
    [SerializeField] private CinemachineCamera cam;
    private int counter = 0;

    public void EndDemo()
    {
        counter++;
        if (counter == 2)
        {
            shockwaveEmitter.Stop();
            shockwaveParticle.Stop();
            NeonDimensionController.Instance.LoseConsciousness();

            StartCoroutine(ActivateTeleporterAfterDelay(2f));
        }
    }

    private System.Collections.IEnumerator ActivateTeleporterAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (teleporter != null)
        {
            teleporter.SetActive(true);
            cam.Priority = 0;
            PlayerController.Instance.SetDisableSprint(false);
            PlayerController.Instance.SetCanMove(true);
            EscapeMenuUI.Instance.EnableToggle();
            BossTriggerAnimator.Instance.isPlayingMoveToPlayerSection = false;
        }
    }
}
