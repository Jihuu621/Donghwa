using UnityEngine;
using UnityEngine.Serialization;

public class BirdSetup : EnemyAIBase
{
    private static readonly int MoveAnimation = Animator.StringToHash("Base Layer.Bird_Move");
    private static readonly int AttackAnimation = Animator.StringToHash("Base Layer.Bird_Attack");
    private const float DivePoseNormalizedTime = 0.9f;
    // The dive artwork points horizontally left by default.
    private const float LeftDiveBaseAngle = 180f;
    private const float RightDiveBaseAngle = 0f;

    private enum State
    {
        None,
        Flying,
        Positioning,
        Preparing,
        Diving,
        Embedded,
        PullingOut,
        Recovering,
        Stunned
    }

    [Header("Flight")]
    [SerializeField] private Vector2 patrolAreaSize = new Vector2(8f, 3f);
    [FormerlySerializedAs("flightSpeed")]
    [SerializeField, Min(0.1f)] private float horizontalFlightSpeed = 2.5f;
    [SerializeField, Min(0.1f)] private float verticalFlightSpeed = 4.5f;
    [SerializeField, Min(0.1f)] private float flightResponsiveness = 2.2f;
    [SerializeField, Min(0.01f)] private float flightSmoothTime = 0.22f;
    [SerializeField, Min(0.1f)] private float patrolTargetReachDistance = 0.5f;
    [SerializeField, Min(0.1f)] private float patrolTargetInterval = 2f;
    [SerializeField, Min(0f)] private float hoverAmplitude = 0.25f;
    [SerializeField, Min(0.1f)] private float hoverFrequency = 1.5f;

    [Header("Detection")]
    [SerializeField, Min(0.1f)] private float detectionRange = 9f;
    [SerializeField, Min(0f)] private float minimumDiveDrop = 0.75f;
    [SerializeField, Min(0f)] private float attackCooldown = 1f;

    [Header("Dive")]
    [SerializeField, Min(0.1f)] private float diveStandoffDistance = 4.5f;
    [SerializeField, Min(0.1f)] private float diveStandoffHeight = 3f;
    [SerializeField, Min(0.1f)] private float positioningSpeedMultiplier = 1.35f;
    [SerializeField, Min(0.1f)] private float positioningReachDistance = 0.65f;
    [SerializeField, Min(0.1f)] private float positioningTimeout = 2f;
    [SerializeField, Min(0f)] private float prepareDuration = 0.55f;
    [SerializeField, Min(0.1f)] private float diveSpeed = 16f;
    [SerializeField, Min(0.1f)] private float maximumDiveDuration = 2f;
    [SerializeField, Range(0.1f, 0.9f)] private float minimumHorizontalDiveRatio = 0.55f;
    [SerializeField, Range(0.2f, 0.95f)] private float maximumVerticalDiveRatio = 0.75f;
    [SerializeField, Range(0.25f, 2f)] private float maximumDiveSlope = 1f;
    [SerializeField] private float diveTargetYOffset = -0.25f;
    [SerializeField, Min(0.1f)] private float diveHitRadius = 0.65f;
    [SerializeField, Min(0.05f)] private float impactCastRadius = 0.4f;
    [SerializeField] private LayerMask impactMask = (1 << 3) | (1 << 9);

    [Header("Beak Recovery")]
    [SerializeField, Min(0f)] private float embeddedDuration = 0.65f;
    [SerializeField, Min(0.01f)] private float pullOutDuration = 0.3f;
    [SerializeField, Min(0.1f)] private float pullOutDistance = 1.25f;
    [SerializeField, Min(0f)] private float pullOutLift = 0.35f;
    [SerializeField, Min(0f)] private float recoveryDuration = 0.6f;
    [SerializeField, Min(0f)] private float recoveryRiseHeight = 1.5f;

    [Header("Animation")]
    [SerializeField, Min(0.01f)] private float attackAnimationLength = 0.4167f;
    [SerializeField, Range(0f, 60f)] private float maximumDiveVisualTilt = 28f;

    private State _state;
    private Vector2 _patrolCenter;
    private Vector2 _patrolTarget;
    private Vector2 _flightVelocitySmooth;
    private Vector2 _diveDirection;
    private Vector2 _previousDivePosition;
    private float _stateTimer;
    private float _patrolTargetTimer;
    private float _attackCooldownTimer;
    private float _stunDuration;
    private bool _dealtDiveDamage;
    private Color _normalColor;
    private float _attackApproachSide;
    private Vector2 _pullOutStart;
    private Vector2 _pullOutTarget;
    private Vector2 _recoveryTarget;
    private Quaternion _embeddedRotation;

    protected override void Awake()
    {
        base.Awake();
        _patrolCenter = transform.position;
        _normalColor = Fsm.Sr != null ? Fsm.Sr.color : Color.white;

        if (Fsm.Rb != null)
        {
            Fsm.Rb.gravityScale = 0f;
            Fsm.Rb.freezeRotation = true;
            Fsm.Rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            Fsm.Rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
    }

    private void Start()
    {
        PickPatrolTarget();
        ChangeState(State.Flying);
    }

    private void Update()
    {
        _attackCooldownTimer = Mathf.Max(0f, _attackCooldownTimer - Time.deltaTime);

        switch (_state)
        {
            case State.Flying: UpdateFlying(); break;
            case State.Positioning: UpdatePositioning(); break;
            case State.Preparing: UpdatePreparing(); break;
            case State.Diving: UpdateDiving(); break;
            case State.Embedded: UpdateEmbedded(); break;
            case State.PullingOut: UpdatePullingOut(); break;
            case State.Recovering: UpdateRecovering(); break;
            case State.Stunned: UpdateStunned(); break;
        }
    }

    public override bool TryStun(float duration)
    {
        _stunDuration = Mathf.Max(_stunDuration, duration);
        ChangeState(State.Stunned);
        return true;
    }

    private void ChangeState(State next)
    {
        if (_state == next)
        {
            if (next == State.Stunned) _stateTimer = 0f;
            return;
        }

        _state = next;
        _stateTimer = 0f;

        switch (next)
        {
            case State.Flying:
                RestoreFlightVisuals();
                PlayAnimation(MoveAnimation, 1f, 0f);
                break;
            case State.Positioning:
                RestoreFlightVisuals();
                PickAttackApproachSide();
                PlayAnimation(MoveAnimation, 1.2f, 0f);
                break;
            case State.Preparing:
                StopMovement();
                FacePlayer();
                PlayAnimation(AttackAnimation, attackAnimationLength / Mathf.Max(prepareDuration, 0.01f), 0f);
                break;
            case State.Diving:
                StartDive();
                break;
            case State.Embedded:
                StopMovement();
                _embeddedRotation = transform.rotation;
                HoldDivePose();
                break;
            case State.PullingOut:
                BeginPullOut();
                break;
            case State.Recovering:
                RestoreFlightVisuals();
                transform.rotation = Quaternion.identity;
                PlayAnimation(MoveAnimation, 1f, 0f);
                _recoveryTarget = GetRecoveryTarget();
                break;
            case State.Stunned:
                StopMovement();
                transform.rotation = Quaternion.identity;
                if (Fsm.Sr != null) Fsm.Sr.color = Color.cyan;
                break;
        }
    }

    private void UpdateFlying()
    {
        if (CanStartAttack())
        {
            ChangeState(State.Positioning);
            return;
        }

        _patrolTargetTimer -= Time.deltaTime;
        if (_patrolTargetTimer <= 0f || Vector2.Distance(transform.position, _patrolTarget) <= patrolTargetReachDistance)
        {
            PickPatrolTarget();
        }

        MoveFlightTowards(_patrolTarget);
    }

    private void UpdatePositioning()
    {
        if (Fsm.Player == null || Vector2.Distance(transform.position, Fsm.Player.position) > detectionRange * 1.5f)
        {
            ChangeState(State.Flying);
            return;
        }

        Vector2 preparationPoint = (Vector2)Fsm.Player.position +
                                   new Vector2(_attackApproachSide * diveStandoffDistance, diveStandoffHeight);
        MoveFlightTowards(preparationPoint, positioningSpeedMultiplier);

        _stateTimer += Time.deltaTime;
        if (Vector2.Distance(transform.position, preparationPoint) <= positioningReachDistance ||
            _stateTimer >= positioningTimeout)
        {
            ChangeState(State.Preparing);
        }
    }

    private void UpdatePreparing()
    {
        StopMovement();
        FacePlayer();
        _stateTimer += Time.deltaTime;
        if (_stateTimer >= prepareDuration) ChangeState(State.Diving);
    }

    private void StartDive()
    {
        if (Fsm.Rb == null)
        {
            ChangeState(State.Recovering);
            return;
        }

        Vector2 origin = Fsm.Rb.position;
        Vector2 target = Fsm.Player != null
            ? (Vector2)Fsm.Player.position + Vector2.up * diveTargetYOffset
            : origin + new Vector2(Fsm.Sr != null && Fsm.Sr.flipX ? 1f : -1f, -1f);

        _diveDirection = (target - origin).normalized;
        float horizontalSign = Mathf.Abs(_diveDirection.x) > 0.01f
            ? Mathf.Sign(_diveDirection.x)
            : Fsm.Sr != null && Fsm.Sr.flipX ? 1f : -1f;
        float horizontalAmount = Mathf.Max(Mathf.Abs(_diveDirection.x), minimumHorizontalDiveRatio);
        float verticalAmount = Mathf.Clamp(Mathf.Abs(_diveDirection.y), 0.35f, maximumVerticalDiveRatio);
        verticalAmount = Mathf.Min(verticalAmount, horizontalAmount * maximumDiveSlope);
        _diveDirection.x = horizontalSign * horizontalAmount;
        _diveDirection.y = -verticalAmount;
        _diveDirection.Normalize();

        _dealtDiveDamage = false;
        _previousDivePosition = origin;
        Fsm.Rb.linearVelocity = _diveDirection * diveSpeed;
        HoldDivePose();
        SetDiveVisualDirection();
    }

    private void UpdateDiving()
    {
        if (Fsm.Rb == null) return;

        _stateTimer += Time.deltaTime;
        Vector2 currentPosition = Fsm.Rb.position;
        if (TryFindImpact(_previousDivePosition, currentPosition, out _))
        {
            ChangeState(State.Embedded);
            return;
        }

        _previousDivePosition = currentPosition;
        TryDealDiveDamage();
        Fsm.Rb.linearVelocity = _diveDirection * diveSpeed;

        if (_stateTimer >= maximumDiveDuration)
        {
            _attackCooldownTimer = attackCooldown;
            ChangeState(State.Recovering);
        }
    }

    private bool TryFindImpact(Vector2 start, Vector2 end, out RaycastHit2D impact)
    {
        Vector2 delta = end - start;
        float distance = delta.magnitude;
        if (distance < 0.001f)
        {
            impact = default;
            return false;
        }

        impact = Physics2D.CircleCast(start, impactCastRadius, delta / distance, distance, impactMask);
        return impact.collider != null;
    }

    private void UpdateEmbedded()
    {
        StopMovement();
        _stateTimer += Time.deltaTime;
        if (_stateTimer >= embeddedDuration) ChangeState(State.PullingOut);
    }

    private void UpdatePullingOut()
    {
        _stateTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(_stateTimer / pullOutDuration);
        float easedProgress = Mathf.SmoothStep(0f, 1f, progress);

        if (Fsm.Rb != null)
        {
            Fsm.Rb.linearVelocity = Vector2.zero;
            Fsm.Rb.MovePosition(Vector2.Lerp(_pullOutStart, _pullOutTarget, easedProgress));
        }

        float rotationProgress = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.25f, 1f, progress));
        transform.rotation = Quaternion.Slerp(_embeddedRotation, Quaternion.identity, rotationProgress);
        if (progress < 1f) return;

        StopMovement();
        transform.rotation = Quaternion.identity;
        _attackCooldownTimer = attackCooldown;
        ChangeState(State.Recovering);
    }

    private void UpdateRecovering()
    {
        MoveFlightTowards(_recoveryTarget);
        _stateTimer += Time.deltaTime;
        if (_stateTimer < recoveryDuration) return;

        PickPatrolTarget();
        ChangeState(State.Flying);
    }

    private void BeginPullOut()
    {
        StopMovement();
        _pullOutStart = Fsm.Rb != null ? Fsm.Rb.position : (Vector2)transform.position;
        _pullOutTarget = _pullOutStart - _diveDirection * pullOutDistance + Vector2.up * pullOutLift;
        _embeddedRotation = transform.rotation;
        PlayAnimation(MoveAnimation, 1f, 0f);
    }

    private void HoldDivePose()
    {
        PlayAnimation(AttackAnimation, 0f, DivePoseNormalizedTime);
    }

    private Vector2 GetRecoveryTarget()
    {
        Vector2 currentPosition = Fsm.Rb != null ? Fsm.Rb.position : (Vector2)transform.position;
        Vector2 target = currentPosition + Vector2.up * recoveryRiseHeight;
        Vector2 halfSize = patrolAreaSize * 0.5f;
        target.x = Mathf.Clamp(target.x, _patrolCenter.x - halfSize.x, _patrolCenter.x + halfSize.x);
        target.y = Mathf.Clamp(target.y, _patrolCenter.y - halfSize.y, _patrolCenter.y + halfSize.y);
        return target;
    }

    private void UpdateStunned()
    {
        StopMovement();
        _stateTimer += Time.deltaTime;
        if (_stateTimer < _stunDuration) return;

        _stunDuration = 0f;
        _attackCooldownTimer = attackCooldown;
        ChangeState(State.Recovering);
    }

    private bool CanStartAttack()
    {
        if (_attackCooldownTimer > 0f || Fsm.Player == null) return false;

        Vector2 toPlayer = Fsm.Player.position - transform.position;
        return toPlayer.magnitude <= detectionRange && toPlayer.y <= -minimumDiveDrop;
    }

    private void TryDealDiveDamage()
    {
        if (_dealtDiveDamage || Fsm.Player == null || Fsm.Rb == null) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(Fsm.Rb.position, diveHitRadius);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].transform.root != Fsm.Player.root) continue;

            IDamageable player = hits[i].GetComponentInParent<IDamageable>();
            if (player != null) player.TakeDamage(Fsm.Data != null ? Fsm.Data.Damage : 10f, gameObject);
            _dealtDiveDamage = true;
            return;
        }
    }

    private void MoveFlightTowards(Vector2 target, float speedMultiplier = 1f)
    {
        if (Fsm.Rb == null) return;

        Vector2 hoverOffset = Vector2.up * (Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude);
        Vector2 targetDelta = target + hoverOffset - Fsm.Rb.position;
        float horizontalSpeed = horizontalFlightSpeed * speedMultiplier;
        float verticalSpeed = verticalFlightSpeed * speedMultiplier;
        Vector2 desiredVelocity = new Vector2(
            Mathf.Clamp(targetDelta.x * flightResponsiveness, -horizontalSpeed, horizontalSpeed),
            Mathf.Clamp(targetDelta.y * flightResponsiveness, -verticalSpeed, verticalSpeed));
        Fsm.Rb.linearVelocity = Vector2.SmoothDamp(
            Fsm.Rb.linearVelocity,
            desiredVelocity,
            ref _flightVelocitySmooth,
            flightSmoothTime,
            Mathf.Infinity,
            Time.deltaTime);
        FaceDirection(Fsm.Rb.linearVelocity.x);
    }

    private void PickAttackApproachSide()
    {
        if (Fsm.Player == null)
        {
            _attackApproachSide = Random.value < 0.5f ? -1f : 1f;
            return;
        }

        float relativeX = transform.position.x - Fsm.Player.position.x;
        _attackApproachSide = Mathf.Abs(relativeX) > 0.1f
            ? Mathf.Sign(relativeX)
            : Random.value < 0.5f ? -1f : 1f;
    }

    private void PickPatrolTarget()
    {
        Vector2 halfSize = patrolAreaSize * 0.5f;
        _patrolTarget = _patrolCenter + new Vector2(
            Random.Range(-halfSize.x, halfSize.x),
            Random.Range(-halfSize.y, halfSize.y));
        _patrolTargetTimer = patrolTargetInterval;
    }

    private void FacePlayer()
    {
        if (Fsm.Player != null) FaceDirection(Fsm.Player.position.x - transform.position.x);
    }

    private void FaceDirection(float directionX)
    {
        if (Fsm.Sr != null && Mathf.Abs(directionX) > 0.01f) Fsm.Sr.flipX = directionX > 0f;
    }

    private void SetDiveVisualDirection()
    {
        bool facesRight = _diveDirection.x > 0f;
        if (Fsm.Sr != null) Fsm.Sr.flipX = facesRight;

        float baseAngle = facesRight ? RightDiveBaseAngle : LeftDiveBaseAngle;
        float targetAngle = Mathf.Atan2(_diveDirection.y, _diveDirection.x) * Mathf.Rad2Deg;
        float correctionAngle = Mathf.DeltaAngle(baseAngle, targetAngle);
        correctionAngle = Mathf.Clamp(correctionAngle, -maximumDiveVisualTilt, maximumDiveVisualTilt);
        transform.rotation = Quaternion.Euler(0f, 0f, correctionAngle);
    }

    private void RestoreFlightVisuals()
    {
        transform.rotation = Quaternion.identity;
        if (Fsm.Sr != null) Fsm.Sr.color = _normalColor;
    }

    private void StopMovement()
    {
        if (Fsm.Rb != null) Fsm.Rb.linearVelocity = Vector2.zero;
        _flightVelocitySmooth = Vector2.zero;
    }

    private void PlayAnimation(int stateHash, float speed, float normalizedTime)
    {
        if (Fsm.Anim == null) return;
        Fsm.Anim.speed = speed;
        Fsm.Anim.Play(stateHash, 0, normalizedTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleDiveContact(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleDiveContact(collision.collider);
    }

    private void HandleDiveContact(Collider2D other)
    {
        if (_state != State.Diving || other == null) return;

        if (Fsm.Player != null && other.transform.root == Fsm.Player.root)
        {
            TryDealDiveDamage();
            return;
        }

        if (IsImpactSurface(other))
        {
            ChangeState(State.Embedded);
        }
    }

    private bool IsImpactSurface(Collider2D other)
    {
        int layerBit = 1 << other.gameObject.layer;
        return (impactMask.value & layerBit) != 0 ||
               other.CompareTag("Ground") ||
               other.GetComponentInParent<PlatformEffector2D>() != null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector2 center = Application.isPlaying ? _patrolCenter : (Vector2)transform.position;
        Gizmos.DrawWireCube(center, patrolAreaSize);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }

    private void OnValidate()
    {
        patrolAreaSize.x = Mathf.Max(1f, patrolAreaSize.x);
        patrolAreaSize.y = Mathf.Max(1f, patrolAreaSize.y);
        horizontalFlightSpeed = Mathf.Max(0.1f, horizontalFlightSpeed);
        verticalFlightSpeed = Mathf.Max(0.1f, verticalFlightSpeed);
        flightResponsiveness = Mathf.Max(0.1f, flightResponsiveness);
        flightSmoothTime = Mathf.Max(0.01f, flightSmoothTime);
        patrolTargetReachDistance = Mathf.Max(0.1f, patrolTargetReachDistance);
        patrolTargetInterval = Mathf.Max(0.1f, patrolTargetInterval);
        detectionRange = Mathf.Max(0.1f, detectionRange);
        prepareDuration = Mathf.Max(0f, prepareDuration);
        diveStandoffDistance = Mathf.Max(0.1f, diveStandoffDistance);
        diveStandoffHeight = Mathf.Max(0.1f, diveStandoffHeight);
        positioningSpeedMultiplier = Mathf.Max(0.1f, positioningSpeedMultiplier);
        positioningReachDistance = Mathf.Max(0.1f, positioningReachDistance);
        positioningTimeout = Mathf.Max(0.1f, positioningTimeout);
        diveSpeed = Mathf.Max(0.1f, diveSpeed);
        maximumDiveDuration = Mathf.Max(0.1f, maximumDiveDuration);
        diveHitRadius = Mathf.Max(0.1f, diveHitRadius);
        impactCastRadius = Mathf.Max(0.05f, impactCastRadius);
        pullOutDuration = Mathf.Max(0.01f, pullOutDuration);
        pullOutDistance = Mathf.Max(0.1f, pullOutDistance);
        pullOutLift = Mathf.Max(0f, pullOutLift);
        recoveryRiseHeight = Mathf.Max(0f, recoveryRiseHeight);
        attackAnimationLength = Mathf.Max(0.01f, attackAnimationLength);
    }
}
