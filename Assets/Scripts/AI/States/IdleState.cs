using UnityEngine;

public class IdleState : IEnemyState
{

    float _idletime;
    float _timer;

    public void EnterState(EnemyStateManager enemy)
    {
        enemy.GetComponent<SpriteRenderer>().color = Color.white;
        _idletime = Random.Range(1f, 4f);
        _timer = 0f;
    }

    public void ExitState(EnemyStateManager enemy)
    {
        //Debug.Log("[Idle State] : Exit");
    }

    public void UpdateState(EnemyStateManager enemy)
    {
        _timer += Time.deltaTime;
        if (_timer >= _idletime) {
            enemy.TransitionToState(new PatrolState());
            return;
        }
    }
}
