using UnityEngine;

public class Bird_Chase : IBirdState
{
    Transform _player;
    float _chaseSpeed;
    float _chaseRange = 15f; // 추적 유지 반경
    float _attackRange = 10f; // 공격 전환 거리

    public void EnterState(BirdManager enemy)
    {
        if (enemy.Sprite != null) enemy.Sprite.color = Color.red;
        _player = GameObject.FindGameObjectWithTag("Player")?.transform;
        _chaseSpeed = enemy.DataManager != null ? enemy.DataManager.EnemyData.MoveSpeed : 2f;
    }

    public void ExitState(BirdManager enemy)
    {
    }

    public void UpdateState(BirdManager enemy)
    {
        if (_player == null)
        {
            enemy.TransitionToState(new Bird_Idle());
            return;
        }

        float dist = Vector2.Distance(enemy.transform.position, _player.position);

        if (dist > _chaseRange)
        {
            if (enemy.Rb != null) enemy.Rb.linearVelocity = Vector2.zero;
            enemy.TransitionToState(new Bird_Patrol());
            return;
        }

        if (dist < _attackRange)
        {
            if (enemy.Rb != null) enemy.Rb.linearVelocity = Vector2.zero;
            enemy.TransitionToState(new Bird_Attack());
            return;
        }

        float moveDir = _player.position.x > enemy.transform.position.x ? 1f : -1f;

        if (enemy.Rb != null)
        {
            float vy = enemy.GetHoverVy();
            enemy.Rb.linearVelocity = new Vector2(moveDir * _chaseSpeed, vy);
        }

        if (enemy.Sprite != null) enemy.Sprite.flipX = (moveDir > 0);
    }
}