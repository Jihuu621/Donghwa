using UnityEngine;

public class Rabbit_Patrol : IRabbitState
{
    float _movetime = 0f;
    float _moveTimer = 0f;
    bool _isPatrolling = false;
    Transform _player;
    float _detectRange = 10f; // 인식 반경

    float _hopTimer = 0f;

    public void EnterState(RabbitManager enemy)
    {
        if (enemy.Sprite != null) enemy.Sprite.color = Color.yellow;
        _isPatrolling = false;
        _moveTimer = 0f;
        _movetime = Random.Range(1f, 2.5f); // 너무 오래 움직이지 않도록 제한
        _player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (enemy.PatrolDirection == 0) enemy.PatrolDirection = 1;
    }

    public void ExitState(RabbitManager enemy)
    {
    }

    public void UpdateState(RabbitManager enemy)
    {
        // 플레이어 인식 체크
        if (_player != null)
        {
            float dist = Vector2.Distance(enemy.transform.position, _player.position);
            if (dist <= _detectRange)
            {
                if (enemy.Rb != null) enemy.Rb.linearVelocity = Vector2.zero;
                enemy.TransitionToState(new Rabbit_Chase());
                return;
            }
        }

        if (!_isPatrolling)
        {
            _isPatrolling = true;
        }

        float dir = enemy.PatrolDirection;
        Vector2 toStart = (Vector2)enemy.transform.position - enemy.StartPosition;
        if (Mathf.Abs(toStart.x) >= enemy.PatrolRadius)
        {
            dir = -Mathf.Sign(toStart.x);
            enemy.PatrolDirection = (int)dir;
        }

        float patrolSpeed = enemy.DataManager != null ? enemy.DataManager.EnemyData.PatrolSpeed : 1f;
        if (enemy.Rb != null)
        {
            enemy.Rb.linearVelocity = new Vector2(dir * patrolSpeed, enemy.Rb.linearVelocity.y);
        }

        if (enemy.Sprite != null) enemy.Sprite.flipX = (dir > 0);

        _moveTimer += Time.deltaTime;

    
        // 깡총거림 적용
        enemy.TryHop(ref _hopTimer);

        if (_moveTimer >= _movetime)
        {
            if (enemy.Rb != null) enemy.Rb.linearVelocity = Vector2.zero;
            enemy.TransitionToState(new Rabbit_Idle());
        }
    }
}