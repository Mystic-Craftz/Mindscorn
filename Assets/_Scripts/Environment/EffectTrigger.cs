using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FullscreenEffectTrigger : MonoBehaviour
{
    public FullscreenPassController controller;
    public string playerTag = "Player";
    public bool playOnEnter = true;
    public bool stopOnEnter = false;
    public bool triggerOnce = true;
    private bool hasTriggered = false;

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        if (triggerOnce && hasTriggered) return;

        if (playOnEnter)
        {
            controller.PlayEffect();
        }
        else if (stopOnEnter)
        {
            controller.StopEffect();
        }

        hasTriggered = true;
    }
}
