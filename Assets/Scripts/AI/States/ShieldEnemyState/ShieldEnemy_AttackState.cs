using UnityEngine;

public class ShieldEnemy_AttackState : IEnemyState
{
    float cool = 1.2f;
    float timer;

    public void EnterState(EnemyStateManager enemy)
    {
        timer = 0f;
    }

    public void ExitState(EnemyStateManager enemy) { }

    public void UpdateState(EnemyStateManager enemy)
    {
        Debug.Log("ShieldEnemy_AttackState Update 진입");
        timer += Time.deltaTime;
        if (timer < cool) return;
        Debug.Log(" 공격 쿨타임 완료");

        ShieldEnemyManager manager = enemy.GetComponent<ShieldEnemyManager>();

        // 방어 상태 확률
        if (!manager.IsShieldBroken && Random.value < 0.4f)
        {
            enemy.TransitionToState(new ShieldEnemy_BlockState());
            return;
        }

        // 공격 패턴 랜덤
        int roll = Random.Range(0, 3);

        if (roll == 0) enemy.TransitionToState(new ShieldEnemy_StabState());
        else if (roll == 1) enemy.TransitionToState(new ShieldEnemy_SlamState());
        else enemy.TransitionToState(new ShieldEnemy_ChargeSwingState());
    }
}
