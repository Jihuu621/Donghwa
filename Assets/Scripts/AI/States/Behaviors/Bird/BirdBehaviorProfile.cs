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

        if (dist < AttackRange)
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

        float moveDir = _chasePlayer.position.x > enemy.transform.position.x ? 1f : -1f;
        float hoverVy = GetHoverVy(enemy.transform);

        if (enemy.TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.linearVelocity = new Vector2(moveDir * chaseSpeed, hoverVy);
        }

        if (enemy.TryGetComponent<SpriteRenderer>(out var sprite))
        {
            sprite.flipX = (moveDir > 0);
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

        float offsetX = (playerPosAtStart.x - startPos.x) * LungeDistanceMultiplierFar;
        float targetX = startPos.x + offsetX;
        float targetY = startPos.y + LungeTargetYOffset;
        _targetPos = new Vector2(targetX, targetY);

        _lungeTime = Mathf.Max(0.01f, LungeDuration);

        Vector2 toTarget = _targetPos - startPos;
        float dx = toTarget.x;
        float dy = toTarget.y;
        float t = _lungeTime;

        if (enemy.TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.gravityScale = _defaultGravityScale * 0.25f;
            float gravity = Physics2D.gravity.y * rb.gravityScale;
            float vx = dx / t;
            float vy = (dy - 0.5f * gravity * t * t) / t;
            vy *= Mathf.Clamp01(LungeArcLowFactor);

            _baseVx = vx;
            _initialVy = vy;

            rb.linearVelocity = new Vector2(_baseVx, _initialVy);
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
            rb.linearVelocity = new Vector2(vxNow, rb.linearVelocity.y);
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

        var playerHealth = _attackPlayer.GetComponent<Health>();
        if (playerHealth == null) return;

        float damage = 1f;
        if (TryGetComponent<EnemyDataManager>(out var data))
        {
            damage = data.EnemyData.Damage;
        }

        float finalDamage = damage;
        var parry = _attackPlayer.GetComponent<PlayerParry>();
        if (parry != null)
        {
            finalDamage = parry.OnHit(damage);
        }

        if (finalDamage > 0f)
        {
            playerHealth.TakeDamage(finalDamage);
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
