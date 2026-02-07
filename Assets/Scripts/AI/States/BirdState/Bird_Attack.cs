using UnityEngine;

/// 공격: 평소에는 공중 호버, 플레이어가 근접하면 중력을 낮춰 활강하듯 돌진하여 반대편까지 넘어감
public class Bird_Attack : IBirdState
{
    float _attackRange = 10f;
    Transform _player;
    float _prepTimer = 0f;
    float _lungeTimer = 0f;
    bool _isLunging = false;
    Vector2 _targetPos;

    // 활강 파라미터
    float _baseVx = 0f;
    float _initialVy = 0f;
    float _lungeTime = 0f;

    // 공격 데미지 중복 방지
    bool _hasDealtDamage = false;

    public void EnterState(BirdManager enemy)
    {
        if (enemy.Sprite != null) enemy.Sprite.color = Color.magenta;
        _player = GameObject.FindGameObjectWithTag("Player")?.transform;
        _prepTimer = 0f;
        _lungeTimer = 0f;
        _isLunging = false;
        _targetPos = Vector2.zero;
        _baseVx = 0f;
        _initialVy = 0f;
        _lungeTime = 0f;
        _hasDealtDamage = false;
        if (enemy.Rb != null) enemy.Rb.linearVelocity = Vector2.zero;
    }

    public void ExitState(BirdManager enemy)
    {
        if (enemy.Rb != null)
        {
            enemy.Rb.gravityScale = enemy.DefaultGravityScale;
        }
    }

    public void UpdateState(BirdManager enemy)
    {
        if (_player == null)
        {
            enemy.TransitionToState(new Bird_Idle());
            return;
        }

        float dist = Vector2.Distance(enemy.transform.position, _player.position);

        // 너무 멀면 추적 복귀
        if (dist > _attackRange * 2f && !_isLunging)
        {
            if (enemy.Rb != null) enemy.Rb.linearVelocity = Vector2.zero;
            enemy.TransitionToState(new Bird_Chase());
            return;
        }

        if (!_isLunging)
        {
            // 활강 준비: 플레이어 바라보고 잠깐 멈춤
            _prepTimer += Time.deltaTime;
            if (enemy.Rb != null)
            {
                float vy = enemy.GetHoverVy();
                enemy.Rb.linearVelocity = new Vector2(0f, vy);
            }
            if (enemy.Sprite != null) enemy.Sprite.flipX = (_player.position.x > enemy.transform.position.x);

            if (_prepTimer >= enemy.AttackPrepTime)
            {
                Vector2 startPos = enemy.transform.position;
                Vector2 playerPosAtStart = _player.position;

                // 플레이어를 지나 훨씬 멀리 목표 설정 (Y 오프셋으로 포물선 하강 느낌)
                float farMultiplier = enemy.LungeDistanceMultiplierFar;
                float offsetX = (playerPosAtStart.x - startPos.x) * farMultiplier;
                float targetX = startPos.x + offsetX;
                float targetY = startPos.y + enemy.LungeTargetYOffset;
                _targetPos = new Vector2(targetX, targetY);

                // 돌진 시간
                _lungeTime = Mathf.Max(0.01f, enemy.LungeDuration);

                // 목표까지 벡터 및 시간
                Vector2 toTarget = _targetPos - startPos;
                float dx = toTarget.x;
                float dy = toTarget.y;
                float t = _lungeTime;

                // 활강: 중력 낮추기 후 투사체 공식으로 초기 속도 계산
                if (enemy.Rb != null)
                {
                    enemy.Rb.gravityScale = enemy.DefaultGravityScale * 0.25f;
                }
                float gravity = Physics2D.gravity.y * (enemy.Rb != null ? enemy.Rb.gravityScale : 1f);

                float vx = dx / t;
                float vy = (dy - 0.5f * gravity * t * t) / t;
                vy *= Mathf.Clamp01(enemy.LungeArcLowFactor);

                _baseVx = vx;
                _initialVy = vy;

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
            // 활강 돌진 중: 수평 가속으로 속도감
            _lungeTimer += Time.deltaTime;
            float t = _lungeTime;
            float progress = Mathf.Clamp01(_lungeTimer / t);
            float speedMultiplier = Mathf.Lerp(1f, 1f + enemy.LungeAccel, progress);

            if (enemy.Rb != null)
            {
                float vxNow = _baseVx * speedMultiplier;
                enemy.Rb.linearVelocity = new Vector2(vxNow, enemy.Rb.linearVelocity.y);
            }

            // 공격 도중 플레이어에 닿으면 데미지 적용하되 멈추지 않음
            if (!_hasDealtDamage)
            {
                float duringDist = Vector2.Distance(enemy.transform.position, _player.position);
                if (duringDist <= _attackRange)
                {
                    var playerHealth = _player.GetComponent<Health>();
                    float dmg = (enemy.DataManager != null) ? enemy.DataManager.EnemyData.Damage : 1f;
                    if (playerHealth != null)
                    {
                        playerHealth.TakeDamage(dmg);
                    }
                    _hasDealtDamage = true;
                    // 의도적으로 상태 전환이나 속도 제어 하지 않음 -> 계속해서 목표 지점까지 돌진
                }
            }

            // 종료 시점
            if (_lungeTimer >= _lungeTime)
            {
                // 중력 복구
                if (enemy.Rb != null)
                {
                    enemy.Rb.gravityScale = enemy.DefaultGravityScale;
                    enemy.Rb.linearVelocity = Vector2.zero;
                }
                _isLunging = false;

                // 도달 여부 체크 (도달 여부와 관계없이 이미 지나쳤으므로 추적 상태로 전환)
                enemy.TransitionToState(new Bird_Chase());
                return;
            }
        }
    }
}