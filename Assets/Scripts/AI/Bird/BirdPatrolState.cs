using UnityEngine;

public class BirdPatrolState : EnemyState
{
    private BirdSetup setup;
    private float patrolMoveTimer;
    private float patrolMoveTime;
    private int direction;
    private Vector2 startPosition;
    private float hoverY;
    private float defaultGravityScale;

    public BirdPatrolState(EnemyFSM fsm, BirdSetup setup) : base(fsm)
    {
        this.setup = setup;
        this.startPosition = fsm.transform.position;
        if (fsm.Rb != null) defaultGravityScale = fsm.Rb.gravityScale;
    }

    public override void Enter()
    {
        if (fsm.Sr != null) fsm.Sr.color = Color.yellow;

        patrolMoveTimer = 0f;
        patrolMoveTime = Random.Range(setup.PatrolMoveTimeMin, setup.PatrolMoveTimeMax);
        direction = Random.value < 0.5f ? -1 : 1;
        hoverY = startPosition.y;
    }

    public override void Execute()
    {
        if (fsm.Player != null)
        {
            float dist = Vector2.Distance(fsm.transform.position, fsm.Player.position);
            if (dist <= setup.DetectRange)
            {
                fsm.StopMovement();
                fsm.TransitionTo(fsm.Chase);
                return;
            }
        }

        Vector2 toStart = (Vector2)fsm.transform.position - startPosition;
        if (Mathf.Abs(toStart.x) >= setup.PatrolRadius)
        {
            direction = (int)-Mathf.Sign(toStart.x);
        }

        float speed = fsm.Data != null ? fsm.Data.PatrolSpeed : setup.HoverDriftSpeed;
        float hoverVy = GetHoverVy();

        if (fsm.Rb != null)
        {
            fsm.Rb.linearVelocity = new Vector2(direction * speed, hoverVy);
        }

        if (fsm.Sr != null)
        {
            fsm.Sr.flipX = direction > 0;
        }

        patrolMoveTimer += Time.deltaTime;
        if (patrolMoveTimer >= patrolMoveTime)
        {
            fsm.StopMovement();
            fsm.TransitionTo(fsm.Idle);
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
