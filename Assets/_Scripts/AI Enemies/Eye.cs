using UnityEngine;

public class Eye : MonoBehaviour
{
    [SerializeField] private bool shouldStartLookingAtStart = false;
    private Transform cameraTransform;

    private bool shouldLook = false;

    private void Start()
    {
        cameraTransform = Camera.main.transform;

        if (shouldStartLookingAtStart)
        {
            shouldLook = true;
            transform.LookAt(cameraTransform.position);
        }
    }

    private void FixedUpdate()
    {
        if (shouldLook)
        {
            transform.LookAt(cameraTransform.position);
        }
    }

    public void StartLookingAtPlayer()
    {
        shouldLook = true;
    }
}
