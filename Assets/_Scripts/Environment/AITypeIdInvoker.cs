using UnityEngine;

public class AITypeIdInvoker : MonoBehaviour
{
    public MonsterType monsterType;
    public int monsterId = 0;

    [Tooltip("Optional debug logs")]
    public bool debug = false;

    public void Enable()
    {
        if (AIManager.Instance == null)
        {
            if (debug) Debug.LogWarning("AIManager.Instance is null");
            return;
        }
        AIManager.Instance.ActiveAI(monsterType, monsterId);
        if (debug) Debug.Log($"type={monsterType} id={monsterId}");
    }

    public void Disable()
    {
        if (AIManager.Instance == null)
        {
            if (debug) Debug.LogWarning("AIManager.Instance is null");
            return;
        }
        AIManager.Instance.DisableAI(monsterType, monsterId);
        if (debug) Debug.Log($"type={monsterType} id={monsterId}");
    }
}
