using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AIManager : MonoBehaviour
{
    public static AIManager Instance { get; private set; }

    // pending registrations for when AIManager isn't initialized yet
    private static List<MonsterAI> pendingRegistrations = new List<MonsterAI>();

    // single pending boss registration
    private static BossAI pendingBossRegistration = null;

    // monster registries 
    private readonly Dictionary<int, MonsterAI> byId = new Dictionary<int, MonsterAI>();
    private readonly Dictionary<MonsterType, HashSet<MonsterAI>> byType = new Dictionary<MonsterType, HashSet<MonsterAI>>();

    // single boss 
    private BossAI boss;

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

        // process pending monster regs
        if (pendingRegistrations.Count > 0)
        {
            foreach (var ai in pendingRegistrations)
            {
                if (ai != null) RegisterInternal(ai);
            }
            pendingRegistrations.Clear();
        }

        // process pending boss reg (if any)
        if (pendingBossRegistration != null)
        {
            RegisterInternal(pendingBossRegistration);
            pendingBossRegistration = null;
        }
    }

    // ---------- Static entry points so AIs can call Register at anytime ----------
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

    // Boss registration (single boss expected)
    public static void Register(BossAI boss)
    {
        if (boss == null) return;
        if (Instance != null) Instance.RegisterInternal(boss);
        else
        {
            if (pendingBossRegistration == null) pendingBossRegistration = boss;
            else
            {
                Debug.LogWarning("[AIManager] Multiple BossAI attempted to register before AIManager existed. Ignoring additional boss: " + boss.name);
            }
        }
    }

    public static void Unregister(BossAI boss)
    {
        if (boss == null) return;
        if (Instance != null) Instance.UnregisterInternal(boss);
        else
        {
            if (pendingBossRegistration == boss) pendingBossRegistration = null;
        }
    }

    // ---------- Internal registration implementations ----------
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
        if (ai == null) return;

        if (ai.MonsterID > 0 && byId.TryGetValue(ai.MonsterID, out var existing) && existing == ai)
            byId.Remove(ai.MonsterID);

        if (byType.TryGetValue(ai.MonsterType, out var set))
        {
            set.Remove(ai);
            if (set.Count == 0) byType.Remove(ai.MonsterType);
        }

        if (logActions) Debug.Log($"AIManager: Unregistered id={ai.MonsterID} type={ai.MonsterType} ({ai.name})");
    }

    // ---------- Boss registration internals (single boss) ----------
    private void RegisterInternal(BossAI newBoss)
    {
        if (newBoss == null) return;

        if (boss == null)
        {
            boss = newBoss;
            if (logActions) Debug.Log($"Registered Boss: {boss.name} Active={boss.gameObject.activeSelf}");
        }
        else
        {
            Debug.LogWarning($"AIManager: A BossAI ({newBoss.name}) attempted to register but a boss ({boss.name}) is already registered. Ignoring {newBoss.name}.");
        }
    }

    private void UnregisterInternal(BossAI removedBoss)
    {
        if (removedBoss == null) return;

        if (boss == removedBoss)
        {
            if (logActions) Debug.Log($"AIManager: Unregistered Boss {boss.name}");
            boss = null;
        }
        else
        {
            if (logActions) Debug.LogWarning($"AIManager: Unregister called for an unknown boss {removedBoss.name}");
        }
    }

    // ---------- Existing public helpers for monsters ----------
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

    // ---------- Existing public helpers for bosses ----------
    public BossAI GetBoss() => boss;

    public void SetBossActive(bool isActive)
    {
        if (boss == null)
        {
            if (logActions) Debug.LogWarning("AIManager: No boss registered to SetBossActive.");
            return;
        }

        boss.SetActiveState(isActive);
        if (logActions) Debug.Log($"AIManager: Set boss '{boss.name}' active={isActive}");
    }

    public void DisableBoss() => SetBossActive(false);

    public void ActivateBoss() => SetBossActive(true);



    // Enable/disable boss invincibility flag in BossHealth (invincibleDuringStalking).
    public void SetBossInvincibility(bool invincible)
    {
        if (boss == null)
        {
            if (logActions) Debug.LogWarning("AIManager: No boss registered to SetBossInvincibility.");
            return;
        }

        var health = boss.GetComponent<BossHealth>();
        if (health == null)
        {
            if (logActions) Debug.LogWarning("AIManager: Boss has no BossHealth component.");
            return;
        }

        health.invincibleDuringStalking = invincible;
        if (logActions) Debug.Log($"AIManager: Set boss invincibility={invincible}");
    }


    // warp boss to destination and optionally activate
    public void WarpBossTo(Transform destination, bool activateAfterWarp = true)
    {
        if (boss == null)
        {
            if (logActions) Debug.LogWarning("AIManager: No boss registered to WarpBossTo.");
            return;
        }

        if (destination == null)
        {
            Debug.LogWarning("AIManager: WarpBossTo called with null destination.");
            return;
        }

        try
        {
            var agent = boss.agent;
            if (agent != null)
            {
                // If agent is on the navmesh, warp; otherwise fall back to setting transform
                if (agent.isOnNavMesh)
                {
                    // Warp keeps internal NavMeshAgent state consistent
                    bool warped = agent.Warp(destination.position);
                    if (!warped)
                    {
                        // fallback
                        boss.transform.position = destination.position;
                    }
                }
                else
                {
                    boss.transform.position = destination.position;
                }

                // align rotation to destination
                boss.transform.rotation = destination.rotation;
            }
            else
            {
                boss.transform.position = destination.position;
                boss.transform.rotation = destination.rotation;
            }

            if (activateAfterWarp)
            {
                SetBossActive(true);
            }

            if (logActions) Debug.Log($"AIManager: Warped boss to '{destination.name}' at {destination.position} (activateAfterWarp={activateAfterWarp})");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"AIManager: Exception while warping boss: {ex}");
            // fallback attempt
            boss.transform.position = destination.position;
            boss.transform.rotation = destination.rotation;
            if (activateAfterWarp) SetBossActive(true);
        }
    }
}
