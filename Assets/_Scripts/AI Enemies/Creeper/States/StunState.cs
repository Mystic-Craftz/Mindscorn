using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class StunState : IState
{
    private MonsterAI monsterAI;
    private AIAnimationController aiAnimator;
    private NavMeshAgent agent;
    private float originalSpeed;
    private Coroutine stunSoundRoutine;

    public StunState(MonsterAI ai)
    {
        monsterAI = ai;
        aiAnimator = ai.aiAnimator;
        agent = ai.GetComponent<NavMeshAgent>();
    }

    public void Enter()
    {
        originalSpeed = agent.speed;
        agent.speed = 0f;
        agent.isStopped = true;

        aiAnimator.ForceUnlock();
        aiAnimator.PlayAnimation(monsterAI.stunAnim);

        monsterAI.currentStunTimer = 0f;

        // Start timer for stun duration
        monsterAI.StartCoroutine(StunTimer());

        // Start repeating audio
        stunSoundRoutine = monsterAI.StartCoroutine(PlayStunSoundLoop());
    }

    public void Update() { }

    public void Exit()
    {
        agent.isStopped = false;
        agent.speed = originalSpeed;
        aiAnimator.ForceUnlock();

        // Stop audio coroutine if active
        if (stunSoundRoutine != null)
        {
            monsterAI.StopCoroutine(stunSoundRoutine);
            stunSoundRoutine = null;
        }
    }

    private IEnumerator StunTimer()
    {
        while (monsterAI.currentStunTimer < monsterAI.stunTime)
        {
            monsterAI.currentStunTimer += Time.deltaTime;
            yield return null;
        }

        monsterAI.stateMachine.ChangeState(monsterAI.nextStateAfterStun);
    }

    private IEnumerator PlayStunSoundLoop()
    {
        while (true)
        {
            var inst = AudioManager.Instance.CreateInstance(monsterAI.stunSound);
            inst.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(monsterAI.transform.position));
            inst.start();

            FMOD.Studio.PLAYBACK_STATE state;
            do
            {
                inst.getPlaybackState(out state);
                yield return null;
            } while (state != FMOD.Studio.PLAYBACK_STATE.STOPPED &&
                     state != FMOD.Studio.PLAYBACK_STATE.STOPPING);

            inst.release();

            yield return new WaitForSeconds(1f);
        }
    }
}
