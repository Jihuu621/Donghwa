public interface IEnemyIdleBehavior
{
    void EnterState(EnemyStateManager enemy);
    void UpdateState(EnemyStateManager enemy);
    void ExitState(EnemyStateManager enemy);
}

public interface IEnemyPatrolBehavior
{
    void EnterState(EnemyStateManager enemy);
    void UpdateState(EnemyStateManager enemy);
    void ExitState(EnemyStateManager enemy);
}

public interface IEnemyChaseBehavior
{
    void EnterState(EnemyStateManager enemy);
    void UpdateState(EnemyStateManager enemy);
    void ExitState(EnemyStateManager enemy);
}

public interface IEnemyAttackBehavior
{
    void EnterState(EnemyStateManager enemy);
    void UpdateState(EnemyStateManager enemy);
    void ExitState(EnemyStateManager enemy);
}
