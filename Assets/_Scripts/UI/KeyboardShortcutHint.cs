using TMPro;
using UnityEngine;

public enum KeyOption
{
    NavigateUp,
    NavigateDown,
    NavigateLeft,
    NavigateRight,
    UseItem,
    Close,
    Inventory
}

public class KeyboardShortcutHint : MonoBehaviour
{
    [SerializeField] private KeyOption keyOption;
    [SerializeField] private string whatItDoes = "Default";
    [SerializeField] private TextMeshProUGUI keyText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    private void Start()
    {
        keyText.text = InputManager.Instance?.GetInputString(keyOption);
        descriptionText.text = whatItDoes;
    }
}
