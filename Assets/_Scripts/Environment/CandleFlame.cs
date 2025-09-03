using System.Collections.Generic;
using UnityEngine;

public class CandleFlame : MonoBehaviour
{
    [SerializeField] private List<Sprite> flameLists;
    [SerializeField] private float distanceFromCameraToStartAnimatingAt;
    private SpriteRenderer spriteRenderer;
    private int index = 0;
    private Transform cameraTransform;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = flameLists[0];
        index = 0;
        cameraTransform = Camera.main.transform;
    }

    private void FixedUpdate()
    {
        if (Vector3.Distance(transform.position, cameraTransform.position) < distanceFromCameraToStartAnimatingAt)
        {
            spriteRenderer.sprite = flameLists[index];
            index = (index + 1) % flameLists.Count;
        }
        transform.LookAt(cameraTransform.position);

    }
}
