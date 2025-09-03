using System.Collections.Generic;
using UnityEngine;

public class TestGymCube : MonoBehaviour
{
    [SerializeField] private ObjectiveSO objective1SO;

    public void Finish0()
    {
        //* This function only runs if this objective is already not finished and check is done with ID
        if (CheckpointManager.Instance.CheckIfObjectiveIsCompletedById(0)) return;

        Rigidbody rb = GetComponent<Rigidbody>();
        rb.AddForce(Vector3.up * 10, ForceMode.Impulse);
        Objective completedObjective = CheckpointManager.Instance.CompleteObjectiveById(0);
        List<Objective> newObjs = CheckpointManager.Instance.GetObjectivesByCurrentObjId();
        Debug.Log(newObjs.Count);
        newObjs.ForEach(obj => NotificationUI.Instance.ShowNotification($"Next objective: {obj.data.id}"));
        NotificationUI.Instance.ShowNotification($"Finished {completedObjective.data.objective}");
    }

    public void Finish1()
    {
        //* This function only runs if this objective is already not finished and check is done with SO
        if (CheckpointManager.Instance.CheckIfObjectiveIsCompletedBySO(objective1SO)) return;

        Rigidbody rb = GetComponent<Rigidbody>();
        rb.AddForce(Vector3.forward * 10, ForceMode.Impulse);
        Objective completedObjective = CheckpointManager.Instance.CompleteObjectiveBySO(objective1SO);
        List<Objective> newObjs = CheckpointManager.Instance.GetObjectivesByCurrentObjId();
        Debug.Log(newObjs.Count);
        newObjs.ForEach(obj => NotificationUI.Instance.ShowNotification($"Next objective: {obj.data.id}"));
        NotificationUI.Instance.ShowNotification($"Finished {completedObjective.data.objective}");
    }

    public void Finish2o1()
    {
        if (CheckpointManager.Instance.CheckIfObjectiveIsCompletedById(2.1f)) return;
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.AddTorque(Vector3.up * 10, ForceMode.Impulse);
        Objective completedObjective = CheckpointManager.Instance.CompleteObjectiveById(2.1f);
        List<Objective> newObjs = CheckpointManager.Instance.GetObjectivesByCurrentObjId();
        newObjs.ForEach(obj => NotificationUI.Instance.ShowNotification($"Next objective: {obj.data.id}"));
        NotificationUI.Instance.ShowNotification($"Finished {completedObjective.data.objective}");
    }

    public void Finish2o2()
    {
        if (CheckpointManager.Instance.CheckIfObjectiveIsCompletedById(2.2f)) return;
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.AddTorque(Vector3.back * 10, ForceMode.Impulse);
        Objective completedObjective = CheckpointManager.Instance.CompleteObjectiveById(2.2f);
        List<Objective> newObjs = CheckpointManager.Instance.GetObjectivesByCurrentObjId();
        newObjs.ForEach(obj => NotificationUI.Instance.ShowNotification($"Next objective: {obj.data.id}"));
        NotificationUI.Instance.ShowNotification($"Finished {completedObjective.data.objective}");
    }

    public void Finish3()
    {
        if (CheckpointManager.Instance.CheckIfObjectiveIsCompletedById(3)) return;
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.AddForce(Vector3.up * 10, ForceMode.Impulse);
        rb.AddTorque(Vector3.up * 10, ForceMode.Impulse);
        Objective completedObjective = CheckpointManager.Instance.CompleteObjectiveById(3);
        List<Objective> newObjs = CheckpointManager.Instance.GetObjectivesByCurrentObjId();
        newObjs.ForEach(obj => NotificationUI.Instance.ShowNotification($"Next objective: {obj.data.id}"));
        NotificationUI.Instance.ShowNotification($"Finished {completedObjective.data.objective}");
    }
}
