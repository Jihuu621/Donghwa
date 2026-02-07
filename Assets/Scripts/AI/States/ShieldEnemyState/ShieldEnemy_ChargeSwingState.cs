using UnityEngine;

public class ShieldEnemy_ChargeSwingState : IEnemyState
{
    float timer;
    Rigidbody2D rb;
    float chargeSpeed = 5f;

    public void EnterState(EnemyStateManager enemy)
    {
        timer = 0f;
        rb = enemy.GetComponent<Rigidbody2D>();
        Debug.Log("돌진 공격!");
    }

    public void UpdateState(EnemyStateManager enemy)
    {
        timer += Time.deltaTime;

        // 0.5초 동안 돌진 후 공격
        if (timer < 0.5f)
        {
            float dir = enemy.transform.localScale.x > 0 ? 1f : -1f;
            rb.linearVelocity = new Vector2(dir * chargeSpeed, rb.linearVelocity.y);
        }
        else if (timer >= 0.5f && timer < 0.6f)
        {
            rb.linearVelocity = Vector2.zero;
            enemy.PerformAttack(2.2f); // 돌진 힘을 실어 넓은 사거리
        }
        else if (timer >= 1.0f)
        {
            enemy.TransitionToState(new ShieldEnemy_ChaseState());
        }
    }
    public void ExitState(EnemyStateManager enemy) { }
}