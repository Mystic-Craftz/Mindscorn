using UnityEngine;

public class WeaponBulletTrail : MonoBehaviour
{
    [SerializeField] private float trailTime = 0.5f;
    [SerializeField] private float textureScrollSpeed = 0.5f;
    private LineRenderer lineRenderer;

    private void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    private void Update()
    {
        if (lineRenderer != null)
        {
            Material mat = lineRenderer.material;
            mat.color = Color.Lerp(new Color(mat.color.r, mat.color.g, mat.color.b, mat.color.a), new Color(mat.color.r, mat.color.g, mat.color.b, 0), Time.deltaTime * trailTime);

            mat.mainTextureOffset = new Vector2(mat.mainTextureOffset.x - Time.deltaTime * textureScrollSpeed, 0);

            if (mat.color.a <= 0.01)
            {
                Destroy(gameObject);
            }
        }
    }
}
