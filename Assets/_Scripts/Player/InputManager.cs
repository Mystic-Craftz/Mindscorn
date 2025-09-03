using UnityEngine;

public class InputManager : MonoBehaviour
{
    private static InputManager instance;

    public static InputManager Instance
    {
        get { return instance; }
    }

    private InputSystem_Actions inputActions;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            instance = this;
        }

        inputActions = new InputSystem_Actions();
        inputActions.Player.Torch.performed += ctx => PlayerWeapons.Instance.ToggleTorch();
        inputActions.Player.Escape.performed += ctx =>
        {

            if (
               ContainerSearchingUI.Instance.IsOpen()
            || ConfirmItemUseUI.Instance.IsOpen()
            || NoteContentUI.Instance.IsOpen()
            || InventoryManager.Instance.IsOpen()
            ) return;

            EscapeMenuUI.Instance.Toggle();
        };
    }

    private void OnEnable()
    {
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }


    //helper functions
    public Vector2 GetPlayerMovement()
    {
        return inputActions.Player.Move.ReadValue<Vector2>();
    }

    public Vector2 GetMouseDelta()
    {
        return inputActions.Player.Look.ReadValue<Vector2>();
    }

    public bool IsPlayerSprinting()
    {
        return inputActions.Player.Sprint.phase == UnityEngine.InputSystem.InputActionPhase.Performed;
    }

    public bool IsPlayerMoving()
    {
        return inputActions.Player.Move.IsInProgress();
    }

    public bool GetPlayerCrouch()
    {
        return inputActions.Player.Crouch.triggered;
    }

    public bool GetPlayerInteract()
    {
        return inputActions.Player.Interact.WasPressedThisFrame();
    }


    public bool GetPlayerShoot()
    {
        return inputActions.Player.Shoot.triggered;
    }

    public bool GetPlayerReload()
    {
        return inputActions.Player.Reload.triggered;
    }

    public bool GetQuickSlot1()
    {
        return inputActions.Player.QuickSlotOne.triggered;
    }
    public bool GetQuickSlot2()
    {
        return inputActions.Player.QuickSlotTwo.triggered;
    }
    public bool GetQuickSlot3()
    {
        return inputActions.Player.QuickSlotThree.triggered;
    }

    public bool GetPlayerAim()
    {
        return inputActions.Player.Aim.IsInProgress();
    }

    public bool GetInventoryOpen()
    {
        return inputActions.Player.Inventory.triggered;
    }

    public bool GetPlayerMelee()
    {
        return inputActions.Player.Melee.triggered;
    }

    public bool GetTorchToggle()
    {
        return inputActions.Player.Torch.triggered;
    }

    public bool GetNavigateRightTriggered()
    {
        return inputActions.UI.Navigate.WasPressedThisFrame() && inputActions.UI.Navigate.ReadValue<Vector2>().x > 0;
    }

    public bool GetNavigateLeftTriggered()
    {
        return inputActions.UI.Navigate.WasPressedThisFrame() && inputActions.UI.Navigate.ReadValue<Vector2>().x < 0;
    }

    public bool GetNavigateUpTriggered()
    {
        return inputActions.UI.Navigate.WasPressedThisFrame() && inputActions.UI.Navigate.ReadValue<Vector2>().y > 0;
    }

    public bool GetNavigateDownTriggered()
    {
        return inputActions.UI.Navigate.WasPressedThisFrame() && inputActions.UI.Navigate.ReadValue<Vector2>().y < 0;
    }

    public bool GetUseItem()
    {
        return inputActions.UI.UseItem.triggered;
    }

    public bool GetCloseTriggered()
    {
        return inputActions.UI.Close.triggered;
    }

    public string GetInputString(KeyOption keyOption)
    {
        switch (keyOption)
        {
            case KeyOption.NavigateUp:
                return GetStringOfSafeLength(inputActions.UI.Navigate.bindings[1].ToDisplayString().ToUpper());
            case KeyOption.NavigateDown:
                return GetStringOfSafeLength(inputActions.UI.Navigate.bindings[3].ToDisplayString().ToUpper());
            case KeyOption.NavigateLeft:
                return GetStringOfSafeLength(inputActions.UI.Navigate.bindings[5].ToDisplayString().ToUpper());
            case KeyOption.NavigateRight:
                return GetStringOfSafeLength(inputActions.UI.Navigate.bindings[7].ToDisplayString().ToUpper());
            case KeyOption.UseItem:
                return GetStringOfSafeLength(inputActions.UI.UseItem.bindings[0].ToDisplayString().ToUpper());
            case KeyOption.Close:
                return GetStringOfSafeLength(inputActions.UI.Close.bindings[0].ToDisplayString().ToUpper());
            case KeyOption.Inventory:
                return GetStringOfSafeLength(inputActions.Player.Inventory.bindings[0].ToDisplayString().ToUpper());
            default:
                return null;
        }
    }

    private string GetStringOfSafeLength(string input, int maxLength = 3)
    {
        if (input.Length > maxLength)
        {
            return input[..maxLength];
        }
        return input;
    }
}
