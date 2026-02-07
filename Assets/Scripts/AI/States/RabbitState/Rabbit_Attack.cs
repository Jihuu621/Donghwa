using UnityEngine;

/// 공격: 준비 후 준비 시점의 플레이어 위치를 기준으로 더 멀리 목표를 잡고
/// 낮은 점프 궤적으로 돌진. 돌진 중 수평 가속이 붙어서 속도감 있게 이동.
public class Rabbit_Attack : IRabbitState
{
    float _attackRange = 1.5f;
    Transform _player;
    float _prepTimer = 0f;
    float _lungeTimer = 0f;
    bool _isLunging = false;
    Vector2 _targetPos;
    float _hopTimer = 0f;

    // 누락되어 있던 필드들
    float _baseVx = 0f;
    float _initialVy = 0f;
    float _lungeTime = 0f;

    public void EnterState(RabbitManager enemy)
    {
        if (enemy.Sprite != null) enemy.Sprite.color = Color.magenta;
        _player = GameObject.FindGameObjectWithTag("Player")?.transform;
        _prepTimer = 0f;
        _lungeTimer = 0f;
        _isLunging = false;
        _targetPos = Vector2.zero;
        _hopTimer = 0f;
        _baseVx = 0f;
        _initialVy = 0f;
        _lungeTime = 0f;
        if (enemy.Rb != null) enemy.Rb.linearVelocity = Vector2.zero;
    }

    public void ExitState(RabbitManager enemy)
    {
        // 상태 종료시 별도 처리 없음
    }

    public void UpdateState(RabbitManager enemy)
    {
        if (_player == null)
        {
            enemy.TransitionToState(new Rabbit_Idle());
            return;
        }

        float dist = Vector2.Distance(enemy.transform.position, _player.position);

        // 만약 너무 멀어졌다면 추적 상태로 복귀
        if (dist > _attackRange * 2f)
        {
            if (enemy.Rb != null) enemy.Rb.linearVelocity = Vector2.zero;
            enemy.TransitionToState(new Rabbit_Chase());
            return;
        }

        if (!_isLunging)
        {
            // 준비: 멈춰서 플레이어 바라보기
            _prepTimer += Time.deltaTime;
            if (enemy.Rb != null) enemy.Rb.linearVelocity = Vector2.zero;
            if (enemy.Sprite != null) enemy.Sprite.flipX = (_player.position.x > enemy.transform.position.x);

            // 준비 중에는 긴장감의 약한 깡총 유지 (원하면 비활성화 가능)
            enemy.TryHop(ref _hopTimer);

            if (_prepTimer >= enemy.AttackPrepTime)
            {
                // 준비 완료 -> 목표 계산 (더 멀리)
                Vector2 startPos = enemy.transform.position;
                Vector2 playerPosAtStart = _player.position;
                // 목표는 플레이어 방향으로 더 멀리(배수)
                float offsetX = (playerPosAtStart.x - startPos.x) * enemy.LungeDistanceMultiplier;
                float targetX = startPos.x + offsetX;
                _targetPos = new Vector2(targetX, startPos.y);

                // 돌진 시간
                _lungeTime = Mathf.Max(0.01f, enemy.LungeDuration);

                // 물리 기반 초기 속도 계산 (포물선), 하지만 수직 성분은 낮게 유지
                Vector2 toTarget = _targetPos - startPos;
                float dx = toTarget.x;
                float dy = toTarget.y;
                float t = _lungeTime;
                float gravity = Physics2D.gravity.y * (enemy.Rb != null ? enemy.Rb.gravityScale : 1f); // 음수
                float vx = dx / t;
                float vy = (dy - 0.5f * gravity * t * t) / t;

                // 낮은 점프: 수직 성분 감소
                vy *= Mathf.Clamp01(enemy.LungeArcLowFactor);

                _baseVx = vx;         // 기준 수평속도
                _initialVy = vy;      // 초기 수직속도

                // 시작 속도 설정
                if (enemy.Rb != null)
                {
                    enemy.Rb.linearVelocity = new Vector2(_baseVx, _initialVy);
                }

                _isLunging = true;
                _lungeTimer = 0f;
            }
        }
        else
        {
            // 돌진 중: 수평 가속 적용하여 속도감 증가
            _lungeTimer += Time.deltaTime;
            float t = _lungeTime;
            float progress = Mathf.Clamp01(_lungeTimer / t);
            // 속도계수: 1 -> 1 + LungeAccel (선형 증가). 튜닝 가능.
            float speedMultiplier = Mathf.Lerp(1f, 1f + enemy.LungeAccel, progress);

            if (enemy.Rb != null)
            {
                // 수평속도에 가속 적용. 수직은 기존 물리 영향에 맡김(중력)
                float vxNow = _baseVx * speedMultiplier;
                enemy.Rb.linearVelocity = new Vector2(vxNow, enemy.Rb.linearVelocity.y);
            }

            // 돌진 시간 종료
            if (_lungeTimer >= _lungeTime)
            {
                if (enemy.Rb != null) enemy.Rb.linearVelocity = Vector2.zero;
                _isLunging = false;

                // 도달 여부 판단: 플레이어 가까우면 데미지 적용(있으면)
                float postDist = Vector2.Distance(enemy.transform.position, _player.position);
                if (postDist <= _attackRange)
                {
                    var playerHealth = _player.GetComponent<Health>();
                    float dmg = (enemy.DataManager != null) ? enemy.DataManager.EnemyData.Damage : 1f;
                    if (playerHealth != null)
                    {
                        playerHealth.TakeDamage(dmg);
                    }
                    enemy.TransitionToState(new Rabbit_Idle());
                }
                else
                {
                    enemy.TransitionToState(new Rabbit_Chase());
                }
                return;
            }
        }

        // 준비 중일 때만 TryHop 호출(돌진 중엔 호핑을 따로 하지 않음)
        if (!_isLunging)
            enemy.TryHop(ref _hopTimer);
    }
}