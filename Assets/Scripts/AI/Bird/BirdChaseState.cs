using UnityEngine;

public class BirdChaseState : EnemyState
{
    private BirdSetup setup;
    private float hoverY;

    public BirdChaseState(EnemyFSM fsm, BirdSetup setup) : base(fsm)
    {
        this.setup = setup;
    }

    public override void Enter()
    {
        if (fsm.Sr != null) fsm.Sr.color = Color.red;
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

        float dx = fsm.Player.position.x - fsm.transform.position.x;
        float horizDist = Mathf.Abs(dx);
        if (horizDist <= setup.AttackRange)
        {
            fsm.StopMovement();
            fsm.TransitionTo(fsm.Attack);
            return;
        }

        float speed = fsm.Data != null ? fsm.Data.MoveSpeed : 2f;
        hoverY = fsm.Player.position.y + setup.HoverHeightAboveTarget;

        float moveDir = 0f;
        if (horizDist > setup.AttackStandoffDistance)
        {
            moveDir = dx > 0f ? 1f : -1f;
        }
        float hoverVy = GetHoverVy();

        if (fsm.Rb != null)
        {
            fsm.Rb.linearVelocity = new Vector2(moveDir * speed, hoverVy);
        }

        if (fsm.Sr != null)
        {
            fsm.Sr.flipX = dx >= 0f;
        }
    }

    private float GetHoverVy()
    {
        float oscillation = Mathf.Sin(Time.time * setup.HoverFrequency) * setup.HoverAmplitude;
        float desiredY = hoverY + oscillation;
        float delta = desiredY - fsm.transform.position.y;
        float vy = delta * setup.HoverReturnSpeed;
        return Mathf.Clamp(vy, -setup.HoverMaxVy, setup.HoverMaxVy);
    }
}
