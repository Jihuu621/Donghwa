using UnityEngine;

public class ChaseState : IEnemyState
{
    IEnemyChaseBehavior _behavior;

    Transform _player;
    float _chaseSpeed;
    float _chaseRange = 15f;
    float _attackRange = 1.5f;

    public void EnterState(EnemyStateManager enemy)
    {
        _behavior = enemy.ChaseBehavior;
        if (_behavior != null)
        {
            _behavior.EnterState(enemy);
            return;
        }

        enemy.GetComponent<SpriteRenderer>().color = Color.red;
        _player = GameObject.FindGameObjectWithTag("Player")?.transform;
        _chaseSpeed = enemy.GetComponent<EnemyDataManager>().EnemyData.MoveSpeed;

    }

    public void ExitState(EnemyStateManager enemy)
    {
        if (_behavior != null)
        {
            _behavior.ExitState(enemy);
        }
    }

    public void UpdateState(EnemyStateManager enemy)
    {
        if (_behavior != null)
        {
            _behavior.UpdateState(enemy);
            return;
        }

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
            enemy.TransitionToState(new AttackState());
            return;
        }

        float moveDir = _player.position.x > enemy.transform.position.x ? 1f : -1f;

        if (enemy.TryGetComponent<Rigidbody2D>(out var rb))
            rb.linearVelocity = new Vector2(moveDir * _chaseSpeed, 0f);

        if (enemy.TryGetComponent<SpriteRenderer>(out var sr))
            sr.flipX = moveDir < 0;
    }
}
