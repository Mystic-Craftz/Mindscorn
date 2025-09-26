using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class AIAnimationController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [Tooltip("Default cross-fade time (seconds) between all animations")]
    [SerializeField] private float defaultTransitionDuration = 0.25f;

    private string currentAnimation;
    private bool locked;

    private float currentSpeed = 0f;
    [SerializeField] private float smoothTime = 0.15f;
    private float speedVel = 0f;

    private Dictionary<string, float> clipLengths;

    public string CurrentAnimation => currentAnimation;

    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        // cache clip lengths
        clipLengths = new Dictionary<string, float>();
        foreach (var clip in animator.runtimeAnimatorController.animationClips)
            clipLengths[clip.name] = clip.length;
    }

    public void SetMoveSpeed(float targetSpeed)
    {
        currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedVel, smoothTime);
        animator.SetFloat("MoveSpeed", currentSpeed);
    }


    // Play a one-shot transition. Blends over <transitionDuration> if >0, otherwise uses default.
    public void PlayAnimation(string stateName, float transitionDuration = -1f)
    {
        if (locked) return;
        if (currentAnimation == stateName) return;

        float t = transitionDuration > 0f ? transitionDuration : defaultTransitionDuration;
        animator.CrossFadeInFixedTime(stateName, t, 0, 0f);
        currentAnimation = stateName;
    }


    // Cross-fades into the clip, waits its full length (including the blend), then unlocks. remeber the name of the clip and state should be same
    public IEnumerator PlayAndWait(string stateName, float transitionDuration = -1f)
    {
        locked = true;
        float t = transitionDuration > 0f ? transitionDuration : defaultTransitionDuration;
        animator.CrossFadeInFixedTime(stateName, t, 0, 0f);
        currentAnimation = stateName;

        // wait for blend + clip
        float clipLen = clipLengths.TryGetValue(stateName, out var len) ? len : 0f;
        yield return new WaitForSeconds(t + Mathf.Max(0f, clipLen - t));

        locked = false;
    }

    public float GetClipLength(string stateName)
    {
        if (clipLengths.TryGetValue(stateName, out var len)) return len;
        Debug.LogWarning($"No clip '{stateName}' found!");
        return 0f;
    }

    public void ForceUnlock() => locked = false;
    public void ForceLock() => locked = true;
}
