using UnityEngine;

public class HandSway : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private Transform cameraTransform;

    [SerializeField]
    private Transform fpsCameraTransform;

    [SerializeField]
    private Transform meshTransform;

    [SerializeField]
    private float movementRotation = 3f;

    [SerializeField]
    private float movementRotationSmoothing = 3f;

    private Quaternion originalMeshRotation;
    private Vector3 originalEulerAngles;

    private void Start()
    {
        originalMeshRotation = Quaternion.Euler(Vector3.zero);
        originalEulerAngles = Vector3.zero;
    }

    private void LateUpdate()
    {
        if (PlayerController.Instance.GetCanMove() == false) return;

        Vector2 movement = InputManager.Instance.GetPlayerMovement();

        transform.localEulerAngles = new Vector3(fpsCameraTransform.localEulerAngles.x, 0, 0);

        // Apply movement-based rotation
        if (Mathf.Approximately(movement.x, 0))
        {
            // Smoothly return to the original rotation when there's no movement
            meshTransform.localRotation = Quaternion.Slerp(meshTransform.localRotation, originalMeshRotation, Time.deltaTime * movementRotationSmoothing);
        }
        else
        {
            // Apply rotation based on movement
            float targetZRotation = originalEulerAngles.z - movement.x * movementRotation;
            Quaternion movementRotationTarget = Quaternion.Euler(originalEulerAngles.x, originalEulerAngles.y, targetZRotation);
            meshTransform.localRotation = Quaternion.Slerp(meshTransform.localRotation, movementRotationTarget, Time.deltaTime * movementRotationSmoothing);
        }
    }
}