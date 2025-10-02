using DG.Tweening;
using FMODUnity;
using UnityEngine;

public class DoorAutoClose : MonoBehaviour
{
    [SerializeField] private GameObject doorMesh;
    [SerializeField]
    private GameObject doorHandle;
    [SerializeField] private float walkOpeningDuration = 0.25f;
    [SerializeField] private float sprintOpeningDuration = 0.5f;
    [SerializeField] private float closingDuration = 0.45f;
    [SerializeField] private int axisInversion = 1;
    [SerializeField] private Transform doorFrame;
    [SerializeField] private EventReference openSound;
    [SerializeField] private EventReference closeSound;
    [SerializeField] private bool shouldPlayerOpenThisDoor = false;

    public bool debug = false;

    private const string HANDLE_OPEN = "HandleOpen";

    private bool isPlayerEntered = false;

    private Animator doorAnim;
    private DoorLockFeatures lockFeatures;
    private BoxCollider gameObjectCollider;

    private void Start()
    {
        if (debug)
        {
            Debug.Log(doorMesh.transform.eulerAngles);
        }
        doorAnim = GetComponent<Animator>();
        lockFeatures = GetComponent<DoorLockFeatures>();
        gameObjectCollider = GetComponent<BoxCollider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if ((other.CompareTag("Player Door Collider") || other.CompareTag("Enemy")) && !lockFeatures.isLocked && !lockFeatures.isBroken)
        {
            isPlayerEntered = true;
            Vector3 doorRight = doorMesh.transform.up;
            Vector3 doorToPlayer = other.transform.position - doorMesh.transform.position;

            float side = Vector3.Dot(doorRight, doorToPlayer);

            float openingDuration = walkOpeningDuration;

            if ((PlayerController.Instance.isSprinting && other.CompareTag("Player Door Collider")) || other.CompareTag("Enemy"))
            {
                openingDuration = sprintOpeningDuration;
            }

            if (side > 0)
            {
                doorMesh.transform.DOLocalRotate(new Vector3(doorMesh.transform.localEulerAngles.x, 0, 90 * axisInversion), openingDuration);
            }
            else
            {
                doorMesh.transform.DOLocalRotate(new Vector3(doorMesh.transform.localEulerAngles.x, 0, -90 * axisInversion), openingDuration);

            }
            AudioManager.Instance.PlayOneShot(openSound, transform.position);
            doorHandle.transform.DOLocalRotate(new Vector3(0, 45f, 0), .2f).OnComplete(() => doorHandle.transform.DOLocalRotate(new Vector3(0, 0, 0), .5f));
            gameObjectCollider.size = new Vector3(gameObjectCollider.size.x, gameObjectCollider.size.y, 2.43f);

            if (other.CompareTag("Player Door Collider") && shouldPlayerOpenThisDoor)
            {
                PlayerController.Instance.OpenDoorAnimation(doorFrame);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if ((other.CompareTag("Player Door Collider") || other.CompareTag("Enemy")) && !lockFeatures.isLocked && !lockFeatures.isBroken)
        {
            isPlayerEntered = false;
            gameObjectCollider.size = new Vector3(gameObjectCollider.size.x, gameObjectCollider.size.y, 0.62f);
            AudioManager.Instance.PlayOneShot(closeSound, transform.position);
            doorMesh.transform.DOLocalRotate(new Vector3(doorMesh.transform.localEulerAngles.x, 0, 0), closingDuration);
        }
    }
}
