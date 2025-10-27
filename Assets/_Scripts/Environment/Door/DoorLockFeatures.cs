using System.Collections;
using DG.Tweening;
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
    [SerializeField] private BoxCollider[] colliders;
    [SerializeField] private Transform doorHandle;

    [Header("Callbacks")]
    [SerializeField] private UnityEvent onUnlock;
    [SerializeField] private UnityEvent onFirstTimeDoorOpen;
    [SerializeField] private UnityEvent onLoad;
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
    private bool firstTimeOpenEventTriggered = false;

    private Animator doorAnim;

    private void Start()
    {
        doorAnim = GetComponent<Animator>();

        if (!hasBeenUnlocked && !isLocked)
        {
            isLocked = isLockedInitially;
        }

        if (breakOnStart)
        {
            BreakDoor();
        }
    }

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
                doorAnim.enabled = false;
                doorHandle.DOLocalRotate(new Vector3(0, 45f, 0), .2f).OnComplete(() =>
                {
                    doorHandle.DOLocalRotate(new Vector3(0, 0, 0), .25f).OnComplete(() => doorAnim.enabled = true);
                });
                AudioManager.Instance.PlayOneShot(tryToOpenSound, transform.position);
            }
        }
    }

    public void DoorOpened()
    {
        if (!firstTimeOpenEventTriggered)
        {
            firstTimeOpenEventTriggered = true;
            onFirstTimeDoorOpen?.Invoke();
        }
    }

    public void StartDoorBreakingCoRoutine()
    {
        StartCoroutine(BreakDoorCoRoutine());
    }

    private IEnumerator BreakDoorCoRoutine()
    {
        yield return new WaitForSeconds(startDelay);
        doorAnim.enabled = false;
        for (int i = 0; i < bangsBeforeBreaking; i++)
        {
            AudioManager.Instance.PlayOneShot(bangSound, transform.position);
            // doorAnim.CrossFade(DOOR_SHAKE, 0f);
            colliders[0].gameObject.transform.DOLocalRotate(new Vector3(colliders[0].gameObject.transform.localEulerAngles.x, 0, 4 * breakDirection), 0.1f);
            yield return new WaitForSeconds(0.1f);
            colliders[0].gameObject.transform.DOLocalRotate(new Vector3(colliders[0].gameObject.transform.localEulerAngles.x, 0, 0), 0.1f);

            yield return new WaitForSeconds(pauseBetweenBangs);
        }
        doorAnim.enabled = true;
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
            keyID = keyID,
            hasBeenUnlocked = hasBeenUnlocked,
            firstTimeOpenEventTriggered = firstTimeOpenEventTriggered
        };
    }

    public void RestoreState(object state)
    {
        string json = state as string;
        SaveData data = JsonUtility.FromJson<SaveData>(json);
        isLocked = data.isLocked;
        keyID = data.keyID;
        hasBeenUnlocked = data.hasBeenUnlocked;
        firstTimeOpenEventTriggered = data.firstTimeOpenEventTriggered;

        // if (GetComponent<SaveableEntity>().UniqueId == "RightWingDoor")
        // {
        //     Debug.Log("OnLoad.isLocked: " + data.isLocked);
        //     Debug.Log("OnLoad.hasBeenUnlocked: " + data.hasBeenUnlocked);
        // }

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

        onLoad?.Invoke();
    }

    public void ChangeLockKey(int newId)
    {
        keyID = newId;
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
        // refresh collider to ensure correct collision state (matches your other code)
        var col = GetComponent<BoxCollider>();
        if (col != null)
        {
            col.enabled = false;
            col.enabled = true;
        }
        colliders[0].gameObject.transform.DOLocalRotate(new Vector3(colliders[0].gameObject.transform.localEulerAngles.x, 0, 0), 0f);
        if (colliders.Length > 1)
        {
            colliders[1].gameObject.transform.DOLocalRotate(new Vector3(colliders[1].gameObject.transform.localEulerAngles.x, 0, 0), 0f);
        }
    }

    public void Unlock()
    {
        isLocked = false;
        hasBeenUnlocked = true;
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

        public int keyID;
        public bool hasBeenUnlocked;
        public bool firstTimeOpenEventTriggered;
    }
}
