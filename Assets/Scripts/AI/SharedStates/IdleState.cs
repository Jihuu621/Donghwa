using UnityEngine;

public class IdleState : EnemyState
{
    private float _idleTime;
    private float _timer;

    public IdleState(EnemyFSM fsm) : base(fsm) { }

    public override void Enter()
    {
        if (fsm.Sr != null)
        {
            fsm.Sr.color = Color.white;
        }

        _idleTime = Random.Range(1f, 4f);
        _timer = 0f;
        
        fsm.StopMovement();
    }

    public override void Execute()
    {
        _timer += Time.deltaTime;
        if (_timer >= _idleTime)
        {
            if (fsm.Patrol != null)
                fsm.TransitionTo(fsm.Patrol);
        }
    }
}
