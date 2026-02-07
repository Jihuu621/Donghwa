using UnityEngine;

public class Bird_Idle : IBirdState
{
    float _idletime;
    float _timer;

    public void EnterState(BirdManager enemy)
    {
        enemy.Sprite.color = Color.white;
        _idletime = Random.Range(1f, 4f);
        _timer = 0f;

        enemy.PatrolDirection = (Random.value < 0.5f) ? -1 : 1;
        enemy.Sprite.flipX = (enemy.PatrolDirection > 0);

        if (enemy.Rb != null)
        {
            // 공중 호버 상태로 전환: 중력 유지, 수평 이동은 없고 수직은 호버로만
            enemy.Rb.linearVelocity = Vector2.zero;
        }
    }

    public void ExitState(BirdManager enemy)
    {
    }

    public void UpdateState(BirdManager enemy)
    {
        _timer += Time.deltaTime;
        if (_timer >= _idletime)
        {
            enemy.TransitionToState(new Bird_Patrol());
            return;
        }

        // 공중 호버: 약한 수직 진동만 적용
        if (enemy.Rb != null)
        {
            float vy = enemy.GetHoverVy();
            enemy.Rb.linearVelocity = new Vector2(0f, vy);
        }
    }
}
