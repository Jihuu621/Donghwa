using UnityEngine;

public class ChaseState : IEnemyState
{
    Transform _player;
    float _chaseSpeed;
    float _chaseRange = 15f;
    float _attackRange = 1.5f;

    ShieldEnemyManager _shieldEnemy;

    public void EnterState(EnemyStateManager enemy)
    {
        enemy.GetComponent<SpriteRenderer>().color = Color.red;
        _player = GameObject.FindGameObjectWithTag("Player")?.transform;
        _chaseSpeed = enemy.GetComponent<EnemyDataManager>().EnemyData.MoveSpeed;

        _shieldEnemy = enemy.GetComponent<ShieldEnemyManager>();
    }

    public void ExitState(EnemyStateManager enemy) { }

    public void UpdateState(EnemyStateManager enemy)
    {
        if (_player == null)
        {
            enemy.TransitionToState(new IdleState());
            return;
        }

        float dist = Vector2.Distance(enemy.transform.position, _player.position);

        if (dist > _chaseRange)
        {
            enemy.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
            enemy.TransitionToState(new PatrolState());
            return;
        }

        if (dist < _attackRange)
        {
            enemy.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;

            if (_shieldEnemy != null)
            {
                Debug.Log("<color=cyan>[Chase ¡æ ShieldEnemy_AttackState]</color>");
                enemy.TransitionToState(new ShieldEnemy_AttackState());
            }
            else
            {
                enemy.TransitionToState(new AttackState());
            }
            return;
        }

        float moveDir = _player.position.x > enemy.transform.position.x ? 1f : -1f;

        if (enemy.TryGetComponent<Rigidbody2D>(out var rb))
            rb.linearVelocity = new Vector2(moveDir * _chaseSpeed, 0f);

        if (enemy.TryGetComponent<SpriteRenderer>(out var sr))
            sr.flipX = moveDir < 0;
    }
}
