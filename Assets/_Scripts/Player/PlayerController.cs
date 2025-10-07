using System;
using DG.Tweening;
using FMOD.Studio;
using FMODUnity;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Animations.Rigging;

[RequireComponent(typeof(SaveableEntity))]
public class PlayerController : MonoBehaviour, ISaveable
{
    public static PlayerController Instance { get; private set; }

    [Header("References")]

    [Header("Player Settings")]
    [SerializeField] private float playerSpeed = 2.0f;
    [SerializeField] private float sprintSpeed = 5.0f;
    [SerializeField] private float rotationSmoothing = 0.1f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float groundedOffset = -0.1f;
    [SerializeField] private float groundedCheckRadius = 0.3f;
    [SerializeField] private Transform eyes;
    [SerializeField] private CinemachineInputAxisController cinemachineFPSCamController;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private EventReference playerWalkSound;
    [SerializeField] private EventReference playerSprintSound;

    [Header("Hands Rig")]
    [SerializeField] private Rig leftHandRig;
    [SerializeField] private Rig rightHandRig;

    [Header("Debugging")]
    [SerializeField] private bool testBool = false;

    private InputManager inputManager;
    private CharacterController controller;
    private Transform cameraTransform;
    private Animator animator;
    private Vector3 move;
    private PlayerWeapons playerWeapons;
    private float currentSpeed;
    private float verticalVelocity;
    private bool isGrounded;
    private bool canMove = true;
    private Vector3 lastPosition;
    private float minMovementThreshold = 0.05f;
    private EventInstance playerWalk;
    private EventInstance playerSprint;

    private bool isFirstForWalkSound = true;

    private bool disableSprint = false;

    [HideInInspector] public bool isMoving;
    [HideInInspector] public bool isSprinting;


    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        inputManager = InputManager.Instance;
        cameraTransform = Camera.main.transform;
        animator = PlayerAnimations.Instance.GetAnimator();
        playerWeapons = PlayerWeapons.Instance;

        playerWalk = AudioManager.Instance.CreateInstance(playerWalkSound);
        playerSprint = AudioManager.Instance.CreateInstance(playerSprintSound);

        //* Setting sens here
        float sens = PlayerPrefs.GetFloat("mouseSens", .5f);
        cinemachineFPSCamController.Controllers.ForEach(controller =>
        {
            if (controller.Name == "Look X (Pan)")
            {
                controller.Input.Gain = sens;
            }
            else if (controller.Name == "Look Y (Tilt)")
            {
                controller.Input.Gain = -sens;
            }
        });

        //TODO: Uncomment this later
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        // Debug.unityLogger.logEnabled = false;
    }

    private void Update()
    {
        // Cursor.lockState = CursorLockMode.None;
        // Cursor.visible = true;
        if (!canMove) return;
        GroundCheck();
        CalculateMovement();
        ApplyGravity();
        PerformMovement();
    }

    private void GroundCheck()
    {
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y + groundedOffset, transform.position.z);
        isGrounded = Physics.CheckSphere(spherePosition, groundedCheckRadius, groundLayer, QueryTriggerInteraction.Ignore);

        if (isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }
    }

    private void CalculateMovement()
    {
        Vector2 input = inputManager.GetPlayerMovement();
        isMoving = input.sqrMagnitude > 0.01f;
        isSprinting = inputManager.IsPlayerSprinting() && input.y > 0 && input.x == 0;
        if (disableSprint) isSprinting = false;
        currentSpeed = isSprinting ? sprintSpeed : playerSpeed;

        move = cameraTransform.forward * input.y + cameraTransform.right * input.x;
        move.y = 0f;
    }

    private void ApplyGravity()
    {
        verticalVelocity += gravity * Time.deltaTime;
    }

    private void PerformMovement()
    {
        if (move != Vector3.zero)
            move = move.normalized;

        Quaternion targetRotation = Quaternion.Euler(0f, cameraTransform.eulerAngles.y, 0f);
        float rotationStep = rotationSmoothing * Time.deltaTime * 10f;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationStep);

        Vector3 totalMovement = move * currentSpeed + Vector3.up * verticalVelocity;
        controller.Move(totalMovement * Time.deltaTime);

        Vector3 displacement = transform.position - lastPosition;
        float distanceMoved = displacement.magnitude / Time.deltaTime;

        if (isFirstForWalkSound)
        {
            distanceMoved = 0f;
            isFirstForWalkSound = false;
        }
        bool isActuallyMoving = distanceMoved > minMovementThreshold;

        UpdateSounds(isActuallyMoving);

        animator.SetBool(PlayerConstants.IS_WALKING, isMoving);
        animator.SetBool(PlayerConstants.IS_SPRINTING, isSprinting && isMoving);

        lastPosition = transform.position;
    }

    private void UpdateSounds(bool isActuallyMoving)
    {
        PLAYBACK_STATE walkPlaybackState;
        playerWalk.getPlaybackState(out walkPlaybackState);

        PLAYBACK_STATE sprintPlaybackState;
        playerSprint.getPlaybackState(out sprintPlaybackState);

        if (isActuallyMoving && isGrounded)
        {
            if (isSprinting)
            {
                if (sprintPlaybackState == PLAYBACK_STATE.STOPPED)
                    playerSprint.start();
                playerWalk.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            }
            else
            {
                if (walkPlaybackState == PLAYBACK_STATE.STOPPED)
                    playerWalk.start();
                playerSprint.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            }
        }
        else
        {
            playerWalk.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            playerSprint.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize ground check sphere in the editor
        if (transform != null)
        {
            Gizmos.color = Color.yellow;
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y + groundedOffset, transform.position.z);
            Gizmos.DrawWireSphere(spherePosition, groundedCheckRadius);
        }
    }

    public void SetCanMove(bool value)
    {
        canMove = value;
        cinemachineFPSCamController.enabled = value;
        if (!value)
        {
            controller.Move(Vector3.zero);
            animator.SetBool(PlayerConstants.IS_WALKING, false);
            animator.SetBool(PlayerConstants.IS_SPRINTING, false);
            playerWalk.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            playerSprint.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        }
    }

    public bool GetCanMove() => canMove;

    public void SetDisableSprint(bool value) => disableSprint = value;

    public void OpenDoorAnimation(Transform doorObj)
    {

        Vector3 directionToDoor = (transform.position - doorObj.position).normalized;
        float dotProduct = Vector3.Dot(transform.forward, directionToDoor);

        PlayerWeapons.Weapons currentWeaponType = PlayerWeapons.Instance.GetCurrectWeaponType();

        if (dotProduct < -0.85 && !isSprinting)
        {
            switch (currentWeaponType)
            {
                case PlayerWeapons.Weapons.Revolver:
                    DOTween.To(() => leftHandRig.weight, x =>
                {
                    leftHandRig.weight = x;
                    animator.SetLayerWeight(2, x);
                }, 1f, 0.2f).OnComplete(() =>
               {
                   DOTween.To(() => leftHandRig.weight, x =>
                   {
                       leftHandRig.weight = x;
                       animator.SetLayerWeight(2, x);
                   }, 0f, 0.4f).SetDelay(1 / 60 * 3);
               });
                    break;
                case PlayerWeapons.Weapons.Shotgun:
                    DOTween.To(() => rightHandRig.weight, x =>
                    {
                        rightHandRig.weight = x;
                        animator.SetLayerWeight(3, x);
                    }, 1f, 0.2f).OnComplete(() =>
                   {
                       DOTween.To(() => rightHandRig.weight, x =>
                       {
                           rightHandRig.weight = x;
                           animator.SetLayerWeight(3, x);
                       }, 0f, 0.4f).SetDelay(1 / 60 * 3);
                   });
                    break;
                case PlayerWeapons.Weapons.Rifle:
                    DOTween.To(() => rightHandRig.weight, x =>
                                        {
                                            rightHandRig.weight = x;
                                            animator.SetLayerWeight(3, x);
                                        }, 1f, 0.2f).OnComplete(() =>
                                       {
                                           DOTween.To(() => rightHandRig.weight, x =>
                                           {
                                               rightHandRig.weight = x;
                                               animator.SetLayerWeight(3, x);
                                           }, 0f, 0.4f).SetDelay(1 / 60 * 3);
                                       });
                    break;
                case PlayerWeapons.Weapons.None:
                default:
                    break;
            }
        }
    }

    public Transform GetEyes()
    {
        return cinemachineFPSCamController.transform;
    }

    public void SetSensitivity(float sens)
    {
        cinemachineFPSCamController.Controllers.ForEach(controller =>
        {
            if (controller.Name == "Look X (Pan)")
            {
                controller.Input.Gain = sens;
            }
            else if (controller.Name == "Look Y (Tilt)")
            {
                controller.Input.Gain = -sens;
            }
        });
    }

    public string GetUniqueIdentifier()
    {
        return GetComponent<SaveableEntity>().UniqueId;
    }

    public object CaptureState()
    {
        return new SaveData
        {
            x = transform.position.x,
            y = transform.position.y,
            z = transform.position.z,
            rotY = transform.rotation.eulerAngles.y
        };
    }

    public void RestoreState(object state)
    {
        string json = state as string;
        SaveData data = JsonUtility.FromJson<SaveData>(json);
        transform.SetPositionAndRotation(new Vector3(data.x, data.y, data.z), Quaternion.Euler(0f, data.rotY, 0f));
    }

    [Serializable]
    class SaveData
    {
        public float x;
        public float y;
        public float z;
        public float rotY;
    }
}