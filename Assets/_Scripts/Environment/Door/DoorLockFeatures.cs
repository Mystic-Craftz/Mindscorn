using System.Collections;
using FMODUnity;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(SaveableEntity))]
public class DoorLockFeatures : MonoBehaviour, ISaveable
{
    [Header("Settings")]
    [SerializeField] private bool isLockedInitially = false;
    [SerializeField] private string lockMessage = "Door is locked.";
    [SerializeField] private float lockMessageDuration = 0.7f;
    [SerializeField, Tooltip("For testing only")] private bool breakOnStart = false;

    [Tooltip("The ID will only be used if isLocked is true")]
    [SerializeField] private int keyID = 0;

    [Header("Components")]
    [SerializeField] private Rigidbody[] rigidbodies;
    [SerializeField] private HingeJoint[] joint;
    [SerializeField] private BoxCollider[] colliders;

    [Header("Callbacks")]
    [SerializeField] private UnityEvent onUnlock;
    //! To be implemented
    [SerializeField] private UnityEvent onBreak;

    [Header("Breaking Settings")]
    [SerializeField, Range(-1, 1)] private int breakDirection = 1;
    [SerializeField] private int bangsBeforeBreaking = 3;
    [SerializeField] private float pauseBetweenBangs = 1;
    [SerializeField] private float startDelay = 0;

    [Header("Sounds")]
    [SerializeField] private EventReference unlockSound;
    [SerializeField] private EventReference tryToOpenSound;
    [SerializeField] private EventReference bangSound;
    [SerializeField] private EventReference breakSound;

    private const string DOOR_SHAKE = "DoorShake";
    private const string BREAK_FRONT = "BreakFront";
    private const string BREAK_BACK = "BreakBack";
    private const string BROKEN_FRONT = "BrokenOnFloorFront";
    private const string BROKEN_BACK = "BrokenOnFloorBack";

    public bool isLocked = false;
    public bool isBroken = false;
    private bool isInteracting = false;
    private bool hasBeenUnlocked = false;

    private Animator doorAnim;

    private void Start()
    {
        doorAnim = GetComponent<Animator>();

        // if (GetComponent<SaveableEntity>().UniqueId == "RightWingDoor")
        // {
        //     Debug.Log("isLocked: " + isLocked);
        //     Debug.Log("hasBeenUnlocked: " + hasBeenUnlocked);
        // }

        if (!hasBeenUnlocked && !isLocked)
        {
            isLocked = isLockedInitially;

            if (isLockedInitially)
            {
                for (int i = 0; i < rigidbodies.Length; i++)
                {
                    rigidbodies[i].isKinematic = true;
                }
            }
        }

        if (breakOnStart)
        {
            BreakDoor();
        }
    }

    // private void Update()
    // {
    // if (GetComponent<SaveableEntity>().UniqueId == "RightWingDoor")
    // {
    //     Debug.Log("isLocked: " + isLocked);
    //     Debug.Log("hasBeenUnlocked: " + hasBeenUnlocked);
    // }
    // }

    public void PerformInteract()
    {
        if (isLocked && !isInteracting && !isBroken)
        {
            InventoryItem item = InventoryManager.Instance.GetItemByID(keyID);
            if (item != null)
            {
                //? If user has the key
                ConfirmItemUseUI.Instance.Show(
                    item,
                    () =>
                    {
                        isLocked = false;
                        isInteracting = false;
                        for (int i = 0; i < rigidbodies.Length; i++)
                        {
                            rigidbodies[i].isKinematic = false;
                        }
                        BoxCollider collider = GetComponent<BoxCollider>();
                        collider.enabled = false;
                        collider.enabled = true;
                        hasBeenUnlocked = true;
                        ConfirmItemUseUI.Instance.Hide();
                        AudioManager.Instance.PlayOneShot(unlockSound, transform.position);
                        onUnlock?.Invoke();
                    },
                    () =>
                    {
                        ConfirmItemUseUI.Instance.Hide();
                        isInteracting = false;
                    }
                );
                isInteracting = true;
            }
            else
            {
                //? If user doesn't have key for the door
                DialogUI.Instance.ShowDoorDialog(lockMessage, lockMessageDuration);
                doorAnim.CrossFade(DOOR_SHAKE, 0f);
                AudioManager.Instance.PlayOneShot(tryToOpenSound, transform.position);
            }
        }
    }

    public void StartDoorBreakingCoRoutine()
    {
        StartCoroutine(BreakDoorCoRoutine());
    }

    private IEnumerator BreakDoorCoRoutine()
    {
        yield return new WaitForSeconds(startDelay);
        for (int i = 0; i < bangsBeforeBreaking; i++)
        {
            AudioManager.Instance.PlayOneShot(bangSound, transform.position);
            doorAnim.CrossFade(DOOR_SHAKE, 0f);
            yield return new WaitForSeconds(pauseBetweenBangs);
        }
        AudioManager.Instance.PlayOneShot(breakSound, transform.position);
        BreakDoor();
        onBreak?.Invoke();
    }

    private void BreakDoor()
    {
        if (breakDirection == 0) return;
        else if (breakDirection == 1)
        {
            doorAnim.SetLayerWeight(1, 1);
            doorAnim.CrossFade(BREAK_FRONT, 0f, 1);
            isBroken = true;
            isLocked = false;
        }
        else if (breakDirection == -1)
        {
            doorAnim.SetLayerWeight(1, 1);
            doorAnim.CrossFade(BREAK_BACK, 0f, 1);
            isBroken = true;
            isLocked = false;
        }
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }
    }

    public bool ShouldShowInteractionUI()
    {
        return isLocked && !isBroken;
    }

    public string GetUniqueIdentifier()
    {
        return GetComponent<SaveableEntity>().UniqueId;
    }

    public object CaptureState()
    {
        return new SaveData
        {
            isLocked = isLocked,
            isBroken = isBroken,
            hasBeenUnlocked = hasBeenUnlocked
        };
    }

    public void RestoreState(object state)
    {
        string json = state as string;
        SaveData data = JsonUtility.FromJson<SaveData>(json);
        isLocked = data.isLocked;
        hasBeenUnlocked = data.hasBeenUnlocked;

        // if (GetComponent<SaveableEntity>().UniqueId == "RightWingDoor")
        // {
        //     Debug.Log("OnLoad.isLocked: " + data.isLocked);
        //     Debug.Log("OnLoad.hasBeenUnlocked: " + data.hasBeenUnlocked);
        // }

        if (isLocked)
        {
            for (int i = 0; i < rigidbodies.Length; i++)
            {
                rigidbodies[i].isKinematic = true;
            }
        }
        else
        {
            for (int i = 0; i < rigidbodies.Length; i++)
            {
                rigidbodies[i].isKinematic = false;
            }
        }

        isBroken = data.isBroken;

        if (isBroken)
        {
            doorAnim = GetComponent<Animator>();
            doorAnim.SetLayerWeight(1, 1);
            if (breakDirection == 1)
            {
                doorAnim.CrossFade(BROKEN_FRONT, 0f, 1);
            }
            else if (breakDirection == -1)
            {
                doorAnim.CrossFade(BROKEN_BACK, 0f, 1);
            }
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }
        }
    }

    public void Lock()
    {
        if (isBroken)
        {
            Debug.LogWarning($"Lock() called on broken door '{name}' — cannot lock a broken door.");
            return;
        }

        isLocked = true;
        hasBeenUnlocked = false;
        // Make physics static/kinematic while locked
        if (rigidbodies != null)
        {
            for (int i = 0; i < rigidbodies.Length; i++)
                if (rigidbodies[i] != null) rigidbodies[i].isKinematic = true;
        }
        // refresh collider to ensure correct collision state (matches your other code)
        var col = GetComponent<BoxCollider>();
        if (col != null)
        {
            col.enabled = false;
            col.enabled = true;
        }
    }

    public void Unlock()
    {
        isLocked = false;
        hasBeenUnlocked = true;
        if (rigidbodies != null)
        {
            for (int i = 0; i < rigidbodies.Length; i++)
                if (rigidbodies[i] != null) rigidbodies[i].isKinematic = false;
        }
        var col = GetComponent<BoxCollider>();
        if (col != null)
        {
            col.enabled = false;
            col.enabled = true;
        }
    }


    class SaveData
    {
        public bool isLocked;
        public bool isBroken;
        public bool hasBeenUnlocked;
    }
}
