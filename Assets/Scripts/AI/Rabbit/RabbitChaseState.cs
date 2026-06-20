using UnityEngine;

public class RabbitChaseState : EnemyState
{
    private RabbitSetup setup;
    private float hopTimer;

    public RabbitChaseState(EnemyFSM fsm, RabbitSetup setup) : base(fsm)
    {
        this.setup = setup;
    }

    public override void Enter()
    {
        if (fsm.Sr != null) fsm.Sr.color = Color.red;
        hopTimer = 0f;
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
            fsm.StopMovement();
            fsm.TransitionTo(fsm.Patrol);
            return;
        }

        if (dist < setup.AttackRange)
        {
            fsm.StopMovement();
            fsm.TransitionTo(fsm.Attack);
            return;
        }

        float speed = fsm.Data != null ? fsm.Data.MoveSpeed : 2f;
        float moveDir = fsm.Player.position.x > fsm.transform.position.x ? 1f : -1f;

        if (fsm.Rb != null)
        {
            fsm.Rb.linearVelocity = new Vector2(moveDir * speed, fsm.Rb.linearVelocity.y);
            
            hopTimer += Time.deltaTime;
            if (hopTimer >= setup.HopInterval)
            {
                fsm.Rb.linearVelocity = new Vector2(fsm.Rb.linearVelocity.x, setup.HopForce);
                hopTimer = 0f;
            }
        }

        SetFacing(moveDir > 0);
    }

    private void SetFacing(bool faceRight)
    {
        Vector3 scale = fsm.transform.localScale;
        float absX = Mathf.Abs(scale.x);
        if (absX < 0.0001f) absX = 1f;

        bool usePositiveX = setup.DefaultFacingRight ? faceRight : !faceRight;
        scale.x = usePositiveX ? absX : -absX;
        fsm.transform.localScale = scale;
    }
}
