using UnityEngine;

public class ShieldEnemy_BlockState : IEnemyState
{
    float blockTime = 1.2f;
    float timer;
    bool didCounterAttack;

    public void EnterState(EnemyStateManager enemy)
    {
        timer = 0f;
        didCounterAttack = false;
        Debug.Log("[쉴드 에너미] 방어 상태");
    }

    public void ExitState(EnemyStateManager enemy) { }

    public void UpdateState(EnemyStateManager enemy)
    {
        ShieldEnemyManager manager = enemy.GetComponent<ShieldEnemyManager>();

        if (manager != null && manager.IsShieldBroken)
        {
            enemy.TransitionToState(new ShieldEnemy_ShieldBrokenState());
            return;
        }

        timer += Time.deltaTime;

        //방어 중 급습은 단 1회만
        if (!didCounterAttack && timer > 0.3f && Random.value < 0.25f)
        {
            didCounterAttack = true;
            enemy.TransitionToState(new ShieldEnemy_BlockStabState());
            return;
        }

        //방어는 반드시 끝나야 함
        if (timer >= blockTime)
        {
            enemy.TransitionToState(new ShieldEnemy_ChaseState());
        }
    }
}
