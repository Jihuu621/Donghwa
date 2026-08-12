using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class RabbitWeirdSetup : EnemyAIBase
{
    private static readonly int IdleAnimation = Animator.StringToHash("Base Layer.Rabbit_Weird_Idle");
    private static readonly int AttackAnimation = Animator.StringToHash("Base Layer.Rabbit_Weird_Attack");

    private enum State
    {
        None,
        Idle,
        Patrol,
        Chase,
        BodySlamPrep,
        BodySlam,
        PawPrep,
        PawAttack,
        Recovery,
        Stunned
    }

    [Header("Patrol")]
    [SerializeField, Min(0.1f)] private float patrolRadius = 4f;
    [SerializeField, Min(0.1f)] private float patrolSpeed = 2.2f;
    [SerializeField, Min(0.1f)] private float patrolMoveTimeMin = 1f;
    [SerializeField, Min(0.1f)] private float patrolMoveTimeMax = 3f;
    [SerializeField, Min(0f)] private float patrolPauseTimeMin = 0.45f;
    [SerializeField, Min(0f)] private float patrolPauseTimeMax = 0.9f;

    [Header("Chase")]
    [SerializeField, Min(0.1f)] private float detectionRange = 9f;
    [SerializeField, Min(0.1f)] private float loseTargetRange = 14f;
    [SerializeField, Min(0.1f)] private float chaseSpeed = 3.5f;
    [SerializeField, Min(0f)] private float horizontalDeadZone = 0.12f;

    [Header("Hopping")]
    [SerializeField, Min(0.05f)] private float hopInterval = 0.55f;
    [SerializeField, Min(0f)] private float hopVelocity = 6f;
    [SerializeField] private Vector2 groundCheckOffset = new Vector2(0f, -0.82f);
    [SerializeField, Min(0.02f)] private float groundCheckRadius = 0.18f;
    [SerializeField] private LayerMask groundMask = (1 << 3) | (1 << 9);

    [Header("Body Slam Combo")]
    [SerializeField, Min(0.1f)] private float bodySlamRange = 4.5f;
    [SerializeField, Min(0f)] private float bodySlamPrepTime = 0.55f;
    [SerializeField, Range(1, 10)] private int bodySlamCount = 3;
    [SerializeField, Min(0.1f)] private float bodySlamTargetTime = 0.45f;
    [SerializeField, Min(1f)] private float bodySlamOvershoot = 1.25f;
    [Tooltip("Each slam travels at least this far, even when the player is very close.")]
    [SerializeField, Min(0.1f)] private float bodySlamMinimumDistance = 3.25f;
    [SerializeField, Min(0.1f)] private float bodySlamMaximumHorizontalSpeed = 13f;
    [SerializeField] private Vector2 bodySlamVerticalVelocityRange = new Vector2(4f, 12f);
    [SerializeField, Min(0.1f)] private float bodySlamMaximumDuration = 1.4f;
    [SerializeField, Min(0f)] private float minimumAirTime = 0.12f;
    [SerializeField, Min(0.05f)] private float bodyHitRadius = 0.62f;
    [SerializeField, Min(0f)] private float bodySlamDamage = 8f;

    [Header("Paw Combo")]
    [SerializeField, Min(0.1f)] private float pawAttackRange = 1.65f;
    [SerializeField, Min(0f)] private float pawPrepTime = 0.25f;
    [SerializeField, Range(1, 10)] private int pawSwingCount = 5;
    [SerializeField, Min(0.02f)] private float pawSwingInterval = 0.22f;
    [SerializeField, Min(0f)] private float pawAttackMoveSpeed = 0.8f;
    [SerializeField, Min(0.1f)] private float pawAttackHeight = 1.25f;
    [SerializeField] private float pawAttackVerticalOffset = 0.05f;
    [SerializeField, Min(0f)] private float pawDamagePerSwing = 3f;

    [Header("Recovery")]
    [SerializeField, Min(0f)] private float attackRecoveryTime = 0.55f;
    [SerializeField, Min(0f)] private float attackCooldown = 0.65f;

    [Header("Animation")]
    [SerializeField, Min(0.01f)] private float attackAnimationLength = 0.4167f;
    [Tooltip("Rabbit_Weird sprites face left when Flip X is disabled.")]
    [SerializeField] private bool defaultFacingRight;

    private readonly List<Collider2D> _ignoredOwnColliders = new List<Collider2D>();
    private readonly List<Collider2D> _ignoredPlayerColliders = new List<Collider2D>();

    private State _state;
    private Vector2 _spawnPosition;
    private Color _normalColor = Color.white;
    private float _stateTimer;
    private float _idleDuration;
    private float _patrolMoveDuration;
    private float _hopTimer;
    private float _attackCooldownTimer;
    private float _stunDuration;
    private int _moveDirection = -1;
    private int _facingDirection = -1;
    private int _bodySlamsStarted;
    private int _pawSwingsCompleted;
    private float _pawSwingTimer;
    private bool _bodyDamageDealt;
    private bool _ignoringPlayerCollision;
    private Vector2 _previousBodyPosition;

    protected override void Awake()
    {
        base.Awake();
        _spawnPosition = transform.position;
        if (Fsm.Sr != null) _normalColor = Fsm.Sr.color;

        if (Fsm.Rb != null)
        {
            Fsm.Rb.freezeRotation = true;
            Fsm.Rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            Fsm.Rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
    }

    private void Start()
    {
        SetPlayerCollisionIgnored(true);
        ChangeState(State.Idle);
    }

    private void Update()
    {
        if (!_ignoringPlayerCollision) SetPlayerCollisionIgnored(true);
        _attackCooldownTimer = Mathf.Max(0f, _attackCooldownTimer - Time.deltaTime);

        switch (_state)
        {
            case State.Idle: UpdateIdle(); break;
            case State.Patrol: UpdatePatrol(); break;
            case State.Chase: UpdateChase(); break;
            case State.BodySlamPrep: UpdateBodySlamPrep(); break;
            case State.BodySlam: UpdateBodySlam(); break;
            case State.PawPrep: UpdatePawPrep(); break;
            case State.PawAttack: UpdatePawAttack(); break;
            case State.Recovery: UpdateRecovery(); break;
            case State.Stunned: UpdateStunned(); break;
        }
    }

    public override bool TryStun(float duration)
    {
        _stunDuration = Mathf.Max(_stunDuration, duration);
        if (_state == State.Stunned)
        {
            _stateTimer = 0f;
            return true;
        }

        ChangeState(State.Stunned);
        return true;
    }

    private void ChangeState(State next)
    {
        if (_state == next) return;

        _state = next;
        _stateTimer = 0f;

        switch (next)
        {
            case State.Idle:
                StopHorizontalMovement();
                _idleDuration = Random.Range(patrolPauseTimeMin, patrolPauseTimeMax);
                PlayIdle();
                break;
            case State.Patrol:
                _moveDirection = ChoosePatrolDirection();
                _patrolMoveDuration = Random.Range(patrolMoveTimeMin, patrolMoveTimeMax);
                _hopTimer = hopInterval;
                FaceDirection(_moveDirection);
                PlayIdle();
                break;
            case State.Chase:
                _hopTimer = hopInterval;
                PlayIdle();
                break;
            case State.BodySlamPrep:
                StopHorizontalMovement();
                _bodySlamsStarted = 0;
                FacePlayer();
                HoldAttackPose();
                break;
            case State.PawPrep:
                StopHorizontalMovement();
                FacePlayer();
                HoldAttackPose();
                break;
            case State.PawAttack:
                _pawSwingsCompleted = 0;
                _pawSwingTimer = 0f;
                StartNextPawSwing();
                break;
            case State.Recovery:
                StopHorizontalMovement();
                _attackCooldownTimer = attackCooldown;
                PlayIdle();
                break;
            case State.Stunned:
                StopAllMovement();
                PlayIdle();
                if (Fsm.Sr != null) Fsm.Sr.color = Color.cyan;
                break;
        }
    }

    private void UpdateIdle()
    {
        StopHorizontalMovement();
        if (CanDetectPlayer())
        {
            ChangeState(State.Chase);
            return;
        }

        _stateTimer += Time.deltaTime;
        if (_stateTimer >= _idleDuration) ChangeState(State.Patrol);
    }

    private void UpdatePatrol()
    {
        if (CanDetectPlayer())
        {
            ChangeState(State.Chase);
            return;
        }

        float offset = transform.position.x - _spawnPosition.x;
        if (Mathf.Abs(offset) >= patrolRadius)
        {
            _moveDirection = offset > 0f ? -1 : 1;
        }

        MoveAndHop(_moveDirection, patrolSpeed);
        _stateTimer += Time.deltaTime;
        if (_stateTimer >= _patrolMoveDuration) ChangeState(State.Idle);
    }

    private void UpdateChase()
    {
        if (Fsm.Player == null)
        {
            ChangeState(State.Idle);
            return;
        }

        float distance = Vector2.Distance(transform.position, Fsm.Player.position);
        if (distance > loseTargetRange)
        {
            ChangeState(State.Patrol);
            return;
        }

        if (_attackCooldownTimer <= 0f)
        {
            if (distance <= pawAttackRange)
            {
                ChangeState(State.PawPrep);
                return;
            }

            if (distance <= bodySlamRange)
            {
                ChangeState(State.BodySlamPrep);
                return;
            }
        }

        int direction = GetDirectionToPlayer();
        if (direction == 0)
        {
            StopHorizontalMovement();
            return;
        }

        MoveAndHop(direction, chaseSpeed);
    }

    private void UpdateBodySlamPrep()
    {
        StopHorizontalMovement();
        FacePlayer();
        _stateTimer += Time.deltaTime;
        if (_stateTimer < bodySlamPrepTime) return;

        _state = State.BodySlam;
        BeginBodySlam();
    }

    private void BeginBodySlam()
    {
        if (Fsm.Rb == null || Fsm.Player == null)
        {
            ChangeState(State.Recovery);
            return;
        }

        _stateTimer = 0f;
        _bodySlamsStarted++;
        _bodyDamageDealt = false;

        Vector2 origin = Fsm.Rb.position;
        Vector2 lockedTarget = Fsm.Player.position;
        float targetTime = Mathf.Max(0.1f, bodySlamTargetTime);
        float targetDeltaX = lockedTarget.x - origin.x;
        float slamDirection = Mathf.Abs(targetDeltaX) > horizontalDeadZone
            ? Mathf.Sign(targetDeltaX)
            : _facingDirection;
        float travelDistance = Mathf.Max(
            Mathf.Abs(targetDeltaX) * bodySlamOvershoot,
            bodySlamMinimumDistance);
        float deltaX = slamDirection * travelDistance;

        // The minimum travel distance takes priority over the optional speed cap.
        float effectiveMaximumSpeed = Mathf.Max(
            bodySlamMaximumHorizontalSpeed,
            bodySlamMinimumDistance / targetTime);
        float horizontalVelocity = Mathf.Clamp(
            deltaX / targetTime,
            -effectiveMaximumSpeed,
            effectiveMaximumSpeed);
        float gravity = Physics2D.gravity.y * Fsm.Rb.gravityScale;
        float verticalVelocity = (lockedTarget.y - origin.y - 0.5f * gravity * targetTime * targetTime) /
                                 targetTime;
        verticalVelocity = Mathf.Clamp(verticalVelocity,
            bodySlamVerticalVelocityRange.x, bodySlamVerticalVelocityRange.y);

        FaceDirection(horizontalVelocity);
        Fsm.Rb.linearVelocity = new Vector2(horizontalVelocity, verticalVelocity);
        _previousBodyPosition = origin;
        PlayAttack(1f, 0f);
    }

    private void UpdateBodySlam()
    {
        if (Fsm.Rb == null)
        {
            ChangeState(State.Recovery);
            return;
        }

        _stateTimer += Time.deltaTime;
        Vector2 currentPosition = Fsm.Rb.position;
        TryDealBodyDamage(_previousBodyPosition, currentPosition);
        _previousBodyPosition = currentPosition;

        bool landed = _stateTimer >= minimumAirTime &&
                      Fsm.Rb.linearVelocity.y <= 0.1f &&
                      IsGrounded();
        if (landed)
        {
            if (_bodySlamsStarted < bodySlamCount)
            {
                BeginBodySlam();
            }
            else
            {
                ChangeState(State.Recovery);
            }
            return;
        }

        if (_stateTimer >= bodySlamMaximumDuration) ChangeState(State.Recovery);
    }

    private void TryDealBodyDamage(Vector2 start, Vector2 end)
    {
        if (_bodyDamageDealt || Fsm.Player == null) return;

        Vector2 delta = end - start;
        IDamageable player;
        bool foundPlayer;
        if (delta.sqrMagnitude > 0.0001f)
        {
            RaycastHit2D[] hits = Physics2D.CircleCastAll(
                start, bodyHitRadius, delta.normalized, delta.magnitude);
            foundPlayer = TryFindPlayerDamageable(hits, out player);
        }
        else
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(end, bodyHitRadius);
            foundPlayer = TryFindPlayerDamageable(hits, out player);
        }

        if (!foundPlayer) return;
        player.TakeDamage(bodySlamDamage, gameObject);
        _bodyDamageDealt = true;
    }

    private void UpdatePawPrep()
    {
        StopHorizontalMovement();
        FacePlayer();
        _stateTimer += Time.deltaTime;
        if (_stateTimer >= pawPrepTime) ChangeState(State.PawAttack);
    }

    private void UpdatePawAttack()
    {
        MoveSlowlyTowardPlayer();
        _pawSwingTimer += Time.deltaTime;

        while (_pawSwingTimer >= pawSwingInterval && _pawSwingsCompleted < pawSwingCount)
        {
            _pawSwingTimer -= pawSwingInterval;
            StartNextPawSwing();
        }

        if (_pawSwingsCompleted >= pawSwingCount && _pawSwingTimer >= pawSwingInterval)
        {
            ChangeState(State.Recovery);
        }
    }

    private void StartNextPawSwing()
    {
        _pawSwingsCompleted++;
        PerformPawSwing();
    }

    private void PerformPawSwing()
    {
        PlayAttack(attackAnimationLength / Mathf.Max(0.02f, pawSwingInterval), 0f);
        if (Fsm.Rb == null || Fsm.Player == null) return;

        Vector2 center = Fsm.Rb.position +
                         new Vector2(_facingDirection * pawAttackRange * 0.5f, pawAttackVerticalOffset);
        Vector2 size = new Vector2(pawAttackRange, pawAttackHeight);
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f);
        if (TryFindPlayerDamageable(hits, out IDamageable player))
        {
            player.TakeDamage(pawDamagePerSwing, gameObject);
        }
    }

    private void MoveSlowlyTowardPlayer()
    {
        if (Fsm.Rb == null || Fsm.Player == null) return;

        int direction = GetDirectionToPlayer();
        if (direction == 0)
        {
            StopHorizontalMovement();
            return;
        }

        FaceDirection(direction);
        Fsm.Rb.linearVelocity = new Vector2(direction * pawAttackMoveSpeed, Fsm.Rb.linearVelocity.y);
    }

    private void UpdateRecovery()
    {
        StopHorizontalMovement();
        _stateTimer += Time.deltaTime;
        if (_stateTimer < attackRecoveryTime) return;

        ChangeState(Fsm.Player != null ? State.Chase : State.Idle);
    }

    private void UpdateStunned()
    {
        StopAllMovement();
        _stateTimer += Time.deltaTime;
        if (_stateTimer < _stunDuration) return;

        _stunDuration = 0f;
        if (Fsm.Sr != null) Fsm.Sr.color = _normalColor;
        ChangeState(Fsm.Player != null ? State.Chase : State.Idle);
    }

    private bool CanDetectPlayer()
    {
        return Fsm.Player != null &&
               Vector2.Distance(transform.position, Fsm.Player.position) <= detectionRange;
    }

    private int GetDirectionToPlayer()
    {
        if (Fsm.Player == null) return 0;

        float deltaX = Fsm.Player.position.x - transform.position.x;
        if (Mathf.Abs(deltaX) <= horizontalDeadZone) return 0;
        return deltaX > 0f ? 1 : -1;
    }

    private int ChoosePatrolDirection()
    {
        float offset = transform.position.x - _spawnPosition.x;
        if (offset >= patrolRadius * 0.8f) return -1;
        if (offset <= -patrolRadius * 0.8f) return 1;
        return Random.value < 0.5f ? -1 : 1;
    }

    private void MoveAndHop(int direction, float speed)
    {
        if (Fsm.Rb == null || direction == 0) return;

        FaceDirection(direction);
        Fsm.Rb.linearVelocity = new Vector2(direction * speed, Fsm.Rb.linearVelocity.y);
        _hopTimer += Time.deltaTime;
        if (_hopTimer >= hopInterval && IsGrounded())
        {
            Fsm.Rb.linearVelocity = new Vector2(Fsm.Rb.linearVelocity.x, hopVelocity);
            _hopTimer = 0f;
        }
    }

    private bool IsGrounded()
    {
        Vector2 origin = Fsm.Rb != null ? Fsm.Rb.position : (Vector2)transform.position;
        return Physics2D.OverlapCircle(origin + groundCheckOffset, groundCheckRadius, groundMask) != null;
    }

    private bool TryFindPlayerDamageable(Collider2D[] hits, out IDamageable damageable)
    {
        damageable = null;
        if (Fsm.Player == null) return false;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null || hit.transform.root != Fsm.Player.root) continue;

            damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable != null) return true;
        }

        return false;
    }

    private bool TryFindPlayerDamageable(RaycastHit2D[] hits, out IDamageable damageable)
    {
        damageable = null;
        if (Fsm.Player == null) return false;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i].collider;
            if (hit == null || hit.transform.root != Fsm.Player.root) continue;

            damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable != null) return true;
        }

        return false;
    }

    private void SetPlayerCollisionIgnored(bool ignored)
    {
        if (ignored == _ignoringPlayerCollision) return;

        if (!ignored)
        {
            for (int ownIndex = 0; ownIndex < _ignoredOwnColliders.Count; ownIndex++)
            {
                Collider2D own = _ignoredOwnColliders[ownIndex];
                if (own == null) continue;
                for (int playerIndex = 0; playerIndex < _ignoredPlayerColliders.Count; playerIndex++)
                {
                    Collider2D player = _ignoredPlayerColliders[playerIndex];
                    if (player != null) Physics2D.IgnoreCollision(own, player, false);
                }
            }

            _ignoredOwnColliders.Clear();
            _ignoredPlayerColliders.Clear();
            _ignoringPlayerCollision = false;
            return;
        }

        if (Fsm.Player == null) return;
        Collider2D[] ownColliders = GetComponentsInChildren<Collider2D>();
        for (int i = 0; i < ownColliders.Length; i++)
        {
            if (ownColliders[i] != null && !ownColliders[i].isTrigger)
            {
                _ignoredOwnColliders.Add(ownColliders[i]);
            }
        }

        Collider2D[] playerColliders = Fsm.Player.GetComponentsInChildren<Collider2D>();
        for (int i = 0; i < playerColliders.Length; i++)
        {
            if (playerColliders[i] != null && !playerColliders[i].isTrigger)
            {
                _ignoredPlayerColliders.Add(playerColliders[i]);
            }
        }

        for (int ownIndex = 0; ownIndex < _ignoredOwnColliders.Count; ownIndex++)
        {
            Collider2D own = _ignoredOwnColliders[ownIndex];
            if (own == null) continue;
            for (int playerIndex = 0; playerIndex < _ignoredPlayerColliders.Count; playerIndex++)
            {
                Collider2D player = _ignoredPlayerColliders[playerIndex];
                if (player != null) Physics2D.IgnoreCollision(own, player, true);
            }
        }

        _ignoringPlayerCollision = true;
    }

    private void FacePlayer()
    {
        int direction = GetDirectionToPlayer();
        if (direction != 0) FaceDirection(direction);
    }

    private void FaceDirection(float direction)
    {
        if (Mathf.Abs(direction) <= 0.01f) return;

        _facingDirection = direction > 0f ? 1 : -1;
        if (Fsm.Sr != null)
        {
            bool faceRight = _facingDirection > 0;
            Fsm.Sr.flipX = defaultFacingRight ? !faceRight : faceRight;
        }
    }

    private void StopHorizontalMovement()
    {
        if (Fsm.Rb != null)
        {
            Fsm.Rb.linearVelocity = new Vector2(0f, Fsm.Rb.linearVelocity.y);
        }
    }

    private void StopAllMovement()
    {
        if (Fsm.Rb != null) Fsm.Rb.linearVelocity = Vector2.zero;
    }

    private void PlayIdle()
    {
        PlayAnimation(IdleAnimation, 1f, 0f);
    }

    private void HoldAttackPose()
    {
        PlayAnimation(AttackAnimation, 0f, 0f);
    }

    private void PlayAttack(float speed, float normalizedTime)
    {
        PlayAnimation(AttackAnimation, speed, normalizedTime);
    }

    private void PlayAnimation(int stateHash, float speed, float normalizedTime)
    {
        if (Fsm.Anim == null || !Fsm.Anim.HasState(0, stateHash)) return;
        Fsm.Anim.speed = speed;
        Fsm.Anim.Play(stateHash, 0, normalizedTime);
    }

    private void OnDisable()
    {
        SetPlayerCollisionIgnored(false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = new Color(1f, 0.45f, 0.1f);
        Gizmos.DrawWireSphere(transform.position, bodySlamRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, pawAttackRange);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere((Vector2)transform.position + groundCheckOffset, groundCheckRadius);
    }

    private void OnValidate()
    {
        patrolRadius = Mathf.Max(0.1f, patrolRadius);
        patrolSpeed = Mathf.Max(0.1f, patrolSpeed);
        patrolMoveTimeMin = Mathf.Max(0.1f, patrolMoveTimeMin);
        patrolMoveTimeMax = Mathf.Max(patrolMoveTimeMin, patrolMoveTimeMax);
        patrolPauseTimeMin = Mathf.Max(0f, patrolPauseTimeMin);
        patrolPauseTimeMax = Mathf.Max(patrolPauseTimeMin, patrolPauseTimeMax);
        detectionRange = Mathf.Max(0.1f, detectionRange);
        loseTargetRange = Mathf.Max(detectionRange, loseTargetRange);
        chaseSpeed = Mathf.Max(0.1f, chaseSpeed);
        horizontalDeadZone = Mathf.Max(0f, horizontalDeadZone);
        hopInterval = Mathf.Max(0.05f, hopInterval);
        hopVelocity = Mathf.Max(0f, hopVelocity);
        groundCheckRadius = Mathf.Max(0.02f, groundCheckRadius);
        bodySlamRange = Mathf.Max(0.1f, bodySlamRange);
        bodySlamPrepTime = Mathf.Max(0f, bodySlamPrepTime);
        bodySlamCount = Mathf.Max(1, bodySlamCount);
        bodySlamTargetTime = Mathf.Max(0.1f, bodySlamTargetTime);
        bodySlamOvershoot = Mathf.Max(1f, bodySlamOvershoot);
        bodySlamMinimumDistance = Mathf.Max(0.1f, bodySlamMinimumDistance);
        bodySlamMaximumHorizontalSpeed = Mathf.Max(0.1f, bodySlamMaximumHorizontalSpeed);
        bodySlamVerticalVelocityRange.x = Mathf.Max(0f, bodySlamVerticalVelocityRange.x);
        bodySlamVerticalVelocityRange.y = Mathf.Max(bodySlamVerticalVelocityRange.x,
            bodySlamVerticalVelocityRange.y);
        bodySlamMaximumDuration = Mathf.Max(minimumAirTime + 0.1f, bodySlamMaximumDuration);
        minimumAirTime = Mathf.Max(0f, minimumAirTime);
        bodyHitRadius = Mathf.Max(0.05f, bodyHitRadius);
        bodySlamDamage = Mathf.Max(0f, bodySlamDamage);
        pawAttackRange = Mathf.Clamp(pawAttackRange, 0.1f, bodySlamRange);
        pawPrepTime = Mathf.Max(0f, pawPrepTime);
        pawSwingCount = Mathf.Max(1, pawSwingCount);
        pawSwingInterval = Mathf.Max(0.02f, pawSwingInterval);
        pawAttackMoveSpeed = Mathf.Max(0f, pawAttackMoveSpeed);
        pawAttackHeight = Mathf.Max(0.1f, pawAttackHeight);
        pawDamagePerSwing = Mathf.Max(0f, pawDamagePerSwing);
        attackRecoveryTime = Mathf.Max(0f, attackRecoveryTime);
        attackCooldown = Mathf.Max(0f, attackCooldown);
        attackAnimationLength = Mathf.Max(0.01f, attackAnimationLength);
    }
}
