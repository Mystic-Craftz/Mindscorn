using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    public static PlayerInteraction Instance { get; private set; }
    [SerializeField] private float interactRange = 3f;
    [SerializeField] private LayerMask interactableLayer;

    private Camera playerCamera;
    private Ray lastRay;
    private RaycastHit lastHit;
    private IAmInteractable lastLookedAtItem = null;
    private InteractionUI interactionUI;

    private bool disabled = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        playerCamera = Camera.main;
        interactionUI = InteractionUI.Instance;
        if (playerCamera == null)
        {
            Debug.LogError("No Camera found! Ensure your main camera is tagged as 'MainCamera'.");
        }
    }

    private void Update()
    {
        if (disabled) return;
        LookingForInteractableObject();
        ItemInteract();
    }

    private void LookingForInteractableObject()
    {
        lastRay = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        Debug.DrawRay(lastRay.origin, lastRay.direction * interactRange, Color.red);

        if (Physics.Raycast(lastRay, out lastHit, interactRange, interactableLayer))
        {
            if (lastHit.collider.TryGetComponent<IAmInteractable>(out var item))
            {
                if (item.ShouldShowInteractionUI()) interactionUI.Show();
                else interactionUI.Hide();
                lastLookedAtItem = item;
            }
            else
            {
                interactionUI.Hide();
                lastLookedAtItem = null;
            }
        }
        else
        {
            interactionUI.Hide();
            lastLookedAtItem = null;
        }
    }

    private void ItemInteract()
    {

        if (InputManager.Instance.GetPlayerInteract())
        {
            if (lastLookedAtItem != null)
                lastLookedAtItem.Interact();
        }
    }

    public void SetDisabled(bool value)
    {
        disabled = value;
    }
}
