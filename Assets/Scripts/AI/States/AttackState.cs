using UnityEngine;

public class AttackState : IEnemyState
{
    IEnemyAttackBehavior _behavior;

    float _attackRange = 1.5f;
    float _attackDelay = 1.0f;
    float _timer = 0f;

    Transform _player;
    float _damage;

    public void EnterState(EnemyStateManager enemy)
    {
        _behavior = enemy.AttackBehavior;
        if (_behavior != null)
        {
            _behavior.EnterState(enemy);
            return;
        }

        enemy.GetComponent<SpriteRenderer>().color = Color.magenta;

        _player = GameObject.FindGameObjectWithTag("Player")?.transform;
        _damage = enemy.GetComponent<EnemyDataManager>().EnemyData.Damage;
        _timer = 0f;
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
        if (dist > _attackRange)
        {
            enemy.TransitionToState(new ChaseState());
            return;
        }

        _timer += Time.deltaTime;
        if (_timer >= _attackDelay)
        {
            Debug.Log("<color=#ff6666>[적] 공격 시도!</color>");

            Health playerHealth = _player.GetComponent<Health>();
            if (playerHealth != null)
            {
                float finalDamage = _damage;

                PlayerParry parry = _player.GetComponent<PlayerParry>();
                if (parry != null)
                {
                    finalDamage = parry.OnHit(_damage);
                }

                if (finalDamage > 0f)
                {
                    playerHealth.TakeDamage(finalDamage);
                    Debug.Log($"<color=red>[적] 플레이어 피해: {finalDamage}</color>");
                }
                else
                {
                    Debug.Log("<color=green>[적] 공격이 무효화되었습니다! (피해 0)</color>");
                }
            }

            _timer = 0f;
        }
    }
}
