using UnityEngine;

public class ShieldAttackState : EnemyState
{
    private ShieldEnemySetup setup;
    private float attackTimer;
    private AttackMode attackMode;
    private bool hasAttacked;
    private bool didCounterAttack;
    private ShieldEnemyManager shieldManager;

    enum AttackMode
    {
        Select,
        Block,
        BlockStab,
        Stab,
        Slam,
        ChargeSwing
    }

    public ShieldAttackState(EnemyFSM fsm, ShieldEnemySetup setup) : base(fsm)
    {
        this.setup = setup;
        this.shieldManager = fsm.GetComponent<ShieldEnemyManager>();
    }

    public override void Enter()
    {
        if (fsm.Sr != null) fsm.Sr.color = Color.magenta;

        attackTimer = 0f;
        attackMode = AttackMode.Select;
        hasAttacked = false;
        didCounterAttack = false;
        
        fsm.StopMovement();
    }

    public override void Exit()
    {
        fsm.StopMovement();
    }

    public override void Execute()
    {
        if (fsm.Player == null)
        {
            fsm.TransitionTo(fsm.Idle);
            return;
        }

        switch (attackMode)
        {
            case AttackMode.Select:
                UpdateAttackSelect();
                break;
            case AttackMode.Block:
                UpdateBlock();
                break;
            case AttackMode.BlockStab:
                UpdateBlockStab();
                break;
            case AttackMode.Stab:
                UpdateStab();
                break;
            case AttackMode.Slam:
                UpdateSlam();
                break;
            case AttackMode.ChargeSwing:
                UpdateChargeSwing();
                break;
        }
    }

    private void UpdateAttackSelect()
    {
        attackTimer += Time.deltaTime;
        if (attackTimer < setup.AttackCooldown) return;

        if (shieldManager != null && !shieldManager.IsShieldBroken && Random.value < setup.BlockChance)
        {
            SetAttackMode(AttackMode.Block);
            return;
        }

        int roll = Random.Range(0, 3);
        SetAttackMode(roll == 0 ? AttackMode.Stab : roll == 1 ? AttackMode.Slam : AttackMode.ChargeSwing);
    }

    private void UpdateBlock()
    {
        if (shieldManager != null && shieldManager.IsShieldBroken)
        {
            SetAttackMode(AttackMode.Select);
            return;
        }

        attackTimer += Time.deltaTime;

        if (!didCounterAttack && attackTimer > setup.CounterAttackDelay && Random.value < setup.CounterAttackChance)
        {
            didCounterAttack = true;
            SetAttackMode(AttackMode.BlockStab);
            return;
        }

        if (attackTimer >= setup.BlockTime)
        {
            fsm.TransitionTo(fsm.Chase);
        }
    }

    private void UpdateBlockStab()
    {
        attackTimer += Time.deltaTime;
        if (!hasAttacked && attackTimer >= setup.BlockStabDelay)
        {
            fsm.PerformAttack(setup.BlockStabRange);
            hasAttacked = true;
        }

        if (attackTimer >= setup.BlockStabExitTime)
        {
            fsm.TransitionTo(fsm.Chase);
        }
    }

    private void UpdateStab()
    {
        attackTimer += Time.deltaTime;
        if (!hasAttacked && attackTimer >= setup.StabDelay)
        {
            fsm.PerformAttack(setup.StabRange);
            hasAttacked = true;
        }

        if (attackTimer >= setup.StabDelay + setup.StabExitDelay)
        {
            fsm.TransitionTo(fsm.Chase);
        }
    }

    private void UpdateSlam()
    {
        attackTimer += Time.deltaTime;
        if (!hasAttacked && attackTimer >= setup.SlamDelay)
        {
            fsm.PerformAttack(setup.SlamRange);
            hasAttacked = true;
        }

        if (attackTimer >= setup.SlamDelay + setup.SlamExitDelay)
        {
            fsm.TransitionTo(fsm.Chase);
        }
    }

    private void UpdateChargeSwing()
    {
        attackTimer += Time.deltaTime;

        if (attackTimer < setup.ChargeAttackStart)
        {
            float dir = fsm.transform.localScale.x > 0 ? 1f : -1f;
            if (fsm.Rb != null)
            {
                fsm.Rb.linearVelocity = new Vector2(dir * setup.ChargeSpeed, fsm.Rb.linearVelocity.y);
            }
        }
        else if (attackTimer >= setup.ChargeAttackStart && attackTimer < setup.ChargeAttackEnd)
        {
            fsm.StopMovement();
            if (!hasAttacked)
            {
                fsm.PerformAttack(setup.ChargeAttackRange);
                hasAttacked = true;
            }
        }
        else if (attackTimer >= setup.ChargeDuration)
        {
            fsm.TransitionTo(fsm.Chase);
        }
    }

    private void SetAttackMode(AttackMode mode)
    {
        attackMode = mode;
        attackTimer = 0f;
        hasAttacked = false;
        didCounterAttack = false;
    }
}
