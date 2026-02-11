using UnityEngine;

public class IdleState : IEnemyState
{
    IEnemyIdleBehavior _behavior;

    float _idletime;
    float _timer;

    public void EnterState(EnemyStateManager enemy)
    {
        _behavior = enemy.IdleBehavior;
        if (_behavior != null)
        {
            _behavior.EnterState(enemy);
            return;
        }

        enemy.GetComponent<SpriteRenderer>().color = Color.white;
        _idletime = Random.Range(1f, 4f);
        _timer = 0f;
    }

    public void ExitState(EnemyStateManager enemy)
    {
        if (_behavior != null)
        {
            _behavior.ExitState(enemy);
            return;
        }
    }

    public void UpdateState(EnemyStateManager enemy)
    {
        if (_behavior != null)
        {
            _behavior.UpdateState(enemy);
            return;
        }

        _timer += Time.deltaTime;
        if (_timer >= _idletime) {
            enemy.TransitionToState(new PatrolState());
            return;
        }
    }
}
