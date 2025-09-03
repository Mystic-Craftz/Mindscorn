using UnityEngine;

public class InteractableItemGlow : MonoBehaviour
{
    [SerializeField] private Renderer renderer;
    [SerializeField] private float maxGlow = 5f;
    [SerializeField] private float glowSpeed = 1f;

    private Material mat;

    private void Start()
    {
        mat = renderer.material;
        mat.EnableKeyword("_EMISSION");
    }

    private void Update()
    {
        Color baseColor = mat.GetColor("_EmissionColor");
        Color emissionColor = baseColor;

        emissionColor.r = Mathf.PingPong(Time.time * glowSpeed, maxGlow);
        emissionColor.g = Mathf.PingPong(Time.time * glowSpeed, maxGlow);
        emissionColor.b = Mathf.PingPong(Time.time * glowSpeed, maxGlow);

        mat.SetColor("_EmissionColor", emissionColor);

    }
}
