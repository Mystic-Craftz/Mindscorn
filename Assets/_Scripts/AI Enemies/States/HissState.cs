using System;
using UnityEngine;


public class HissState : IState
{
    private readonly MonsterAI monsterAI;
    private readonly AIAnimationController anim;
    private readonly Transform player;
    private float hissTimer;
    public Type StateType => typeof(HitState);

    public HissState(MonsterAI ai)
    {
        monsterAI = ai;
        anim = ai.aiAnimator;
        player = ai.playerTransform;
    }

    public void Enter()
    {
        monsterAI.StopAllStateSounds();
        // pause rotation+movement
        if (monsterAI.agent.isOnNavMesh)
        {
            monsterAI.agent.isStopped = true;
            monsterAI.agent.updateRotation = false;
        }

        hissTimer = anim.GetClipLength(monsterAI.hissAnim);
        monsterAI.StartCoroutine(anim.PlayAndWait(monsterAI.hissAnim));
        monsterAI.PlayHissSound();
    }

    public void Update()
    {
        if (player == null) return;

        // face player
        Vector3 dir = player.position - monsterAI.transform.position;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.0001f)
        {
            monsterAI.transform.rotation =
                Quaternion.Slerp(
                    monsterAI.transform.rotation,
                    Quaternion.LookRotation(dir),
                    monsterAI.rotationSpeed * Time.deltaTime
                );
        }

        if ((hissTimer -= Time.deltaTime) <= 0f)
            monsterAI.stateMachine.ChangeState(monsterAI.nextStateAfterHiss);
    }

    public void Exit()
    {
        int soundId = monsterAI.GetStateSoundId(typeof(HissState));
        AudioManager.Instance.StopStateSound(soundId);

        if (monsterAI.agent.isOnNavMesh)
        {
            monsterAI.agent.isStopped = false;
            monsterAI.agent.updateRotation = true;
        }
    }
}
