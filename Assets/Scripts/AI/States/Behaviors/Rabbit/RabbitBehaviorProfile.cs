using UnityEngine;

public class RabbitBehaviorProfile : MonoBehaviour, IEnemyIdleBehavior, IEnemyPatrolBehavior, IEnemyChaseBehavior, IEnemyAttackBehavior
{
    [Header("Rabbit Settings")]
    public float PatrolRadius = 3f;
    public float HopForce = 2f;
    public float HopInterval = 0.6f;
    public float AttackPrepTime = 0.5f;
    public float LungeDuration = 0.4f;

    [Header("Lunge Tuning")]
    public float LungeDistanceMultiplier = 1.6f;
    public float LungeAccel = 1.2f;
    public float LungeArcLowFactor = 0.5f;

    [Header("Ranges")]
    public float DetectRange = 10f;
    public float ChaseRange = 15f;
    public float AttackRange = 1.5f;

    [Header("Patrol Timing")]
    public float PatrolMoveTimeMin = 1f;
    public float PatrolMoveTimeMax = 2.5f;

    [Header("Animation")]
    public string IdleAnimation = "Rabbit_Idle";
    public string AttackAnimation = "Rabbit_Attack";
    // 트리거 방식용 이름 (Animator에 같은 이름의 Trigger 파라미터를 추가)
    public string AttackTrigger = "Attack";

    [Header("Facing")]
    // 원본 스프라이트가 기본적으로 오른쪽을 보고 있으면 true, 왼쪽이면 false
    public bool DefaultFacingRight = false;

    int _patrolDirection = 1;
    Vector2 _startPosition;

    float _idleTime;
    float _idleTimer;

    float _patrolMoveTime;
    float _patrolMoveTimer;
    float _patrolHopTimer;
    Transform _patrolPlayer;

    float _chaseHopTimer;
    Transform _chasePlayer;

    Transform _attackPlayer;
    float _prepTimer;
    float _lungeTimer;
    bool _isLunging;
    Vector2 _targetPos;
    float _attackHopTimer;
    float _baseVx;
    float _initialVy;
    float _lungeTime;

    void Awake()
    {
        _startPosition = transform.position;
        if (_patrolDirection == 0) _patrolDirection = 1;
    }

    void SetFacing(EnemyStateManager enemy, bool faceRight)
    {
        Vector3 scale = enemy.transform.localScale;
        float absX = Mathf.Abs(scale.x);
        if (absX < 0.0001f)
        {
            absX = 1f;
        }

        bool usePositiveX = DefaultFacingRight ? faceRight : !faceRight;
        scale.x = usePositiveX ? absX : -absX;
        enemy.transform.localScale = scale;
    }

    void IEnemyIdleBehavior.EnterState(EnemyStateManager enemy)
    {
        if (enemy.TryGetComponent<Animator>(out var anim) && !string.IsNullOrEmpty(IdleAnimation))
        {
            anim.Play(IdleAnimation);
        }

        if (enemy.TryGetComponent<SpriteRenderer>(out var sr))
        {
            sr.color = Color.white;
        }

        _idleTime = Random.Range(1f, 4f);
        _idleTimer = 0f;

        _patrolDirection = (Random.value < 0.5f) ? -1 : 1;
        SetFacing(enemy, _patrolDirection > 0);

        StopMovement(enemy);
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
        _patrolHopTimer = 0f;
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

        if (enemy.TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.linearVelocity = new Vector2(_patrolDirection * patrolSpeed, rb.linearVelocity.y);
            TryHop(rb, ref _patrolHopTimer);
        }

        SetFacing(enemy, _patrolDirection > 0);

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
        _chaseHopTimer = 0f;
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
        if (enemy.TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.linearVelocity = new Vector2(moveDir * chaseSpeed, rb.linearVelocity.y);
            TryHop(rb, ref _chaseHopTimer);
        }

        SetFacing(enemy, moveDir > 0);
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
        _attackHopTimer = 0f;
        _baseVx = 0f;
        _initialVy = 0f;
        _lungeTime = 0f;

        StopMovement(enemy);
    }

    void IEnemyAttackBehavior.ExitState(EnemyStateManager enemy)
    {
    }

    void IEnemyAttackBehavior.UpdateState(EnemyStateManager enemy)
    {
        if (_attackPlayer == null)
        {
            enemy.TransitionToState(new IdleState());
            return;
        }

        float dist = Vector2.Distance(enemy.transform.position, _attackPlayer.position);
        if (dist > AttackRange * 2f)
        {
            StopMovement(enemy);
            enemy.TransitionToState(new ChaseState());
            return;
        }

        if (!_isLunging)
        {
            _prepTimer += Time.deltaTime;
            StopMovement(enemy);

            SetFacing(enemy, _attackPlayer.position.x > enemy.transform.position.x);

            TryHop(enemy, ref _attackHopTimer);

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
        float offsetX = (playerPosAtStart.x - startPos.x) * LungeDistanceMultiplier;
        float targetX = startPos.x + offsetX;
        _targetPos = new Vector2(targetX, startPos.y);

        _lungeTime = Mathf.Max(0.01f, LungeDuration);

        Vector2 toTarget = _targetPos - startPos;
        float dx = toTarget.x;
        float dy = toTarget.y;
        float t = _lungeTime;

        if (enemy.TryGetComponent<Rigidbody2D>(out var rb))
        {
            float gravity = Physics2D.gravity.y * rb.gravityScale;
            float vx = dx / t;
            float vy = (dy - 0.5f * gravity * t * t) / t;
            vy *= Mathf.Clamp01(LungeArcLowFactor);

            _baseVx = vx;
            _initialVy = vy;

            rb.linearVelocity = new Vector2(_baseVx, _initialVy);
        }

        if (enemy.TryGetComponent<Animator>(out var anim))
        {
            if (!string.IsNullOrEmpty(AttackTrigger) && anim.HasParameterOfType(AttackTrigger, AnimatorControllerParameterType.Trigger))
            {
                anim.SetTrigger(AttackTrigger);
            }
            else if (!string.IsNullOrEmpty(AttackAnimation))
            {
                anim.Play(AttackAnimation);
            }
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

        if (_lungeTimer >= _lungeTime)
        {
            StopMovement(enemy);
            _isLunging = false;

            float postDist = Vector2.Distance(enemy.transform.position, _attackPlayer.position);
            if (postDist <= AttackRange)
            {
                ApplyDamage();
                enemy.TransitionToState(new IdleState());
            }
            else
            {
                enemy.TransitionToState(new ChaseState());
            }
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

    void TryHop(EnemyStateManager enemy, ref float hopTimer)
    {
        hopTimer += Time.deltaTime;
        if (hopTimer >= HopInterval)
        {
            if (enemy.TryGetComponent<Rigidbody2D>(out var rb))
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, HopForce);
            }
            hopTimer = 0f;
        }
    }

    void TryHop(Rigidbody2D rb, ref float hopTimer)
    {
        hopTimer += Time.deltaTime;
        if (hopTimer >= HopInterval)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, HopForce);
            hopTimer = 0f;
        }
    }

    void StopMovement(EnemyStateManager enemy)
    {
        if (enemy.TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.linearVelocity = Vector2.zero;
        }
    }
}

public static class AnimatorExtensions
{
    public static bool HasParameterOfType(this Animator animator, string paramName, AnimatorControllerParameterType type)
    {
        if (animator == null || string.IsNullOrEmpty(paramName)) return false;
        foreach (var p in animator.parameters)
        {
            if (p.type == type && p.name == paramName) return true;
        }
        return false;
    }
}
