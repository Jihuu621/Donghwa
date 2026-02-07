using UnityEngine;

public class Rabbit_Idle : IRabbitState
{
    float _idletime;
    float _timer;

    public void EnterState(RabbitManager enemy)
    {
        // --- 애니메이션 재생 추가 ---
        // Animator 컴포넌트가 있고, "Idle" 상태가 있다면 재생합니다.
        if (enemy.Anim != null)
        {
            enemy.Anim.Play("Rabbit_Idle");
        }

        enemy.Sprite.color = Color.white;
        _idletime = Random.Range(1f, 4f);
        _timer = 0f;

        // 50% 확률로 다음 패트롤 방향 결정
        enemy.PatrolDirection = (Random.value < 0.5f) ? -1 : 1;
        // 시각적으로 방향만 미리 설정 (flipX 사용)
        enemy.Sprite.flipX = (enemy.PatrolDirection > 0);
        // 정지
        if (enemy.Rb != null) enemy.Rb.linearVelocity = Vector2.zero;
    }

    public void ExitState(RabbitManager enemy)
    {
    }

    public void UpdateState(RabbitManager enemy)
    {
        _timer += Time.deltaTime;
        if (_timer >= _idletime)
        {
            enemy.TransitionToState(new Rabbit_Patrol());
            return;
        }
    }
}