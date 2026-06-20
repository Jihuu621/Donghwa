using UnityEngine;

public class StunState : EnemyState
{
    private float stunTime;
    private float timer;
    private Color originalColor;

    public StunState(EnemyFSM fsm, float stunTime = 1.2f) : base(fsm)
    {
        this.stunTime = stunTime;
    }

    public override void Enter()
    {
        fsm.StopMovement();
        
        if (fsm.Sr != null)
        {
            originalColor = fsm.Sr.color;
            fsm.Sr.color = Color.blue;
        }

        timer = 0f;
        Debug.Log("<color=blue>[ENEMY] STUNNED!</color>");
    }

    public override void Execute()
    {
        timer += Time.deltaTime;

        if (timer >= stunTime)
        {
            Debug.Log("<color=white>[ENEMY] STUN RECOVER</color>");
            if (fsm.Chase != null)
                fsm.TransitionTo(fsm.Chase);
        }
    }

    public override void Exit()
    {
        if (fsm.Sr != null)
        {
            fsm.Sr.color = originalColor;
        }
    }
}
