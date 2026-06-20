using UnityEngine;

public class ShieldPatrolState : EnemyState
{
    private ShieldEnemySetup setup;
    private float patrolMoveTimer;
    private float patrolMoveTime;
    private int direction;
    private Vector2 originScale;

    public ShieldPatrolState(EnemyFSM fsm, ShieldEnemySetup setup) : base(fsm)
    {
        this.setup = setup;
        this.originScale = fsm.transform.localScale;
    }

    public override void Enter()
    {
        if (fsm.Sr != null) fsm.Sr.color = Color.yellow;
        
        patrolMoveTimer = 0f;
        patrolMoveTime = Random.Range(setup.PatrolMoveTimeMin, setup.PatrolMoveTimeMax);
        direction = Random.value < 0.5f ? -1 : 1;

        ApplyFacing(direction);
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

        float speed = fsm.Data != null ? fsm.Data.PatrolSpeed : 1f;

        if (fsm.Rb != null)
        {
            fsm.Rb.linearVelocity = new Vector2(direction * speed, fsm.Rb.linearVelocity.y);
        }

        patrolMoveTimer += Time.deltaTime;
        if (patrolMoveTimer >= patrolMoveTime)
        {
            fsm.StopMovement();
            fsm.TransitionTo(fsm.Idle);
        }
    }

    private void ApplyFacing(float dir)
    {
        fsm.transform.localScale = new Vector3(
            Mathf.Abs(originScale.x) * Mathf.Sign(dir),
            originScale.y
        );
    }
}
