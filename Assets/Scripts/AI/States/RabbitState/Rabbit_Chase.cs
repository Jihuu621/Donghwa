using UnityEngine;

public class Rabbit_Chase : IRabbitState
{
    Transform _player;
    float _chaseSpeed;
    float _chaseRange = 15f; // 추적 유지 반경
    float _attackRange = 1.5f; // 공격 전환 거리

    float _hopTimer = 0f;

    public void EnterState(RabbitManager enemy)
    {
        if (enemy.Sprite != null) enemy.Sprite.color = Color.red;
        _player = GameObject.FindGameObjectWithTag("Player")?.transform;
        _chaseSpeed = enemy.DataManager != null ? enemy.DataManager.EnemyData.MoveSpeed : 2f;
        _hopTimer = 0f;
    }

    public void ExitState(RabbitManager enemy)
    {
    }

    public void UpdateState(RabbitManager enemy)
    {
        if (_player == null)
        {
            enemy.TransitionToState(new Rabbit_Idle());
            return;
        }

        float dist = Vector2.Distance(enemy.transform.position, _player.position);

        // 범위를 벗어나면 Patrol
        if (dist > _chaseRange)
        {
            if (enemy.Rb != null) enemy.Rb.linearVelocity = Vector2.zero;
            enemy.TransitionToState(new Rabbit_Patrol());
            return;
        }

        // 가까우면 Attack으로 전환
        if (dist < _attackRange)
        {
            if (enemy.Rb != null) enemy.Rb.linearVelocity = Vector2.zero;
            enemy.TransitionToState(new Rabbit_Attack());
            return;
        }

        // 플레이어 방향으로 이동
        float moveDir = _player.position.x > enemy.transform.position.x ? 1f : -1f;

        if (enemy.Rb != null)
        {
            enemy.Rb.linearVelocity = new Vector2(moveDir * _chaseSpeed, enemy.Rb.linearVelocity.y);
        }

        if (enemy.Sprite != null) enemy.Sprite.flipX = (moveDir > 0);

        // 깡총거림 적용
        enemy.TryHop(ref _hopTimer);
    }
}