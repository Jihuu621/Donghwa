using UnityEngine;

public class ShieldEnemy_ShieldBrokenState : IEnemyState
{
    public void EnterState(EnemyStateManager enemy)
    {
        Debug.Log("[쉴드 에너미] 방패 파괴 → 공격적 전환");
    }

    public void ExitState(EnemyStateManager enemy) { }

    public void UpdateState(EnemyStateManager enemy)
    {
        enemy.TransitionToState(new ShieldEnemy_AttackState());
    }
}
