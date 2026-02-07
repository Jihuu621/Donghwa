using UnityEngine;

public class ShieldEnemy_BlockStabState : IEnemyState
{
    float timer;
    bool hasAttacked;

    public void EnterState(EnemyStateManager enemy) { timer = 0f; hasAttacked = false; Debug.Log("방어 중 기습!"); }
    public void ExitState(EnemyStateManager enemy) { }

    public void UpdateState(EnemyStateManager enemy)
    {
        timer += Time.deltaTime;
        if (!hasAttacked && timer >= 0.2f) 
        {
            enemy.PerformAttack(1.8f);
            hasAttacked = true;
        }
        if (timer >= 0.5f) enemy.TransitionToState(new ShieldEnemy_ChaseState());
    }
}