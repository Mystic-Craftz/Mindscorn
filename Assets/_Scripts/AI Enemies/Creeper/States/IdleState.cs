using System.Collections;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using UnityEngine.AI;

public class IdleState : IState
{
    private MonsterAI monsterAI;
    private bool hasReachedSpot;
    public bool sequenceStarted { get; private set; }
    public Coroutine eatRoutine { get; private set; }

    // gore audio
    private Coroutine goreRoutine;
    private EventInstance currentGoreInstance;

    public IdleState(MonsterAI ai)
    {
        monsterAI = ai;
    }

    public void Enter()
    {
        hasReachedSpot = false;
        sequenceStarted = false;
        eatRoutine = null;

        goreRoutine = null;
        currentGoreInstance = default;

        monsterAI.agent.stoppingDistance = 0f;
        monsterAI.agent.isStopped = false;

        if (monsterAI.eatingSpot != null)
        {
            monsterAI.agent.SetDestination(monsterAI.eatingSpot.position);
        }
        else
        {
            monsterAI.agent.isStopped = true;
        }

        monsterAI.aiAnimator.ForceUnlock();
        monsterAI.aiAnimator.PlayAnimation(monsterAI.locomotionAnim, 0.1f);
        monsterAI.aiAnimator.SetMoveSpeed(
            monsterAI.agent.isStopped
              ? 0f
              : monsterAI.agent.velocity.magnitude
        );
    }

    public void Update()
    {
        if (!hasReachedSpot)
        {
            if (!monsterAI.agent.pathPending
                && monsterAI.agent.remainingDistance <= monsterAI.agent.stoppingDistance)
            {
                hasReachedSpot = true;
                monsterAI.agent.isStopped = true;
                monsterAI.aiAnimator.SetMoveSpeed(0f);
            }
            else
            {
                monsterAI.aiAnimator.SetMoveSpeed(monsterAI.agent.velocity.magnitude);
                return;
            }
        }

        if (hasReachedSpot && !sequenceStarted)
        {
            sequenceStarted = true;
            eatRoutine = monsterAI.StartCoroutine(EatSequence());
        }
    }

    public void Exit()
    {
        // stop eat sequence if running
        if (sequenceStarted && eatRoutine != null)
        {
            monsterAI.StopCoroutine(eatRoutine);
            eatRoutine = null;
            sequenceStarted = false;
        }

        // stop gore routine if running
        if (goreRoutine != null)
        {
            monsterAI.StopCoroutine(goreRoutine);
            goreRoutine = null;
        }

        try
        {
            FMOD.Studio.PLAYBACK_STATE tmp;
            currentGoreInstance.getPlaybackState(out tmp);
            currentGoreInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            currentGoreInstance.release();
        }
        catch { /* ignore if instance wasn't valid */ }

        currentGoreInstance = default;
    }

    private IEnumerator EatSequence()
    {
        // getting down animation — wait until it finishes
        yield return monsterAI.aiAnimator.PlayAndWait(monsterAI.gettingDownAnim);

        // start eating animation
        monsterAI.aiAnimator.PlayAnimation(monsterAI.eatingAnim, 0.1f);

        // wait a few frames at most for animator to register the current animation
        int safetyFrames = 8; // small buffer
        while (safetyFrames-- > 0 && monsterAI.aiAnimator.CurrentAnimation != monsterAI.eatingAnim)
            yield return null;


        if (!monsterAI.idleGoreSound.IsNull)
        {
            goreRoutine = monsterAI.StartCoroutine(PlayIdleGoreLoop());
        }
    }

    private IEnumerator PlayIdleGoreLoop()
    {
        // keep playing while we're still in IdleState, at spot, and eating animation is playing
        while (monsterAI.stateMachine.CurrentState == monsterAI.idleState
               && hasReachedSpot
               && monsterAI.aiAnimator.CurrentAnimation == monsterAI.eatingAnim)
        {
            // create & start instance
            currentGoreInstance = AudioManager.Instance.CreateInstance(monsterAI.idleGoreSound);
            currentGoreInstance.set3DAttributes(RuntimeUtils.To3DAttributes(monsterAI.transform.position));
            try
            {
                if (AudioManager.Instance.AreAIVoicesMuted())
                    currentGoreInstance.setPaused(true);
            }
            catch { }

            currentGoreInstance.start();

            // wait for playback finish OR for any condition to break
            FMOD.Studio.PLAYBACK_STATE state;
            while (true)
            {
                // update position each frame so 3D follows monster
                try { currentGoreInstance.set3DAttributes(RuntimeUtils.To3DAttributes(monsterAI.transform.position)); } catch { }

                // check playback state
                currentGoreInstance.getPlaybackState(out state);
                if (state == FMOD.Studio.PLAYBACK_STATE.STOPPED || state == FMOD.Studio.PLAYBACK_STATE.STOPPING)
                    break;

                bool stillInIdle = (monsterAI.stateMachine.CurrentState == monsterAI.idleState);
                bool atSpotNow = hasReachedSpot;
                bool animEatingNow = (monsterAI.aiAnimator.CurrentAnimation == monsterAI.eatingAnim);

                if (!stillInIdle || !atSpotNow || !animEatingNow)
                {
                    try { currentGoreInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE); } catch { }
                    break;
                }

                yield return null;
            }

            // release instance
            try { currentGoreInstance.release(); } catch { }
            currentGoreInstance = default;

            float waitTime = 0.1f;
            float t = 0f;
            while (t < waitTime)
            {
                if (!(monsterAI.stateMachine.CurrentState == monsterAI.idleState
                      && hasReachedSpot
                      && monsterAI.aiAnimator.CurrentAnimation == monsterAI.eatingAnim))
                {
                    yield break;
                }
                t += Time.deltaTime;
                yield return null;
            }
        }
    }
}
