using UnityEngine;

public class ShieldEnemy_ChaseState : IEnemyState
{
    private Transform _player;

    private float _chaseSpeed;
    private float _chaseRange = 15f;
    private float _attackRange = 1.6f;

    private Vector3 _originScale; //  초기 스케일 저장

    public void EnterState(EnemyStateManager enemy)
    {
        _player = GameObject.FindGameObjectWithTag("Player")?.transform;

        var data = enemy.GetComponent<EnemyDataManager>();
        _chaseSpeed = data != null ? data.EnemyData.MoveSpeed : 2.5f;

        //  최초 스케일 저장
        _originScale = enemy.transform.localScale;

        if (enemy.TryGetComponent<SpriteRenderer>(out var sr))
            sr.color = Color.red;
    }

    public void ExitState(EnemyStateManager enemy)
    {
        if (enemy.TryGetComponent<Rigidbody2D>(out var rb))
            rb.linearVelocity = Vector2.zero;
    }

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
            enemy.TransitionToState(new PatrolState());
            return;
        }

        if (dist < _attackRange)
        {
            enemy.TransitionToState(new ShieldEnemy_AttackState());
            return;
        }

        float dir = _player.position.x > enemy.transform.position.x ? 1f : -1f;

        if (enemy.TryGetComponent<Rigidbody2D>(out var rb))
            rb.linearVelocity = new Vector2(dir * _chaseSpeed, rb.linearVelocity.y);

        // X만 뒤집고 Y/Z는 원본 유지
        enemy.transform.localScale = new Vector3(
            Mathf.Abs(_originScale.x) * dir,
            _originScale.y,
            _originScale.z
        );
    }
}
