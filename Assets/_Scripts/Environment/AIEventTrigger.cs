using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

[System.Serializable]
public class AIEnableDisableAction
{
    public enum ToggleAction { Enable, Disable }

    public ToggleAction action = ToggleAction.Enable;
    public MonsterType monsterType;
    public int monsterId = 0; // 0 == all of that type
}

[System.Serializable]
public class AIResurrectionAction
{
    public MonsterType monsterType;
    public int monsterId = 0; // 0 == all of that type
    [Range(0f, 1f)]
    public float resurrectionChance = 0.0f;
}

/// <summary>
/// Trigger that can perform two independent groups of actions:
///  - Enable/Disable AI (group A)
///  - Set resurrection chance (group B)
/// 
/// Group A and Group B are separate in the inspector and can be run separately.
/// By default Enable/Disable runs on trigger; Resurrection actions do not (configurable).
/// </summary>
[RequireComponent(typeof(SaveableEntity))]
public class AIEventTrigger : MonoBehaviour, ISaveable
{
    public bool triggerOnce = true;
    public bool playerOnly = true;
    public bool runEnableDisableOnTrigger = true;
    public bool runResurrectionOnTrigger = false;
    public List<AIEnableDisableAction> enableDisableActions = new List<AIEnableDisableAction>();
    public List<AIResurrectionAction> resurrectionActions = new List<AIResurrectionAction>();

    [Header("Unity Events")]
    public UnityEvent onTriggerEnter;
    public UnityEvent onTriggerExit;
    private AIManager aiManager;

    private bool isTriggered = false;

    void Start()
    {
        if (aiManager == null)
            aiManager = AIManager.Instance;

        if (aiManager == null)
            Debug.LogWarning("AIEventTrigger: No AIManager found. Actions requiring AIManager will not work.");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!ShouldTrigger(other)) return;

        if (runEnableDisableOnTrigger) ExecuteEnableDisableActions();
        if (runResurrectionOnTrigger) ExecuteResurrectionActions();

        onTriggerEnter?.Invoke();

        if (triggerOnce) isTriggered = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (playerOnly && !other.CompareTag("Player")) return;
        onTriggerExit?.Invoke();
    }

    private bool ShouldTrigger(Collider other)
    {
        if (triggerOnce && isTriggered) return false;
        if (playerOnly && !other.CompareTag("Player")) return false;
        return true;
    }


    // Execute only the enable/disable actions (group A).    
    public void ExecuteEnableDisableActions()
    {
        if (aiManager == null)
        {
            Debug.LogWarning("AIEventTrigger: No AIManager available for Enable/Disable actions.");
            return;
        }

        foreach (var a in enableDisableActions)
        {
            switch (a.action)
            {
                case AIEnableDisableAction.ToggleAction.Enable:
                    aiManager.ActiveAI(a.monsterType, a.monsterId);
                    // Debug.Log($"AIEventTrigger: Enabled AI → Type={a.monsterType}, ID={a.monsterId}");
                    break;
                case AIEnableDisableAction.ToggleAction.Disable:
                    aiManager.DisableAI(a.monsterType, a.monsterId);
                    // Debug.Log($"AIEventTrigger: Disabled AI → Type={a.monsterType}, ID={a.monsterId}");
                    break;
            }
        }
    }


    // Execute only the resurrection chance actions (group B).   
    public void ExecuteResurrectionActions()
    {
        if (aiManager == null)
        {
            Debug.LogWarning("AIEventTrigger: No AIManager available for Resurrection actions.");
            return;
        }

        foreach (var r in resurrectionActions)
        {
            aiManager.SetResurrectionChance(r.monsterType, r.monsterId, r.resurrectionChance);
            Debug.Log($"AIEventTrigger: Set resurrectionChance={r.resurrectionChance} → Type={r.monsterType}, ID={r.monsterId}");
        }
    }


    public void ExecuteAllActions()
    {
        ExecuteEnableDisableActions();
        ExecuteResurrectionActions();
    }


    public void AddEnableDisableAction(AIEnableDisableAction.ToggleAction type, MonsterType monsterType, int id = 0)
    {
        enableDisableActions.Add(new AIEnableDisableAction { action = type, monsterType = monsterType, monsterId = id });
    }

    public void AddResurrectionAction(MonsterType monsterType, int id = 0, float chance = 0f)
    {
        resurrectionActions.Add(new AIResurrectionAction { monsterType = monsterType, monsterId = id, resurrectionChance = Mathf.Clamp01(chance) });
    }

    public string GetUniqueIdentifier()
    {
        return "AIEventTrigger" + GetComponent<SaveableEntity>().UniqueId;
    }

    public object CaptureState()
    {
        return new SaveData { isEventActive = gameObject.activeSelf, isEventTriggered = isTriggered };
    }

    public void RestoreState(object state)
    {
        string json = state as string;
        SaveData data = JsonUtility.FromJson<SaveData>(json);
        gameObject.SetActive(data.isEventActive);
        isTriggered = data.isEventTriggered;
    }

    public class SaveData
    {
        public bool isEventActive;
        public bool isEventTriggered;
    }
}
