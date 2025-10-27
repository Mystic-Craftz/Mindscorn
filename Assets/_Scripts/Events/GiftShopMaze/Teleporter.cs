using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;

public class Teleporter : MonoBehaviour
{
    [SerializeField] private Transform destination;
    [SerializeField] private CinemachinePanTilt cinemachinePanTilt;
    [SerializeField] private UnityEvent onTeleport;

    [Header("Teleport settings")]
    [SerializeField] private bool teleportOnFlicker = false;
    [SerializeField] private bool teleportAtCenter = false;


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            CharacterController controller = other.GetComponent<CharacterController>();
            Transform player = controller.transform;
            if (controller != null)
            {
                if (teleportOnFlicker)
                {
                    PlayerWeapons.Instance.TorchFlicker(onDarkness: () =>
                    {
                        controller.enabled = false;
                        float beforeYaw = player.eulerAngles.y;

                        Vector3 localOffset = transform.InverseTransformPoint(other.transform.position);
                        other.transform.position = destination.TransformPoint(localOffset);

                        Vector3 relativeDir = transform.InverseTransformDirection(other.transform.forward);
                        Vector3 newDir = destination.TransformDirection(relativeDir);

                        other.transform.rotation = Quaternion.LookRotation(newDir, destination.up);

                        float deltaYaw = Mathf.DeltaAngle(beforeYaw, player.eulerAngles.y);
                        cinemachinePanTilt.PanAxis.Value += deltaYaw;

                        controller.enabled = true;
                        onTeleport?.Invoke();
                    });
                }
                else
                {
                    controller.enabled = false;
                    float beforeYaw = player.eulerAngles.y;

                    if (teleportAtCenter)
                    {
                        other.transform.position = destination.position;
                    }
                    else
                    {
                        Vector3 localOffset = transform.InverseTransformPoint(other.transform.position);
                        other.transform.position = destination.TransformPoint(localOffset);
                    }

                    Vector3 relativeDir = transform.InverseTransformDirection(other.transform.forward);
                    Vector3 newDir = destination.TransformDirection(relativeDir);

                    other.transform.rotation = Quaternion.LookRotation(newDir, destination.up);

                    float deltaYaw = Mathf.DeltaAngle(beforeYaw, player.eulerAngles.y);
                    cinemachinePanTilt.PanAxis.Value += deltaYaw;

                    controller.enabled = true;
                    onTeleport?.Invoke();
                }
            }
        }
    }
}
