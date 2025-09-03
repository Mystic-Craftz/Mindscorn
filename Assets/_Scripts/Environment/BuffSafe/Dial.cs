using System;
using FMODUnity;
using UnityEngine;

public class Dial : MonoBehaviour
{
    [Serializable]
    public class DialValues
    {
        public float angle;
        public int value;
    }

    [SerializeField] private DialValues[] dialValues;
    [SerializeField] private BuffSafe parentBuffSafe;
    [SerializeField] private float maxGlow = 5f;
    [SerializeField] private float glowSpeed = 1f;
    [SerializeField] private EventReference scrollSound;

    public int currentValue = -1;

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
        if (!isSelected || !parentBuffSafe.isInteracting) return;
        Glow();
        if (inputManager.GetNavigateUpTriggered()) RotateUp();
        if (inputManager.GetNavigateDownTriggered()) RotateDown();
    }

    private void FindFirstIndex()
    {
        for (int i = 0; i < dialValues.Length; i++)
        {
            if (dialValues[i].angle == transform.eulerAngles.x)
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
        transform.localRotation = Quaternion.Euler(dialValues[currentValueIndex].angle, 0, 0);
    }

    public void SetSelectedDial(bool isSelected)
    {
        this.isSelected = isSelected;
        if (isSelected) { mat.EnableKeyword("_EMISSION"); }
        else { mat.DisableKeyword("_EMISSION"); }
    }

    public void EndInteraction()
    {
        mat.DisableKeyword("_EMISSION");
    }

    public int GetValue() => currentValue;
}
