using UnityEngine;

public class RabbitPatrolState : EnemyState
{
    private RabbitSetup setup;
    private float patrolMoveTimer;
    private float patrolMoveTime;
    private float hopTimer;
    private int direction;
    private Vector2 startPosition;

    public RabbitPatrolState(EnemyFSM fsm, RabbitSetup setup) : base(fsm)
    {
        this.setup = setup;
        this.startPosition = fsm.transform.position;
    }

    public override void Enter()
    {
        if (fsm.Sr != null) fsm.Sr.color = Color.yellow;
        if (fsm.Anim != null && !string.IsNullOrEmpty(setup.IdleAnimation))
        {
            fsm.Anim.Play(setup.IdleAnimation);
        }

        patrolMoveTimer = 0f;
        patrolMoveTime = Random.Range(setup.PatrolMoveTimeMin, setup.PatrolMoveTimeMax);
        hopTimer = 0f;
        direction = Random.value < 0.5f ? -1 : 1;
        SetFacing(direction > 0);
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

        float speed = fsm.Data != null ? fsm.Data.PatrolSpeed : 1f;

        if (fsm.Rb != null)
        {
            fsm.Rb.linearVelocity = new Vector2(direction * speed, fsm.Rb.linearVelocity.y);
            
            hopTimer += Time.deltaTime;
            if (hopTimer >= setup.HopInterval)
            {
                fsm.Rb.linearVelocity = new Vector2(fsm.Rb.linearVelocity.x, setup.HopForce);
                hopTimer = 0f;
            }
        }

        SetFacing(direction > 0);

        patrolMoveTimer += Time.deltaTime;
        if (patrolMoveTimer >= patrolMoveTime)
        {
            fsm.StopMovement();
            fsm.TransitionTo(fsm.Idle);
        }
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
