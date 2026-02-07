using UnityEngine;

public class ShieldEnemy_SlamState : IEnemyState
{
    float timer;
    float attackDelay = 0.7f; //선딜
    bool hasAttacked;

    public void EnterState(EnemyStateManager enemy) { timer = 0f; hasAttacked = false; Debug.Log("내려치기!"); }
    public void ExitState(EnemyStateManager enemy) { }

    public void UpdateState(EnemyStateManager enemy)
    {
        timer += Time.deltaTime;
        if (!hasAttacked && timer >= attackDelay)
        {
            enemy.PerformAttack(1.5f); // 내려치기는 사거리가 짧음
            hasAttacked = true;
        }
        if (timer >= attackDelay + 0.5f) enemy.TransitionToState(new ShieldEnemy_ChaseState());
    }
}