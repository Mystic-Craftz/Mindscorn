using System.Collections;
using UnityEngine;

public class BossDeathState : IState
{
    private BossAI boss;

    public BossDeathState(BossAI boss)
    {
        this.boss = boss;
    }

    public void Enter()
    {
        if (boss == null) return;

        boss.lockStateTransition = true;

        try { boss.StopAllStateSounds(); } catch { }
        try { boss.StopCloseToPlayerLoop(release: true); } catch { }

        if (boss.agent != null)
        {
            try
            {
                boss.agent.isStopped = true;
                boss.agent.updatePosition = false;
                boss.agent.updateRotation = false;
                boss.agent.enabled = false;
            }
            catch { }
        }

        if (boss.sensor != null)
            boss.sensor.enabled = false;

        boss.ResetDashFlags();

        boss.StartCoroutine(DeathSequence());
    }

    public void Exit()
    {

    }

    public void Update()
    {

    }

    private IEnumerator DeathSequence()
    {
        // Play the die animation and make sure the animation controller remains locked afterwards
        if (boss.anim != null)
        {
            boss.anim.ForceLock();

            yield return boss.StartCoroutine(boss.anim.PlayAndWait(boss.die, 0.15f));

            boss.anim.ForceLock();
        }
        else
        {
            yield return null;
        }

    }
}
