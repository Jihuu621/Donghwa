using UnityEngine;

public class ShieldEnemyBehaviorProfile : MonoBehaviour, IEnemyIdleBehavior, IEnemyPatrolBehavior, IEnemyChaseBehavior, IEnemyAttackBehavior
{
    [Header("Ranges")]
    public float DetectRange = 10f;
    public float ChaseRange = 15f;
    public float AttackRange = 1.6f;

    [Header("Patrol Timing")]
    public float PatrolMoveTimeMin = 1f;
    public float PatrolMoveTimeMax = 4f;

    [Header("Attack Tuning")]
    public float AttackCooldown = 1.2f;
    public float BlockChance = 0.4f;
    public float BlockTime = 1.2f;
    public float CounterAttackChance = 0.25f;
    public float CounterAttackDelay = 0.3f;
    public float BlockStabDelay = 0.2f;
    public float BlockStabRange = 1.8f;
    public float BlockStabExitTime = 0.5f;

    public float StabDelay = 0.4f;
    public float StabRange = 2.0f;
    public float StabExitDelay = 0.3f;

    public float SlamDelay = 0.7f;
    public float SlamRange = 1.5f;
    public float SlamExitDelay = 0.5f;

    public float ChargeSpeed = 5f;
    public float ChargeAttackRange = 2.2f;
    public float ChargeDuration = 1.0f;
    public float ChargeAttackStart = 0.5f;
    public float ChargeAttackEnd = 0.6f;

    float _idleTime;
    float _idleTimer;

    float _patrolMoveTime;
    float _patrolMoveTimer;
    int _patrolDirection = 1;
    Transform _patrolPlayer;
    bool _patrolInitialized;

    Transform _chasePlayer;
    Vector3 _originScale;

    Transform _attackPlayer;
    float _attackTimer;
    AttackMode _attackMode;
    bool _hasAttacked;
    bool _didCounterAttack;

    void Awake()
    {
        _originScale = transform.localScale;
        if (_patrolDirection == 0) _patrolDirection = 1;
    }

    void IEnemyIdleBehavior.EnterState(EnemyStateManager enemy)
    {
        if (enemy.TryGetComponent<SpriteRenderer>(out var sr))
        {
            sr.color = Color.white;
        }

        _idleTime = Random.Range(1f, 4f);
        _idleTimer = 0f;
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
        _patrolPlayer = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (!_patrolInitialized)
        {
            _patrolDirection = _patrolDirection == 0 ? 1 : _patrolDirection;
            _patrolInitialized = true;
        }
        else
        {
            _patrolDirection = -_patrolDirection;
        }

        ApplyFacing(enemy, _patrolDirection);
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

        float patrolSpeed = 1f;
        if (enemy.TryGetComponent<EnemyDataManager>(out var data))
        {
            patrolSpeed = data.EnemyData.PatrolSpeed;
        }

        if (enemy.TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.linearVelocity = new Vector2(_patrolDirection * patrolSpeed, rb.linearVelocity.y);
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
        _chasePlayer = GameObject.FindGameObjectWithTag("Player")?.transform;
        _originScale = enemy.transform.localScale;

        if (enemy.TryGetComponent<SpriteRenderer>(out var sr))
        {
            sr.color = Color.red;
        }
    }

    void IEnemyChaseBehavior.ExitState(EnemyStateManager enemy)
    {
        StopMovement(enemy);
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
            enemy.TransitionToState(new PatrolState());
            return;
        }

        if (dist < AttackRange)
        {
            enemy.TransitionToState(new AttackState());
            return;
        }

        float chaseSpeed = 2.5f;
        if (enemy.TryGetComponent<EnemyDataManager>(out var data))
        {
            chaseSpeed = data.EnemyData.MoveSpeed;
        }

        float dir = _chasePlayer.position.x > enemy.transform.position.x ? 1f : -1f;
        if (enemy.TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.linearVelocity = new Vector2(dir * chaseSpeed, rb.linearVelocity.y);
        }

        ApplyFacing(enemy, dir);
    }

    void IEnemyAttackBehavior.EnterState(EnemyStateManager enemy)
    {
        if (enemy.TryGetComponent<SpriteRenderer>(out var sr))
        {
            sr.color = Color.magenta;
        }

        _attackPlayer = GameObject.FindGameObjectWithTag("Player")?.transform;
        _attackTimer = 0f;
        _attackMode = AttackMode.Select;
        _hasAttacked = false;
        _didCounterAttack = false;
        StopMovement(enemy);
    }

    void IEnemyAttackBehavior.ExitState(EnemyStateManager enemy)
    {
        StopMovement(enemy);
    }

    void IEnemyAttackBehavior.UpdateState(EnemyStateManager enemy)
    {
        if (_attackPlayer == null)
        {
            enemy.TransitionToState(new IdleState());
            return;
        }

        switch (_attackMode)
        {
            case AttackMode.Select:
                UpdateAttackSelect(enemy);
                break;
            case AttackMode.Block:
                UpdateBlock(enemy);
                break;
            case AttackMode.BlockStab:
                UpdateBlockStab(enemy);
                break;
            case AttackMode.Stab:
                UpdateStab(enemy);
                break;
            case AttackMode.Slam:
                UpdateSlam(enemy);
                break;
            case AttackMode.ChargeSwing:
                UpdateChargeSwing(enemy);
                break;
        }
    }

    void UpdateAttackSelect(EnemyStateManager enemy)
    {
        _attackTimer += Time.deltaTime;
        if (_attackTimer < AttackCooldown)
        {
            return;
        }

        var manager = enemy.GetComponent<ShieldEnemyManager>();
        if (manager != null && !manager.IsShieldBroken && Random.value < BlockChance)
        {
            SetAttackMode(AttackMode.Block);
            return;
        }

        int roll = Random.Range(0, 3);
        SetAttackMode(roll == 0 ? AttackMode.Stab : roll == 1 ? AttackMode.Slam : AttackMode.ChargeSwing);
    }

    void UpdateBlock(EnemyStateManager enemy)
    {
        var manager = enemy.GetComponent<ShieldEnemyManager>();
        if (manager != null && manager.IsShieldBroken)
        {
            SetAttackMode(AttackMode.Select);
            return;
        }

        _attackTimer += Time.deltaTime;

        if (!_didCounterAttack && _attackTimer > CounterAttackDelay && Random.value < CounterAttackChance)
        {
            _didCounterAttack = true;
            SetAttackMode(AttackMode.BlockStab);
            return;
        }

        if (_attackTimer >= BlockTime)
        {
            enemy.TransitionToState(new ChaseState());
        }
    }

    void UpdateBlockStab(EnemyStateManager enemy)
    {
        _attackTimer += Time.deltaTime;
        if (!_hasAttacked && _attackTimer >= BlockStabDelay)
        {
            enemy.PerformAttack(BlockStabRange);
            _hasAttacked = true;
        }

        if (_attackTimer >= BlockStabExitTime)
        {
            enemy.TransitionToState(new ChaseState());
        }
    }

    void UpdateStab(EnemyStateManager enemy)
    {
        _attackTimer += Time.deltaTime;
        if (!_hasAttacked && _attackTimer >= StabDelay)
        {
            enemy.PerformAttack(StabRange);
            _hasAttacked = true;
        }

        if (_attackTimer >= StabDelay + StabExitDelay)
        {
            enemy.TransitionToState(new ChaseState());
        }
    }

    void UpdateSlam(EnemyStateManager enemy)
    {
        _attackTimer += Time.deltaTime;
        if (!_hasAttacked && _attackTimer >= SlamDelay)
        {
            enemy.PerformAttack(SlamRange);
            _hasAttacked = true;
        }

        if (_attackTimer >= SlamDelay + SlamExitDelay)
        {
            enemy.TransitionToState(new ChaseState());
        }
    }

    void UpdateChargeSwing(EnemyStateManager enemy)
    {
        _attackTimer += Time.deltaTime;

        if (_attackTimer < ChargeAttackStart)
        {
            float dir = enemy.transform.localScale.x > 0 ? 1f : -1f;
            if (enemy.TryGetComponent<Rigidbody2D>(out var rb))
            {
                rb.linearVelocity = new Vector2(dir * ChargeSpeed, rb.linearVelocity.y);
            }
        }
        else if (_attackTimer >= ChargeAttackStart && _attackTimer < ChargeAttackEnd)
        {
            StopMovement(enemy);
            if (!_hasAttacked)
            {
                enemy.PerformAttack(ChargeAttackRange);
                _hasAttacked = true;
            }
        }
        else if (_attackTimer >= ChargeDuration)
        {
            enemy.TransitionToState(new ChaseState());
        }
    }

    void SetAttackMode(AttackMode mode)
    {
        _attackMode = mode;
        _attackTimer = 0f;
        _hasAttacked = false;
        _didCounterAttack = false;
    }

    void ApplyFacing(EnemyStateManager enemy, float dir)
    {
        enemy.transform.localScale = new Vector3(
            Mathf.Abs(_originScale.x) * Mathf.Sign(dir),
            _originScale.y,
            _originScale.z
        );
    }

    void StopMovement(EnemyStateManager enemy)
    {
        if (enemy.TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    enum AttackMode
    {
        Select,
        Block,
        BlockStab,
        Stab,
        Slam,
        ChargeSwing
    }
}
