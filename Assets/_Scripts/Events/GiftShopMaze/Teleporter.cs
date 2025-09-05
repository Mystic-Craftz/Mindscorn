using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class Teleporter : MonoBehaviour
{
    [SerializeField] private Transform destination;
    [SerializeField] private CinemachinePanTilt cinemachinePanTilt;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            CharacterController controller = other.GetComponent<CharacterController>();
            Transform player = controller.transform;
            if (controller != null)
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
            }
        }
    }
}
