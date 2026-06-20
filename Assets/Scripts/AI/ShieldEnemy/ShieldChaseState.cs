using UnityEngine;

public class ShieldChaseState : EnemyState
{
    private ShieldEnemySetup setup;
    private Vector2 originScale;

    public ShieldChaseState(EnemyFSM fsm, ShieldEnemySetup setup) : base(fsm)
    {
        this.setup = setup;
        this.originScale = fsm.transform.localScale;
    }

    public override void Enter()
    {
        if (fsm.Sr != null) fsm.Sr.color = Color.red;
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

        float dist = Vector2.Distance(fsm.transform.position, fsm.Player.position);
        if (dist > setup.ChaseRange)
        {
            fsm.TransitionTo(fsm.Patrol);
            return;
        }

        if (dist < setup.AttackRange)
        {
            fsm.TransitionTo(fsm.Attack);
            return;
        }

        float speed = fsm.Data != null ? fsm.Data.MoveSpeed : 2.5f;
        float dir = fsm.Player.position.x > fsm.transform.position.x ? 1f : -1f;

        if (fsm.Rb != null)
        {
            fsm.Rb.linearVelocity = new Vector2(dir * speed, fsm.Rb.linearVelocity.y);
        }

        ApplyFacing(dir);
    }

    private void ApplyFacing(float dir)
    {
        fsm.transform.localScale = new Vector3(
            Mathf.Abs(originScale.x) * Mathf.Sign(dir),
            originScale.y
        );
    }
}
