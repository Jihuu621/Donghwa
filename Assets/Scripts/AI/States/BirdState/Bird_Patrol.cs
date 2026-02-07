using UnityEngine;

public class Bird_Patrol : IBirdState
{
    float _movetime = 0f;
    float _moveTimer = 0f;
    bool _isPatrolling = false;
    Transform _player;
    float _detectRange = 10f; // 인식 반경

    public void EnterState(BirdManager enemy)
    {
        if (enemy.Sprite != null) enemy.Sprite.color = Color.yellow;
        _isPatrolling = false;
        _moveTimer = 0f;
        _movetime = Random.Range(1f, 2.5f);
        _player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (enemy.PatrolDirection == 0) enemy.PatrolDirection = 1;
    }

    public void ExitState(BirdManager enemy)
    {
    }

    public void UpdateState(BirdManager enemy)
    {
        if (_player != null)
        {
            float dist = Vector2.Distance(enemy.transform.position, _player.position);
            if (dist <= _detectRange)
            {
                if (enemy.Rb != null) enemy.Rb.linearVelocity = Vector2.zero;
                enemy.TransitionToState(new Bird_Chase());
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
            // 공중 호버 드리프트 + 순찰 방향 적용
            float vy = enemy.GetHoverVy();
            enemy.Rb.linearVelocity = new Vector2(dir * patrolSpeed + Mathf.Sign(dir) * enemy.HoverDriftSpeed * 0.5f, vy);
        }

        if (enemy.Sprite != null) enemy.Sprite.flipX = (dir > 0);

        _moveTimer += Time.deltaTime;

        if (_moveTimer >= _movetime)
        {
            if (enemy.Rb != null) enemy.Rb.linearVelocity = Vector2.zero;
            enemy.TransitionToState(new Bird_Idle());
        }
    }
}