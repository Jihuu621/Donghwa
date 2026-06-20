using UnityEngine;

public abstract class EnemyState
{
    protected readonly EnemyFSM fsm;

    protected EnemyState(EnemyFSM fsm)
    {
        this.fsm = fsm;
    }

    public virtual void Enter() { }
    
    public abstract void Execute();
    
    public virtual void Exit() { }
}
