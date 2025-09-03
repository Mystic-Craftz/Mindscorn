using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the checkpoints and objectives within the game.
/// Handles saving and restoring states, as well as completing objectives.
/// </summary>
public class CheckpointManager : MonoBehaviour, ISaveable
{
    public static CheckpointManager Instance { get; private set; }
    [SerializeField] private List<Objective> objectives;

    public int currentObjIndex = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Retrieves all objectives.
    /// </summary>
    /// <returns>A list of all objectives.</returns>
    public List<Objective> GetAllObjectives() => objectives;

    public Objective GetCurrentObjective() => objectives[currentObjIndex];

    /// <summary>
    /// Checks if the given ID is the current objective ID.
    /// </summary>
    /// <param name="id">The ID to check against the current objective index.</param>
    /// <returns>True if the ID is the current objective ID, false otherwise.</returns>
    public bool IsThisTheCurrentObjectiveId(int id) => id == currentObjIndex;

    /// <summary>
    /// Checks if an objective with the specified ID has been completed.
    /// </summary>
    /// <param name="id">The ID of the objective to check.</param>
    /// <returns>True if the objective is completed, false otherwise.</returns>
    public bool CheckIfObjectiveIsCompletedById(float id)
    {
        foreach (Objective objective in objectives)
        {
            if (objective.data.id == id && objective.completed)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Checks if the specified Objective Scriptable Object has been completed.
    /// </summary>
    /// <param name="objectiveSO">The Objective Scriptable Object to check.</param>
    /// <returns>True if the objective is completed, false otherwise.</returns>
    public bool CheckIfObjectiveIsCompletedBySO(ObjectiveSO objectiveSO)
    {
        foreach (Objective objective in objectives)
        {
            if (objective.data == objectiveSO && objective.completed)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Retrieves objectives by the current objective ID.
    /// </summary>
    /// <returns>A list of objectives that match the current objective ID and are not completed.</returns>
    public List<Objective> GetObjectivesByCurrentObjId()
    {
        List<Objective> objectivesById = new List<Objective>();
        foreach (Objective objective in objectives)
        {
            if (MathF.Floor(objective.data.id) == currentObjIndex)
            {
                objectivesById.Add(objective);
            }
        }
        return objectivesById;
    }

    /// <summary>
    /// Completes the objective with the specified ID.
    /// </summary>
    /// <param name="id">The ID of the objective to complete.</param>
    /// <returns>The completed objective.</returns>
    public Objective CompleteObjectiveById(float id)
    {
        Objective completedObjective = null;
        foreach (Objective objective in objectives)
        {
            if (objective.data.id == id)
            {
                objective.completed = true;
                completedObjective = objective;
            }
        }

        bool areAllObjsOfCurrentIndexCompleted = true;

        foreach (Objective objective in objectives)
        {
            if (Mathf.Floor(objective.data.id) == currentObjIndex && !objective.completed)
            {
                areAllObjsOfCurrentIndexCompleted = false;
                break;
            }
        }

        if (areAllObjsOfCurrentIndexCompleted)
        {
            currentObjIndex += 1;
        }

        return completedObjective;
    }

    /// <summary>
    /// Completes the specified Objective Scriptable Object.
    /// </summary>
    /// <param name="objectiveSO">The Objective Scriptable Object to complete.</param>
    /// <returns>The completed objective.</returns>
    public Objective CompleteObjectiveBySO(ObjectiveSO objectiveSO)
    {
        Objective completedObjective = null;
        foreach (Objective objective in objectives)
        {
            if (objective.data == objectiveSO)
            {
                objective.completed = true;
                completedObjective = objective;
            }
        }

        bool areAllObjsOfCurrentIndexCompleted = true;

        foreach (Objective objective in objectives)
        {
            if (Mathf.Floor(objective.data.id) == currentObjIndex && !objective.completed)
            {
                areAllObjsOfCurrentIndexCompleted = false;
                break;
            }
        }

        if (areAllObjsOfCurrentIndexCompleted)
        {
            currentObjIndex += 1;
        }

        return completedObjective;
    }

    /// <summary>
    /// Gets the unique identifier for the saveable entity.
    /// </summary>
    /// <returns>The unique identifier as a string.</returns>
    public string GetUniqueIdentifier()
    {
        return GetComponent<SaveableEntity>().UniqueId;
    }

    /// <summary>
    /// Captures the current state of objectives and player index.
    /// </summary>
    /// <returns>A SaveData object containing the current state.</returns>
    public object CaptureState()
    {
        return new SaveData(objectives, currentObjIndex);
    }

    /// <summary>
    /// Restores the state of objectives from saved data.
    /// </summary>
    /// <param name="state">The saved state to restore from.</param>
    public void RestoreState(object state)
    {
        string json = state as string;
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogWarning("RestoreState: Received null or empty JSON string.");
            return;
        }

        SaveData data = JsonUtility.FromJson<SaveData>(json);

        if (objectives.Count != data.completedFlags.Count)
        {
            Debug.LogWarning("RestoreState: Saved objectives count does not match current objectives count.");
            return;
        }
        for (int i = 0; i < objectives.Count; i++)
        {
            objectives[i].completed = data.completedFlags[i];
        }
        currentObjIndex = data.playerObjectiveIndex;
    }

    /// <summary>
    /// Serializable class for saving the state of objectives and player index.
    /// </summary>
    [Serializable]
    class SaveData
    {
        public int playerObjectiveIndex;
        public List<bool> completedFlags;

        public SaveData(List<Objective> objectives, int playerObjectiveIndex)
        {
            completedFlags = new List<bool>();
            this.playerObjectiveIndex = playerObjectiveIndex;
            foreach (var obj in objectives)
            {
                completedFlags.Add(obj.completed);
            }
        }
    }
}

/// <summary>
/// Represents an objective with its data and completion status.
/// </summary>
[Serializable]
public class Objective
{
    public ObjectiveSO data;
    public bool completed;
}

