using UnityEngine;

public class PatrolState : IEnemyState
{
    float _movetime = 0f;
    float _moveTimer = 0f;
    bool _isPatrolling = false;
    Transform _player;
    float _detectRange = 10f; // 인식 반경

    public void EnterState(EnemyStateManager enemy)
    {
        enemy.GetComponent<SpriteRenderer>().color = Color.yellow;
        _isPatrolling = false;
        _moveTimer = 0f;
        _movetime = Random.Range(1f, 4f);
        _player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    public void ExitState(EnemyStateManager enemy)
    {
        // Debug.Log("[Patrol State] : Exit");
    }

    public void UpdateState(EnemyStateManager enemy)
    {
        // 플레이어 인식 반경 체크
        if (_player != null)
        {
            float dist = Vector2.Distance(enemy.transform.position, _player.position);
            if (dist <= _detectRange)
            {
                enemy.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
                enemy.TransitionToState(new ChaseState());
                return;
            }
        }

        if (!_isPatrolling)
        {
            enemy.transform.localScale = new Vector3(
                -enemy.transform.localScale.x,
                 enemy.transform.localScale.y,
                 enemy.transform.localScale.z);

            _isPatrolling = true;
        }

        Vector2 dirVec = enemy.transform.localScale.x < 0 ? Vector2.right : Vector2.left;
        _moveTimer += Time.deltaTime;

        float patrolSpeed = enemy.GetComponent<EnemyDataManager>().EnemyData.PatrolSpeed;

        enemy.GetComponent<Rigidbody2D>().linearVelocity = new Vector2(dirVec.x * patrolSpeed, 0f);

        if (_moveTimer >= _movetime)
        {
            enemy.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
            enemy.TransitionToState(new IdleState());
        }
    }
}