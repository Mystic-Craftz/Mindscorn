using FMODUnity;
using UnityEngine;

public class DoorAutoClose : MonoBehaviour
{
    [SerializeField] private HingeJoint joint;
    [SerializeField] private BoxCollider box;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private float disableColliderAngle = 80;
    [SerializeField] private float rotationSpeed = 2;
    [SerializeField] private Transform doorFrame;
    [SerializeField] private EventReference openSound;
    [SerializeField] private EventReference closeSound;
    [SerializeField] private bool shouldPlayerOpenThisDoor = false;

    private const string HANDLE_OPEN = "HandleOpen";

    private bool isPlayerEntered = false;

    private Animator doorAnim;
    private DoorLockFeatures lockFeatures;
    private BoxCollider gameObjectCollider;

    private void Start()
    {
        doorAnim = GetComponent<Animator>();
        lockFeatures = GetComponent<DoorLockFeatures>();
        gameObjectCollider = GetComponent<BoxCollider>();
    }

    private void Update()
    {
        if ((joint.angle > disableColliderAngle || joint.angle < -disableColliderAngle) && isPlayerEntered)
        {
            joint.useSpring = true;
            JointSpring spring = joint.spring;
            spring.spring = rotationSpeed;
            spring.targetPosition = 90 * (joint.angle > 0 ? 1 : -1);
            joint.spring = spring;
            rb.isKinematic = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if ((other.CompareTag("Player Door Collider") || other.CompareTag("Enemy")) && !lockFeatures.isLocked && !lockFeatures.isBroken)
        {
            isPlayerEntered = true;
            JointSpring spring = joint.spring;
            spring.spring = rotationSpeed;
            Vector3 doorRight = joint.transform.up;
            Vector3 doorToPlayer = other.transform.position - joint.transform.position;

            float side = Vector3.Dot(doorRight, doorToPlayer);

            if (side > 0)
            {
                spring.targetPosition = 90;
            }
            else
            {
                spring.targetPosition = -90;
            }

            joint.spring = spring;

            AudioManager.Instance.PlayOneShot(openSound, transform.position);
            doorAnim.CrossFade(HANDLE_OPEN, 0f);
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
            rb.isKinematic = false;
            isPlayerEntered = false;
            joint.useSpring = true;
            JointSpring spring = joint.spring;
            spring.spring = rotationSpeed;
            gameObjectCollider.size = new Vector3(gameObjectCollider.size.x, gameObjectCollider.size.y, 0.62f);
            if (spring.targetPosition != 0)
                AudioManager.Instance.PlayOneShot(closeSound, transform.position);
            spring.targetPosition = 0;
            joint.spring = spring;
        }
    }
}
