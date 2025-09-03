using UnityEngine;

public class GeneratorConnectors : MonoBehaviour
{
    public enum ConnectorType
    {
        Light,
        Material,
        Fan,
        None
    }

    [SerializeField] private ConnectorType connectorType = ConnectorType.None;
    [SerializeField] private float fanSpeed = 10f;
    [SerializeField] private float distanceFromCameraToStartAnimatingAt = 30f;
    private Transform cameraTransform;


    private void Start()
    {
        cameraTransform = Camera.main.transform;
        switch (connectorType)
        {
            case ConnectorType.Light:
                Generator.Instance.RegisterLight(GetComponent<Light>());
                break;
            case ConnectorType.Material:
                Renderer renderer = GetComponent<Renderer>();
                int materialIndex = -1;
                for (int i = 0; i < renderer.materials.Length; i++)
                {
                    if (renderer.materials[i].name.ToLower().Contains("light")) materialIndex = i;
                }
                Generator.Instance.RegisterRenderer(renderer, materialIndex);
                break;
            case ConnectorType.Fan:
                break;
        }
    }

    private void FixedUpdate()
    {
        if (Generator.Instance == null) { return; }
        switch (connectorType)
        {
            case ConnectorType.Fan:
                if (Generator.Instance.IsTurnedOn() && Vector3.Distance(transform.position, cameraTransform.position) < distanceFromCameraToStartAnimatingAt)
                    transform.Rotate(fanSpeed * Time.fixedDeltaTime * Vector3.up, Space.World);
                break;
        }
    }
}
