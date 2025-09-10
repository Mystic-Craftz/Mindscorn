using System;
using FMODUnity;
using UnityEngine;

public class RoundDial : MonoBehaviour
{
    [Serializable]
    public class DialValues
    {
        public float angle;
        public int value;
    }

    [SerializeField] private DialValues[] dialValues;
    [SerializeField] private MazeSafe parentSafe;
    [SerializeField] private float maxGlow = 5f;
    [SerializeField] private float glowSpeed = 1f;
    [SerializeField] private EventReference scrollSound;

    public int currentValue = 0;

    private bool isSelected = false;

    private int currentValueIndex = -1;
    InputManager inputManager;
    Renderer rend;
    Material mat;

    private void Start()
    {
        inputManager = InputManager.Instance;
        FindFirstIndex();
        UpdateDial();
        rend = GetComponent<Renderer>();
        mat = rend.material;
    }

    private void Update()
    {
        if (!isSelected || !parentSafe.isInteracting) return;
        Glow();
        if (inputManager.GetNavigateLeftTriggered()) RotateUp();
        if (inputManager.GetNavigateRightTriggered()) RotateDown();
    }

    private void FindFirstIndex()
    {
        for (int i = 0; i < dialValues.Length; i++)
        {
            if (dialValues[i].angle == transform.localEulerAngles.z)
            {
                currentValueIndex = i;
                currentValue = dialValues[i].value;
                break;
            }
        }

        if (currentValueIndex == -1)
        {
            currentValueIndex = 0;
            currentValue = dialValues[currentValueIndex].value;
        }
    }

    private void Glow()
    {
        Color baseColor = mat.GetColor("_EmissionColor");
        Color emissionColor = baseColor;

        emissionColor.r = Mathf.PingPong(Time.time * glowSpeed, maxGlow);
        emissionColor.g = Mathf.PingPong(Time.time * glowSpeed, maxGlow);
        emissionColor.b = Mathf.PingPong(Time.time * glowSpeed, maxGlow);

        mat.SetColor("_EmissionColor", emissionColor);
    }

    public void RotateUp()
    {
        currentValueIndex = (currentValueIndex + dialValues.Length - 1) % dialValues.Length;
        AudioManager.Instance.PlayOneShot(scrollSound, transform.position);
        UpdateDial();
    }

    public void RotateDown()
    {
        currentValueIndex = (currentValueIndex + 1) % dialValues.Length;
        AudioManager.Instance.PlayOneShot(scrollSound, transform.position);
        UpdateDial();
    }

    private void UpdateDial()
    {
        currentValue = dialValues[currentValueIndex].value;
        transform.localRotation = Quaternion.Euler(transform.localEulerAngles.x, 0, dialValues[currentValueIndex].angle);
    }

    public void SetSelectedDial(bool isSelected)
    {
        if (!gameObject.activeSelf || mat == null) return;
        this.isSelected = isSelected;
        if (isSelected) { mat.EnableKeyword("_EMISSION"); }
        else { mat.DisableKeyword("_EMISSION"); }
    }

    public void EndInteraction()
    {
        if (gameObject.activeSelf)
            mat.DisableKeyword("_EMISSION");
    }

    public int GetValue() => currentValue;
}
