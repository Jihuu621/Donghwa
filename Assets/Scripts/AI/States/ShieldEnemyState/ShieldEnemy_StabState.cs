using UnityEngine;

public class ShieldEnemy_StabState : IEnemyState
{
    float timer;
    float attackDelay = 0.4f; //선딜
    bool hasAttacked;

    public void EnterState(EnemyStateManager enemy) { timer = 0f; hasAttacked = false; Debug.Log("찌르기 준비!"); }
    public void ExitState(EnemyStateManager enemy) { }

    public void UpdateState(EnemyStateManager enemy)
    {
        timer += Time.deltaTime;
        if (!hasAttacked && timer >= attackDelay)
        {
            enemy.PerformAttack(2.0f); //실공격판정
            hasAttacked = true;
        }
        if (timer >= attackDelay + 0.3f) enemy.TransitionToState(new ShieldEnemy_ChaseState());
    }
}