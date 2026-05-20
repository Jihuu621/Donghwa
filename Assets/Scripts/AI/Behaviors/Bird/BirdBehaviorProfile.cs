using UnityEngine;

public class BirdBehaviorProfile : MonoBehaviour, IEnemyIdleBehavior, IEnemyPatrolBehavior, IEnemyChaseBehavior, IEnemyAttackBehavior
{
    [Header("Bird Settings")]
    public float PatrolRadius = 3f;
    public float AttackPrepTime = 0.5f;
    public float LungeDuration = 0.4f;

    [Header("Lunge Tuning")]
    public float LungeDistanceMultiplierFar = 3.0f;
    public float LungeTargetYOffset = -0.5f;
    public float LungeAccel = 1.2f;
    public float LungeArcLowFactor = 0.5f;
    [Tooltip("플레이어 너머로 얼마나 더 지나갈지(가로 오버슈트 거리)")]
    public float LungeOvershoot = 4f;
    [Tooltip("다이브 시작 시 추가로 주는 위로 솔구칠 초기 속도 → 포물선 크기")]
    public float LungeArcHeight = 4f;

    [Header("Ranges")]
    public float DetectRange = 10f;
    public float ChaseRange = 15f;
    public float AttackRange = 10f;

    [Header("Patrol Timing")]
    public float PatrolMoveTimeMin = 1f;
    public float PatrolMoveTimeMax = 2.5f;

    [Header("Hover Tuning")]
    public float HoverAmplitude = 0.6f;
    public float HoverFrequency = 1.2f;
    public float HoverDriftSpeed = 0.6f;
    public float HoverY = 0f;
    public float HoverReturnSpeed = 4f;
    public float HoverMaxVy = 3f;
    [Tooltip("Chase / Attack 준비 시 플레이어 머리 위 어느 높이에 호버할지")]
    public float HoverHeightAboveTarget = 3f;
    [Tooltip("공격 전 플레이어와 유지할 가로 거리. 너무 가깝게 붙으면 다이브가 안 그려짐")]
    public float AttackStandoffDistance = 5f;
    [Tooltip("러쉬(돌진) 중 합속도 상한. 너무 빠르게 직선으로 박는 현상 방지")]
    public float MaxDiveSpeed = 9f;

    int _patrolDirection = 1;
    Vector2 _startPosition;
    float _defaultGravityScale = 1f;

    float _idleTime;
    float _idleTimer;

    float _patrolMoveTime;
    float _patrolMoveTimer;
    Transform _patrolPlayer;

    Transform _chasePlayer;

    Transform _attackPlayer;
    float _prepTimer;
    float _lungeTimer;
    bool _isLunging;
    Vector2 _targetPos;
    float _baseVx;
    float _initialVy;
    float _lungeTime;
    bool _hasDealtDamage;

    void Awake()
    {
        _startPosition = transform.position;
        if (_patrolDirection == 0) _patrolDirection = 1;

        if (TryGetComponent<Rigidbody2D>(out var rb))
        {
            _defaultGravityScale = rb.gravityScale;
        }

        HoverY = _startPosition.y;
    }

    void IEnemyIdleBehavior.EnterState(EnemyStateManager enemy)
    {
        if (enemy.TryGetComponent<SpriteRenderer>(out var sr))
        {
            sr.color = Color.white;
        }

        // Idle/Patrol에서는 스폰 고도로 복귀
        HoverY = _startPosition.y;

        _idleTime = Random.Range(1f, 4f);
        _idleTimer = 0f;

        _patrolDirection = (Random.value < 0.5f) ? -1 : 1;
        if (enemy.TryGetComponent<SpriteRenderer>(out var sprite))
        {
            sprite.flipX = (_patrolDirection > 0);
        }

        if (enemy.TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    void IEnemyIdleBehavior.ExitState(EnemyStateManager enemy)
    {
    }

    void IEnemyIdleBehavior.UpdateState(EnemyStateManager enemy)
    {
        _idleTimer += Time.deltaTime;
        if (_idleTimer >= _idleTime)
        {
            enemy.TransitionToState(new PatrolState());
            return;
        }

        if (enemy.TryGetComponent<Rigidbody2D>(out var rb))
        {
            float vy = GetHoverVy(enemy.transform);
            rb.linearVelocity = new Vector2(0f, vy);
        }
    }

    void IEnemyPatrolBehavior.EnterState(EnemyStateManager enemy)
    {
        if (enemy.TryGetComponent<SpriteRenderer>(out var sr))
        {
            sr.color = Color.yellow;
        }

        HoverY = _startPosition.y;

        _patrolMoveTimer = 0f;
        _patrolMoveTime = Random.Range(PatrolMoveTimeMin, PatrolMoveTimeMax);
        _patrolPlayer = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (_patrolDirection == 0)
        {
            _patrolDirection = 1;
        }
    }

    void IEnemyPatrolBehavior.ExitState(EnemyStateManager enemy)
    {
    }

    void IEnemyPatrolBehavior.UpdateState(EnemyStateManager enemy)
    {
        if (_patrolPlayer != null)
        {
            float dist = Vector2.Distance(enemy.transform.position, _patrolPlayer.position);
            if (dist <= DetectRange)
            {
                StopMovement(enemy);
                enemy.TransitionToState(new ChaseState());
                return;
            }
        }

        Vector2 toStart = (Vector2)enemy.transform.position - _startPosition;
        if (Mathf.Abs(toStart.x) >= PatrolRadius)
        {
            _patrolDirection = (int)-Mathf.Sign(toStart.x);
        }

        float patrolSpeed = 1f;
        if (enemy.TryGetComponent<EnemyDataManager>(out var data))
        {
            patrolSpeed = data.EnemyData.PatrolSpeed;
        }

        float hoverVy = GetHoverVy(enemy.transform);

        if (enemy.TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.linearVelocity = new Vector2(_patrolDirection * patrolSpeed + Mathf.Sign(_patrolDirection) * HoverDriftSpeed * 0.5f, hoverVy);
        }

        if (enemy.TryGetComponent<SpriteRenderer>(out var sprite))
        {
            sprite.flipX = (_patrolDirection > 0);
        }

        _patrolMoveTimer += Time.deltaTime;
        if (_patrolMoveTimer >= _patrolMoveTime)
        {
            StopMovement(enemy);
            enemy.TransitionToState(new IdleState());
        }
    }

    void IEnemyChaseBehavior.EnterState(EnemyStateManager enemy)
    {
        if (enemy.TryGetComponent<SpriteRenderer>(out var sr))
        {
            sr.color = Color.red;
        }

        _chasePlayer = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void IEnemyChaseBehavior.ExitState(EnemyStateManager enemy)
    {
    }

    void IEnemyChaseBehavior.UpdateState(EnemyStateManager enemy)
    {
        if (_chasePlayer == null)
        {
            enemy.TransitionToState(new IdleState());
            return;
        }

        float dist = Vector2.Distance(enemy.transform.position, _chasePlayer.position);
        if (dist > ChaseRange)
        {
            StopMovement(enemy);
            enemy.TransitionToState(new PatrolState());
            return;
        }

        // 수평 거리 기준으로 공격 전환 (포물선 다이브를 위해 옆에서 진입)
        float dx = _chasePlayer.position.x - enemy.transform.position.x;
        float horizDist = Mathf.Abs(dx);
        if (horizDist <= AttackRange)
        {
            StopMovement(enemy);
            enemy.TransitionToState(new AttackState());
            return;
        }

        float chaseSpeed = 2f;
        if (enemy.TryGetComponent<EnemyDataManager>(out var data))
        {
            chaseSpeed = data.EnemyData.MoveSpeed;
        }

        // 플레이어 머리 위 일정 높이를 추격 호버 고도로 사용
        HoverY = _chasePlayer.position.y + HoverHeightAboveTarget;

        // AttackStandoffDistance 안으로는 당겨오지 않고 상단 호버 유지
        float moveDir = 0f;
        if (horizDist > AttackStandoffDistance)
        {
            moveDir = dx > 0f ? 1f : -1f;
        }
        float hoverVy = GetHoverVy(enemy.transform);

        if (enemy.TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.linearVelocity = new Vector2(moveDir * chaseSpeed, hoverVy);
        }

        if (enemy.TryGetComponent<SpriteRenderer>(out var sprite))
        {
            // 플레이어 방향을 바라보도록 (멈춰있을 때도)
            float facing = dx >= 0f ? 1f : -1f;
            sprite.flipX = (facing > 0);
        }
    }

    void IEnemyAttackBehavior.EnterState(EnemyStateManager enemy)
    {
        if (enemy.TryGetComponent<SpriteRenderer>(out var sr))
        {
            sr.color = Color.magenta;
        }

        _attackPlayer = GameObject.FindGameObjectWithTag("Player")?.transform;
        _prepTimer = 0f;
        _lungeTimer = 0f;
        _isLunging = false;
        _targetPos = Vector2.zero;
        _baseVx = 0f;
        _initialVy = 0f;
        _lungeTime = 0f;
        _hasDealtDamage = false;

        StopMovement(enemy);
    }

    void IEnemyAttackBehavior.ExitState(EnemyStateManager enemy)
    {
        if (enemy.TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.gravityScale = _defaultGravityScale;
        }
    }

    void IEnemyAttackBehavior.UpdateState(EnemyStateManager enemy)
    {
        if (_attackPlayer == null)
        {
            enemy.TransitionToState(new IdleState());
            return;
        }

        float dist = Vector2.Distance(enemy.transform.position, _attackPlayer.position);
        if (dist > AttackRange * 2f && !_isLunging)
        {
            StopMovement(enemy);
            enemy.TransitionToState(new ChaseState());
            return;
        }

        if (!_isLunging)
        {
            _prepTimer += Time.deltaTime;

            // 준비 동안에도 플레이어 위로 따라붙기
            HoverY = _attackPlayer.position.y + HoverHeightAboveTarget;

            if (enemy.TryGetComponent<Rigidbody2D>(out var rb))
            {
                float vy = GetHoverVy(enemy.transform);
                rb.linearVelocity = new Vector2(0f, vy);
            }

            if (enemy.TryGetComponent<SpriteRenderer>(out var sr))
            {
                sr.flipX = (_attackPlayer.position.x > enemy.transform.position.x);
            }

            if (_prepTimer >= AttackPrepTime)
            {
                StartLunge(enemy);
            }
        }
        else
        {
            UpdateLunge(enemy);
        }
    }

    void StartLunge(EnemyStateManager enemy)
    {
        Vector2 startPos = enemy.transform.position;
        Vector2 playerPosAtStart = _attackPlayer.position;

        // 다이브 방향: 현재 플레이어 옆. 위치가 거의 같으면 스프라이트 flipX 이용
        float horiz = playerPosAtStart.x - startPos.x;
        float dir;
        if (Mathf.Abs(horiz) < 0.05f)
        {
            dir = (enemy.TryGetComponent<SpriteRenderer>(out var sr) && sr.flipX) ? 1f : -1f;
        }
        else
        {
            dir = Mathf.Sign(horiz);
        }

        // 플레이어를 지나 반대편으로 올라가도록 타깃 X를 더 멀리
        float targetX = playerPosAtStart.x + dir * LungeOvershoot;
        float targetY = playerPosAtStart.y;
        _targetPos = new Vector2(targetX, targetY);

        _lungeTime = Mathf.Max(0.01f, LungeDuration);

        Vector2 toTarget = _targetPos - startPos;
        float t = _lungeTime;

        if (enemy.TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.gravityScale = _defaultGravityScale * 0.25f;
            float gravity = Physics2D.gravity.y * rb.gravityScale;
            float vx = toTarget.x / t;
            float vy = (toTarget.y - 0.5f * gravity * t * t) / t;
            vy *= Mathf.Clamp01(LungeArcLowFactor);

            // 포물선 높이 부스트: 초기에 위로 솔아올랐다가 중력으로 떨어짐
            vy += LungeArcHeight;

            // 합속도가 너무 커지지 않도록 클램프
            Vector2 v = new Vector2(vx, vy);
            if (MaxDiveSpeed > 0f && v.magnitude > MaxDiveSpeed)
            {
                v = v.normalized * MaxDiveSpeed;
            }

            _baseVx = v.x;
            _initialVy = v.y;

            rb.linearVelocity = v;
        }

        _isLunging = true;
        _lungeTimer = 0f;
    }

    void UpdateLunge(EnemyStateManager enemy)
    {
        _lungeTimer += Time.deltaTime;
        float progress = _lungeTime <= 0f ? 1f : Mathf.Clamp01(_lungeTimer / _lungeTime);
        float speedMultiplier = Mathf.Lerp(1f, 1f + LungeAccel, progress);

        if (enemy.TryGetComponent<Rigidbody2D>(out var rb))
        {
            float vxNow = _baseVx * speedMultiplier;
            Vector2 v = new Vector2(vxNow, rb.linearVelocity.y);

            // 가속이 적용된 결과도 동일한 상한으로 잘라줌
            if (MaxDiveSpeed > 0f && v.magnitude > MaxDiveSpeed)
            {
                v = v.normalized * MaxDiveSpeed;
            }
            rb.linearVelocity = v;
        }

        if (!_hasDealtDamage)
        {
            float duringDist = Vector2.Distance(enemy.transform.position, _attackPlayer.position);
            if (duringDist <= AttackRange)
            {
                ApplyDamage();
                _hasDealtDamage = true;
            }
        }

        if (_lungeTimer >= _lungeTime)
        {
            if (enemy.TryGetComponent<Rigidbody2D>(out var rbEnd))
            {
                rbEnd.gravityScale = _defaultGravityScale;
                rbEnd.linearVelocity = Vector2.zero;
            }

            _isLunging = false;
            enemy.TransitionToState(new ChaseState());
        }
    }

    void ApplyDamage()
    {
        if (_attackPlayer == null) return;

        IDamageable target = _attackPlayer.GetComponent<IDamageable>();
        if (target != null)
        {
            float damage = 1f;
            if (TryGetComponent<EnemyDataManager>(out var data))
            {
                damage = data.EnemyData.Damage;
            }

            target.TakeDamage(damage, gameObject);
        }
    }

    float GetHoverVy(Transform target)
    {
        float oscillation = Mathf.Sin(Time.time * HoverFrequency) * HoverAmplitude;
        float desiredY = HoverY + oscillation;
        float delta = desiredY - target.position.y;
        float vy = delta * HoverReturnSpeed;
        return Mathf.Clamp(vy, -HoverMaxVy, HoverMaxVy);
    }

    void StopMovement(EnemyStateManager enemy)
    {
        if (enemy.TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.linearVelocity = Vector2.zero;
        }
    }
}
