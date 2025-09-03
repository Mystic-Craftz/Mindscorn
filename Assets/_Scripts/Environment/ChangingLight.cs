using UnityEngine;

public class ChangingLight : MonoBehaviour
{
    //* Just lantern light that dims and goes brighter.

    [SerializeField]
    private float minLightIntensity = 0.25f;

    [SerializeField]
    private float maxLightIntensity = 0.7f;

    [SerializeField]
    private float lerpSpeed;

    private float interval = 0.5f;

    private float currentIntensity = 0.25f;

    private float timeSinceLastChange = 0.0f;

    private Light pointLight;

    private void Start()
    {
        pointLight = GetComponent<Light>();
        interval = Random.Range(0.5f, 1.5f);
    }

    private void FixedUpdate()
    {
        timeSinceLastChange += Time.fixedDeltaTime;
        if (timeSinceLastChange >= interval)
        {
            currentIntensity = Random.Range(minLightIntensity, maxLightIntensity);
            timeSinceLastChange = 0.0f;
            interval = Random.Range(0.5f, 1.5f);
        }

        pointLight.intensity = Mathf.Lerp(pointLight.intensity, currentIntensity, lerpSpeed * Time.fixedDeltaTime);
    }
}
