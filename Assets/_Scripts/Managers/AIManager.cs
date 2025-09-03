using System.Collections.Generic;
using UnityEngine;

public class AIManager : MonoBehaviour
{
    public static AIManager Instance { get; private set; }
    private static List<MonsterAI> pendingRegistrations = new List<MonsterAI>();
    private readonly Dictionary<int, MonsterAI> byId = new Dictionary<int, MonsterAI>();
    private readonly Dictionary<MonsterType, HashSet<MonsterAI>> byType = new Dictionary<MonsterType, HashSet<MonsterAI>>();

    [Header("Debug")]
    public bool logActions = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (pendingRegistrations.Count > 0)
        {
            foreach (var ai in pendingRegistrations)
            {
                if (ai != null) RegisterInternal(ai);
            }
            pendingRegistrations.Clear();
        }
    }

    //  Static entry points so AIs can call Register at anytime 
    public static void Register(MonsterAI ai)
    {
        if (ai == null) return;
        if (Instance != null) Instance.RegisterInternal(ai);
        else pendingRegistrations.Add(ai);
    }

    public static void Unregister(MonsterAI ai)
    {
        if (ai == null) return;
        if (Instance != null) Instance.UnregisterInternal(ai);
        else pendingRegistrations.Remove(ai);
    }

    private void RegisterInternal(MonsterAI ai)
    {
        if (ai == null) return;

        // ID registration
        if (ai.MonsterID > 0)
        {
            // Remove existing entry if ID matches
            if (byId.TryGetValue(ai.MonsterID, out var existing) && existing != ai)
            {
                UnregisterInternal(existing);
            }
            byId[ai.MonsterID] = ai;
        }

        // Type registration
        if (!byType.TryGetValue(ai.MonsterType, out var set))
        {
            set = new HashSet<MonsterAI>();
            byType[ai.MonsterType] = set;
        }
        set.Add(ai);

        if (logActions) Debug.Log($"Registered: ID={ai.MonsterID} Type={ai.MonsterType} Active={ai.gameObject.activeSelf}");
    }

    private void UnregisterInternal(MonsterAI ai)
    {
        if (ai.MonsterID > 0 && byId.TryGetValue(ai.MonsterID, out var existing) && existing == ai)
            byId.Remove(ai.MonsterID);

        if (byType.TryGetValue(ai.MonsterType, out var set))
        {
            set.Remove(ai);
            if (set.Count == 0) byType.Remove(ai.MonsterType);
        }

        if (logActions) Debug.Log($"AIManager: Unregistered id={ai.MonsterID} type={ai.MonsterType} ({ai.name})");
    }

    public MonsterAI GetById(int id) => id > 0 && byId.TryGetValue(id, out var m) ? m : null;

    public List<MonsterAI> GetByType(MonsterType type)
    {
        if (byType.TryGetValue(type, out var set))
            return new List<MonsterAI>(set);
        return new List<MonsterAI>(0);
    }


    public void ActiveAI(MonsterType type, int id)
    {
        if (id > 0)
        {
            if (byId.TryGetValue(id, out var ai))
            {
                if (ai.MonsterType == type) ai.SetActiveState(true);
                else if (logActions) Debug.LogWarning($"ActiveAI: type mismatch for id {id}");
            }
            else if (logActions) Debug.LogWarning($"ActiveAI: no monster with id {id}");
        }
        else
        {
            if (byType.TryGetValue(type, out var set))
            {
                foreach (var a in set) a.SetActiveState(true);
                if (logActions) Debug.Log($"ActiveAI: Activated {set.Count} of type {type}");
            }
            else if (logActions) Debug.LogWarning($"ActiveAI: no monsters of type {type}");
        }
    }

    public void DisableAI(MonsterType type, int id)
    {
        if (id > 0)
        {
            if (byId.TryGetValue(id, out var ai))
            {
                if (ai.MonsterType == type) ai.SetActiveState(false);
                else if (logActions) Debug.LogWarning($"DisableAI: type mismatch for id {id}");
            }
            else if (logActions) Debug.LogWarning($"DisableAI: no monster with id {id}");
        }
        else
        {
            if (byType.TryGetValue(type, out var set))
            {
                foreach (var a in set) a.SetActiveState(false);
                if (logActions) Debug.Log($"DisableAI: Disabled {set.Count} of type {type}");
            }
            else if (logActions) Debug.LogWarning($"DisableAI: no monsters of type {type}");
        }
    }

    public void SetResurrectionChance(MonsterType type, int id, float chance)
    {
        float clamped = Mathf.Clamp01(chance);

        if (id > 0)
        {
            if (byId.TryGetValue(id, out var ai))
            {
                ai.resurrectionChance = clamped;
                if (logActions) Debug.Log($"AIManager: Set resurrectionChance={clamped} for ID={id}");
            }
            else if (logActions) Debug.LogWarning($"AIManager: No monster with id {id} to set resurrectionChance.");
        }
        else
        {
            if (byType.TryGetValue(type, out var set))
            {
                int count = 0;
                foreach (var m in set)
                {
                    m.resurrectionChance = clamped;
                    count++;
                }
                if (logActions) Debug.Log($"AIManager: Set resurrectionChance={clamped} for {count} monsters of type {type}");
            }
            else if (logActions) Debug.LogWarning($"AIManager: No monsters of type {type} to set resurrectionChance.");
        }
    }
}
