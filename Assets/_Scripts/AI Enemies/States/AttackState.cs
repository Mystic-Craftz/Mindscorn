using System;
using UnityEngine;
using UnityEngine.AI;

public class AttackState : IState
{
    private readonly MonsterAI monsterAI;
    private readonly NavMeshAgent agent;
    private readonly AISensor sensor;
    private readonly AIAnimationController anim;
    private readonly Transform player;
    private float attackClipLength;
    private readonly float attackRangeSqr;
    private float attackAnimTimer;
    private float cooldownTimer;
    public Type StateType => typeof(AttackState);

    public AttackState(MonsterAI monsterAI)
    {
        this.monsterAI = monsterAI;
        agent = this.monsterAI.agent;
        sensor = this.monsterAI.aiSensor;
        anim = this.monsterAI.aiAnimator;
        player = this.monsterAI.playerTransform;
        attackRangeSqr = this.monsterAI.attackRange * this.monsterAI.attackRange;
    }

    public void Enter()
    {
        monsterAI.StopAllStateSounds();
        attackClipLength = anim.GetClipLength(monsterAI.attackAnim);
        agent.isStopped = true;
        agent.updateRotation = false;
        attackAnimTimer = 0f;

        if (monsterAI.immediateAttack)
        {
            cooldownTimer = monsterAI.attackCooldown;
        }
        else
        {
            cooldownTimer = monsterAI.attackCooldownTimer;
        }
        monsterAI.attackCooldownTimer = cooldownTimer;

        // If immediateAttack is true, play the attack animation.
        if (monsterAI.immediateAttack)
        {
            monsterAI.StartCoroutine(anim.PlayAndWait(monsterAI.attackAnim));
            monsterAI.PlayAttackSound();
            monsterAI.immediateAttack = false;
            attackAnimTimer = attackClipLength;
        }
    }

    public void Update()
    {
        if (PlayerHealth.Instance.GetCurrentHealth() <= 0f)
        {
            cooldownTimer = float.MaxValue;
            monsterAI.StopAllStateSounds();

            // Keep agent stopped and animations idle
            agent.isStopped = true;
            anim.SetMoveSpeed(0f);
            if (anim.CurrentAnimation != monsterAI.locomotionAnim)
                anim.PlayAnimation(monsterAI.locomotionAnim, 0.1f);
            anim.ForceUnlock();

            return;
        }

        // Face the player every frame
        Vector3 dir = player.position - monsterAI.transform.position;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.0001f)
        {
            var targetRot = Quaternion.LookRotation(dir);
            monsterAI.transform.rotation =
                Quaternion.Slerp(monsterAI.transform.rotation, targetRot, monsterAI.rotationSpeed * Time.deltaTime);
        }

        if (attackAnimTimer > 0f)
        {
            attackAnimTimer -= Time.deltaTime;
            return;
        }

        float distSqr = (player.position - monsterAI.transform.position).sqrMagnitude;
        if (distSqr > attackRangeSqr && sensor.PlayerInSight)
        {
            monsterAI.nextStateAfterHiss = monsterAI.chaseState;
            monsterAI.stateMachine.ChangeState(monsterAI.hissState);
            return;
        }
        if (!sensor.PlayerInSight)
        {
            monsterAI.OnPlayerLost();
            return;
        }

        //  Cooldown logic
        cooldownTimer -= Time.deltaTime;
        monsterAI.attackCooldownTimer = cooldownTimer;
        if (cooldownTimer > 0f)
        {
            if (anim.CurrentAnimation != monsterAI.locomotionAnim)
                anim.PlayAnimation(monsterAI.locomotionAnim, 0.1f);
            anim.SetMoveSpeed(0f);
            return;
        }

        // Fire next attack
        monsterAI.StartCoroutine(anim.PlayAndWait(monsterAI.attackAnim));
        monsterAI.PlayAttackSound();
        cooldownTimer = monsterAI.attackCooldown;
        attackAnimTimer = attackClipLength;
        monsterAI.attackCooldownTimer = cooldownTimer;
    }

    public void Exit()
    {
        int soundId = monsterAI.GetStateSoundId(typeof(AttackState));
        AudioManager.Instance.StopStateSound(soundId);

        agent.updateRotation = true;
        anim.ForceUnlock();
    }
}
