using UnityEngine;

public class StunState : IEnemyState
{
    float stunTime = 1.2f;
    float timer = 0f;

    public void EnterState(EnemyStateManager enemy)
    {
        enemy.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        enemy.GetComponent<SpriteRenderer>().color = Color.blue;

        Debug.Log("<color=blue>[ENEMY] STUNNED!</color>");
    }

    public void UpdateState(EnemyStateManager enemy)
    {
        timer += Time.deltaTime;

        if (timer >= stunTime)
        {
            Debug.Log("<color=white>[ENEMY] STUN RECOVER</color>");
            enemy.TransitionToState(new ChaseState());
        }
    }

    public void ExitState(EnemyStateManager enemy)
    {
        // Stun 끝날 때 원래 색 복구
        enemy.GetComponent<SpriteRenderer>().color = Color.white;
    }
}
