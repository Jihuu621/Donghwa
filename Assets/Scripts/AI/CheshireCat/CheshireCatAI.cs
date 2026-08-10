using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class CheshireCatAI : EnemyAIBase
{
    private const float TeleportAnimationLength = 0.3f;
    private const int MaxPatternATeleports = 60;
    private static readonly int IdleAnimationState = Animator.StringToHash("Base Layer.Cat_Idle");
    private static readonly int TeleportAnimationState = Animator.StringToHash("Base Layer.Cat_Attack1");
    private static readonly int TeleportAppearAnimationState = Animator.StringToHash("Base Layer.Cat_TeleportAppear");

    public enum State
    {
        None, Idle, SmokeEnter, Teleport, SmokeAppear, RangedAttack, ScratchWindup, ScratchDash,
        PatternBSmokeEnter, PatternBSetup, PatternBSmokeAppear, PatternBActive, PatternBExit,
        PatternCSmokeEnter, PatternCTeleport, PatternCSmokeAppear, PatternCCharge, PatternCImpactPause,
        PatternCScratchWindup, PatternCScratchDash,
        Recovery, Stunned, Groggy
    }

    [Header("Pattern")]
    [SerializeField, Min(0f)] private float idleDuration = 0.75f;
    [SerializeField, Min(0f)] private float smokeDuration = 0.45f;
    [SerializeField, Min(0f)] private float recoveryDuration = 0.8f;
    [SerializeField, Min(1)] private int teleportCountMin = 3;
    [SerializeField, Min(1)] private int teleportCountMax = 6;

    [Header("Teleport Area")]
    [FormerlySerializedAs("teleportAreaCenter")]
    [Tooltip("Offset from the boss's initial position. The area center stays fixed after Start.")]
    [SerializeField] private Vector2 teleportAreaOffset;
    [SerializeField] private Vector2 teleportAreaSize = new Vector2(16f, 8f);
    [SerializeField, Min(0f)] private float minimumTeleportDistance = 2f;
    [SerializeField, Min(0.1f)] private float teleportClearanceRadius = 0.8f;
    [SerializeField, Min(1)] private int teleportSearchAttempts = 24;
    [FormerlySerializedAs("teleportGroundMask")]
    [Tooltip("Teleport candidates overlapping these layers are rejected.")]
    [SerializeField] private LayerMask teleportObstacleMask = 1 << 3;

    [Header("Ranged Attack")]
    [SerializeField] private CheshireProjectile projectilePrefab;
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField, Min(0f)] private float rangedWindupDuration = 0.4f;
    [SerializeField, Min(0.1f)] private float projectileSpeed = 8f;
    [SerializeField, Range(5f, 75f)] private float diagonalAngle = 28f;

    [Header("Scratch Attack")]
    [SerializeField, Min(0.1f)] private float meleeTriggerRange = 3f;
    [SerializeField, Min(0.1f)] private float scratchHitRange = 1.8f;
    [SerializeField, Min(0f)] private float scratchWindupDuration = 1f;
    [SerializeField, Min(0.01f)] private float scratchDashDuration = 0.25f;
    [SerializeField, Min(0.1f)] private float scratchDashSpeed = 10f;

    [Header("Hover Movement")]
    [SerializeField, Min(0f)] private float hoverHorizontalAmplitude = 0.55f;
    [SerializeField, Min(0f)] private float hoverVerticalAmplitude = 0.75f;
    [SerializeField, Min(0.1f)] private float hoverDirectionIntervalMin = 0.45f;
    [SerializeField, Min(0.1f)] private float hoverDirectionIntervalMax = 0.9f;
    [SerializeField, Min(0.1f)] private float hoverMaxSpeed = 1.8f;
    [SerializeField, Min(0.1f)] private float hoverResponsiveness = 5f;

    [Header("Smoke Visual")]
    [SerializeField] private Color smokeTint = new Color(0.45f, 0.45f, 0.45f, 0.3f);
    [SerializeField] private Sprite idleSprite;

    [Header("Pattern B")]
    [SerializeField] private CheshireProjectile patternBProjectilePrefab;
    [SerializeField, Min(0f)] private float patternBInitialShotDelay = 1f;
    [SerializeField, Min(0f)] private float patternBMoveDurationAfterShot = 5f;
    [SerializeField, Min(0f)] private float patternBMoveSpeed = 3.5f;
    [SerializeField, Min(0.1f)] private float patternBDirectionIntervalMin = 1.25f;
    [SerializeField, Min(0.1f)] private float patternBDirectionIntervalMax = 2.5f;
    [SerializeField, Min(0.01f)] private float patternBTurnSmoothTime = 0.35f;
    [SerializeField, Min(0f)] private float patternBBoundaryTurnDistance = 1.25f;
    [SerializeField, Min(0.1f)] private float patternBHomingSpeed = 4.5f;
    [SerializeField, Min(0f)] private float patternBHomingTurnSpeed = 75f;
    [SerializeField, Min(0.1f)] private float patternBMainProjectileHealth = 3f;
    [SerializeField, Min(0.1f)] private float patternBCloneProjectileHealth = 1f;
    [SerializeField, Min(0.1f)] private float patternBCloneHealth = 1f;
    [Tooltip("Copy the main body's particle effect to Pattern B clones.")]
    [SerializeField] private bool patternBCloneParticlesEnabled = true;
    [SerializeField, Min(0.1f)] private float patternBProjectileScaleMultiplier = 3f;
    [SerializeField] private Color patternBMainProjectileColor = new Color(1f, 0.2f, 0.65f, 1f);
    [SerializeField] private Color patternBCloneProjectileColor = new Color(0.2f, 0.9f, 1f, 1f);

    [Header("Pattern B Clone Debuff")]
    [SerializeField] private StatusKeyword patternBCloneDebuff = StatusKeyword.None;
    [SerializeField, Min(0.1f)] private float patternBCloneDebuffDuration = 2f;
    [SerializeField, Min(0f)] private float patternBCloneDebuffValue = 0.3f;

    [Header("Pattern C - Charge Impact")]
    [SerializeField, Min(1)] private int patternCRepeatMin = 3;
    [SerializeField, Min(1)] private int patternCRepeatMax = 4;
    [SerializeField, Min(0.1f)] private float patternCChargeSpeed = 20f;
    [SerializeField, Min(0.1f)] private float patternCChargeMaxDuration = 3f;
    [SerializeField, Min(0.1f)] private float patternCImpactCastRadius = 0.65f;
    [SerializeField] private LayerMask patternCImpactMask = (1 << 3) | (1 << 9);
    [SerializeField, Min(0.1f)] private float patternCDirectHitRadius = 0.9f;
    [SerializeField, Min(0f)] private float patternCDirectHitDamage = 25f;
    [SerializeField, Min(0.1f)] private float patternCShockwaveRadius = 4f;
    [SerializeField, Min(0f)] private float patternCShockwaveDamage = 15f;
    [SerializeField, Range(0f, 1f)] private float patternCShockwaveSlowAmount = 0.3f;
    [SerializeField, Min(0.1f)] private float patternCShockwaveSlowDuration = 2f;
    [SerializeField, Min(0f)] private float patternCImpactPauseDuration = 0.35f;
    [SerializeField, Min(0.1f)] private float patternCThreadStunDuration = 2f;
    [SerializeField, Min(0.01f)] private float patternCThreadDetectionRadius = 0.35f;
    [SerializeField, Min(1)] private int patternCCountersForGroggy = 3;
    [SerializeField, Min(0.1f)] private float patternCGroggyDuration = 3f;
    [SerializeField, Min(1f)] private float patternCGroggyNeedleDamageMultiplier = 3f;
    [FormerlySerializedAs("patternCCounterReboundForce")]
    [SerializeField, Min(0f)] private float patternCCounterReboundSpeed = 16f;
    [SerializeField, Min(0.01f)] private float patternCCounterReboundDuration = 0.18f;
    [SerializeField, Min(0f)] private float patternCCounterReboundLift = 2f;
    [SerializeField] private Color patternCGroggyTint = new Color(1f, 0.82f, 0.25f, 1f);
    [SerializeField, Min(0.05f)] private float patternCShockwaveVisualDuration = 0.4f;
    [SerializeField] private Color patternCShockwaveColor = new Color(0.35f, 0.9f, 1f, 0.9f);

    [Header("Afterimage")]
    [SerializeField, Min(0.05f)] private float afterimageInterval = 0.14f;
    [SerializeField, Min(0.05f)] private float afterimageLifetime = 0.32f;
    [SerializeField, Min(0.01f)] private float afterimageMinimumDistance = 0.25f;
    [SerializeField] private Color afterimageColor = new Color(0.75f, 0.9f, 1f, 0.16f);

    public State CurrentState { get; private set; }
    public bool IsSmokeForm { get; private set; }
    public bool IsGroggy => CurrentState == State.Groggy;
    public float GroggyNeedleDamageMultiplier => patternCGroggyNeedleDamageMultiplier;

    private float _stateTimer;
    private float _stunDuration;
    private int _teleportCount;
    private int _teleportsCompleted;
    private Vector2 _attackTarget;
    private Color _normalColor;
    private bool _hasAttacked;
    private Vector2 _fixedTeleportAreaCenter;
    private Collider2D _bodyCollider;
    private readonly List<CheshireCatClone> _clones = new List<CheshireCatClone>();
    private Vector2 _patternBMoveDirection;
    private Vector2 _patternBVelocitySmooth;
    private float _patternBDirectionTimer;
    private float _patternBShotTimer;
    private float _patternBPostShotTimer;
    private Vector2 _hoverAnchor;
    private Vector2 _hoverTargetPosition;
    private float _hoverDirectionTimer;
    private int _patternCRepeatCount;
    private int _patternCRepeatsCompleted;
    private Vector2 _patternCChargeDirection;
    private Vector2 _patternCPreviousPosition;
    private bool _patternCDirectHitDealt;
    private bool _resumePatternCAfterStun;
    private int _patternCCounterCount;
    private float _patternCCounterReboundTimer;
    private Vector2 _patternCCounterReboundVelocity;
    private CheshireAfterimageTrail _afterimageTrail;

    private void OnEnable()
    {
        CheshireProjectile.CloneDebuffRequested += HandleCloneDebuffRequested;
    }

    private void OnDisable()
    {
        CheshireProjectile.CloneDebuffRequested -= HandleCloneDebuffRequested;
    }

    private void Start()
    {
        _normalColor = Fsm.Sr != null ? Fsm.Sr.color : Color.white;
        Vector2 initialPosition = Fsm.Rb != null ? Fsm.Rb.position : (Vector2)transform.position;
        _fixedTeleportAreaCenter = initialPosition + teleportAreaOffset;
        _bodyCollider = GetComponent<Collider2D>();
        SetupAfterimage();
        PlayIdleAnimation();
        ChangeState(State.Idle);
    }

    private void Update()
    {
        switch (CurrentState)
        {
            case State.Idle: UpdateIdle(); break;
            case State.SmokeEnter: UpdateSmokeEnter(); break;
            case State.Teleport: UpdateTeleport(); break;
            case State.SmokeAppear: UpdateSmokeAppear(); break;
            case State.RangedAttack: UpdateRangedAttack(); break;
            case State.ScratchWindup: UpdateScratchWindup(); break;
            case State.ScratchDash: UpdateScratchDash(); break;
            case State.PatternBSmokeEnter: UpdatePatternBSmokeEnter(); break;
            case State.PatternBSetup: UpdatePatternBSetup(); break;
            case State.PatternBSmokeAppear: UpdatePatternBSmokeAppear(); break;
            case State.PatternBActive: UpdatePatternBActive(); break;
            case State.PatternBExit: UpdatePatternBExit(); break;
            case State.PatternCSmokeEnter: UpdatePatternCSmokeEnter(); break;
            case State.PatternCTeleport: UpdatePatternCTeleport(); break;
            case State.PatternCSmokeAppear: UpdatePatternCSmokeAppear(); break;
            case State.PatternCCharge: UpdatePatternCCharge(); break;
            case State.PatternCImpactPause: UpdatePatternCImpactPause(); break;
            case State.PatternCScratchWindup: UpdatePatternCScratchWindup(); break;
            case State.PatternCScratchDash: UpdatePatternCScratchDash(); break;
            case State.Recovery: UpdateRecovery(); break;
            case State.Stunned: UpdateStunned(); break;
            case State.Groggy: UpdateGroggy(); break;
        }
    }

    public override bool TryStun(float duration)
    {
        if (IsSmokeForm || IsGroggy) return false;
        if (CurrentState == State.PatternBActive) DestroyClonesImmediately();
        if (CurrentState == State.PatternCCharge)
        {
            _patternCRepeatsCompleted++;
            _resumePatternCAfterStun = true;
        }
        _stunDuration = Mathf.Max(_stunDuration, duration);
        ChangeState(State.Stunned);
        return true;
    }

    private void ChangeState(State next)
    {
        if (CurrentState == next)
        {
            if (next == State.Stunned) _stateTimer = 0f;
            return;
        }

        if (CurrentState == State.ScratchDash ||
            CurrentState == State.PatternCCharge ||
            CurrentState == State.PatternCScratchDash)
        {
            Fsm.StopAllMovement();
        }
        CurrentState = next;
        _stateTimer = 0f;
        _hasAttacked = false;

        switch (next)
        {
            case State.Idle:
            case State.Recovery:
                SetSmokeForm(false);
                Fsm.StopAllMovement();
                if (Fsm.Sr != null) Fsm.Sr.color = _normalColor;
                BeginHover();
                break;
            case State.SmokeEnter:
            case State.PatternBSmokeEnter:
            case State.PatternCSmokeEnter:
                SetSmokeForm(true);
                Fsm.StopAllMovement();
                PlaySmokeAnimation(TeleportAnimationState);
                break;
            case State.SmokeAppear:
            case State.PatternBSmokeAppear:
            case State.PatternCSmokeAppear:
                SetSmokeForm(true);
                Fsm.StopAllMovement();
                PlaySmokeAnimation(TeleportAppearAnimationState);
                break;
            case State.PatternBActive:
                SetSmokeForm(false);
                PlayIdleAnimation();
                Fsm.StopAllMovement();
                _patternBVelocitySmooth = Vector2.zero;
                _patternBShotTimer = patternBInitialShotDelay;
                _patternBPostShotTimer = patternBMoveDurationAfterShot;
                PickPatternBMoveDirection();
                break;
            case State.PatternBExit:
                SetSmokeForm(true);
                Fsm.StopAllMovement();
                PlaySmokeAnimation(TeleportAnimationState);
                BeginCloneDisappear();
                break;
            case State.PatternCCharge:
                SetSmokeForm(false);
                PlayIdleAnimation();
                BeginPatternCCharge();
                break;
            case State.PatternCImpactPause:
                SetSmokeForm(false);
                Fsm.StopAllMovement();
                break;
            case State.PatternCScratchWindup:
                SetSmokeForm(false);
                Fsm.StopAllMovement();
                break;
            case State.RangedAttack:
            case State.ScratchWindup:
                SetSmokeForm(false);
                Fsm.StopAllMovement();
                BeginHover();
                break;
            case State.Stunned:
                SetSmokeForm(false);
                Fsm.StopAllMovement();
                break;
            case State.Groggy:
                SetSmokeForm(false);
                Fsm.StopAllMovement();
                if (Fsm.Sr != null) Fsm.Sr.color = patternCGroggyTint;
                break;
        }
    }

    private void UpdateIdle()
    {
        UpdateHoverMovement();
        _stateTimer += Time.deltaTime;
        if (_stateTimer < idleDuration) return;
        int min = Mathf.Clamp(teleportCountMin, 1, MaxPatternATeleports);
        int max = Mathf.Clamp(teleportCountMax, min, MaxPatternATeleports);
        _teleportCount = Random.Range(min, max + 1);
        _teleportsCompleted = 0;
        ChangeState(State.SmokeEnter);
    }

    private void UpdateSmokeEnter()
    {
        _stateTimer += Time.deltaTime;
        if (_stateTimer >= smokeDuration) ChangeState(State.Teleport);
    }

    private void UpdateTeleport()
    {
        TeleportToRandomMapPosition();
        ChangeState(State.SmokeAppear);
    }

    private void UpdateSmokeAppear()
    {
        _stateTimer += Time.deltaTime;
        if (_stateTimer < smokeDuration) return;

        PlayIdleAnimation();
        SetSmokeForm(false);
        Vector2 bossPosition = Fsm.Rb != null ? Fsm.Rb.position : (Vector2)transform.position;
        bool playerIsClose = Fsm.Player != null && Vector2.Distance(bossPosition, Fsm.Player.position) <= meleeTriggerRange;
        ChangeState(playerIsClose ? State.ScratchWindup : State.RangedAttack);
    }

    private void UpdateRangedAttack()
    {
        FacePlayer();
        UpdateHoverMovement();
        _stateTimer += Time.deltaTime;
        if (_hasAttacked || _stateTimer < rangedWindupDuration) return;
        FireRandomRangedAttack();
        _hasAttacked = true;
        CompleteAttack();
    }

    private void UpdateScratchWindup()
    {
        FacePlayer();
        UpdateHoverMovement();
        _stateTimer += Time.deltaTime;
        if (_stateTimer < scratchWindupDuration) return;
        _attackTarget = Fsm.Player != null ? Fsm.Player.position : transform.position;
        ChangeState(State.ScratchDash);
    }

    private void UpdateScratchDash()
    {
        MoveTowards(_attackTarget, scratchDashSpeed);
        _stateTimer += Time.deltaTime;
        if (_stateTimer < scratchDashDuration) return;
        Fsm.StopAllMovement();
        Fsm.PerformAttack(scratchHitRange);
        CompleteAttack();
    }

    private void UpdateRecovery()
    {
        UpdateHoverMovement();
        _stateTimer += Time.deltaTime;
        if (_stateTimer >= recoveryDuration) ChangeState(State.Idle);
    }

    private void UpdateStunned()
    {
        UpdatePatternCCounterRebound();
        _stateTimer += Time.deltaTime;
        if (_stateTimer < _stunDuration) return;
        _stunDuration = 0f;

        if (_resumePatternCAfterStun)
        {
            _resumePatternCAfterStun = false;
            ChangeState(_patternCRepeatsCompleted >= _patternCRepeatCount
                ? State.Recovery
                : State.PatternCSmokeEnter);
            return;
        }

        ChangeState(State.Recovery);
    }

    private void UpdatePatternCCounterRebound()
    {
        if (_patternCCounterReboundTimer <= 0f || Fsm.Rb == null) return;

        _patternCCounterReboundTimer = Mathf.Max(0f, _patternCCounterReboundTimer - Time.deltaTime);
        float remaining = _patternCCounterReboundTimer / patternCCounterReboundDuration;
        Fsm.Rb.linearVelocity = _patternCCounterReboundVelocity * remaining * remaining;

        if (_patternCCounterReboundTimer <= 0f) Fsm.StopAllMovement();
    }

    private void CompleteAttack()
    {
        _teleportsCompleted++;
        ChangeState(_teleportsCompleted >= _teleportCount ? State.PatternBSmokeEnter : State.SmokeEnter);
    }

    private void UpdatePatternBSmokeEnter()
    {
        _stateTimer += Time.deltaTime;
        if (_stateTimer >= smokeDuration) ChangeState(State.PatternBSetup);
    }

    private void UpdatePatternBSetup()
    {
        DestroyClonesImmediately();
        Debug.Log("[CheshireCat] Pattern B started: spawning two clones.", this);

        List<Vector2> occupiedPositions = new List<Vector2>(3);
        Vector2 mainPosition = FindPatternBPosition(occupiedPositions);
        SetPosition(mainPosition);
        occupiedPositions.Add(mainPosition);

        for (int i = 0; i < 2; i++)
        {
            Vector2 clonePosition = FindPatternBPosition(occupiedPositions);
            occupiedPositions.Add(clonePosition);
            CreateClone(clonePosition, i);
        }

        ChangeState(State.PatternBSmokeAppear);
    }

    private void UpdatePatternBSmokeAppear()
    {
        _stateTimer += Time.deltaTime;
        if (_stateTimer >= smokeDuration) ChangeState(State.PatternBActive);
    }

    private void UpdatePatternBActive()
    {
        UpdatePatternBMovement();

        if (!_hasAttacked)
        {
            _patternBShotTimer -= Time.deltaTime;
            if (_patternBShotTimer > 0f) return;

            FirePatternBVolley();
            _hasAttacked = true;
            return;
        }

        _patternBPostShotTimer -= Time.deltaTime;
        if (_patternBPostShotTimer <= 0f) ChangeState(State.PatternBExit);
    }

    private void UpdatePatternBExit()
    {
        _stateTimer += Time.deltaTime;
        if (_stateTimer < smokeDuration) return;

        DestroyClonesImmediately();
        BeginPatternC();
    }

    private void BeginPatternC()
    {
        int min = Mathf.Max(1, patternCRepeatMin);
        int max = Mathf.Max(min, patternCRepeatMax);
        _patternCRepeatCount = Random.Range(min, max + 1);
        _patternCRepeatsCompleted = 0;
        _patternCCounterCount = 0;
        _resumePatternCAfterStun = false;
        ChangeState(State.PatternCTeleport);
    }

    private void UpdatePatternCSmokeEnter()
    {
        _stateTimer += Time.deltaTime;
        if (_stateTimer >= smokeDuration) ChangeState(State.PatternCTeleport);
    }

    private void UpdatePatternCTeleport()
    {
        TeleportToRandomMapPosition();
        ChangeState(State.PatternCSmokeAppear);
    }

    private void UpdatePatternCSmokeAppear()
    {
        _stateTimer += Time.deltaTime;
        if (_stateTimer >= smokeDuration) ChangeState(State.PatternCCharge);
    }

    private void BeginPatternCCharge()
    {
        if (Fsm.Rb == null) return;

        Vector2 target = Fsm.Player != null ? Fsm.Player.position : Fsm.Rb.position + Vector2.right;
        _patternCChargeDirection = (target - Fsm.Rb.position).normalized;
        if (_patternCChargeDirection.sqrMagnitude < 0.01f) _patternCChargeDirection = Vector2.right;

        _patternCPreviousPosition = Fsm.Rb.position;
        _patternCDirectHitDealt = false;
        Fsm.Rb.linearVelocity = _patternCChargeDirection * patternCChargeSpeed;
        if (Fsm.Sr != null && Mathf.Abs(_patternCChargeDirection.x) > 0.01f)
        {
            Fsm.Sr.flipX = _patternCChargeDirection.x > 0f;
        }
    }

    private void UpdatePatternCCharge()
    {
        if (Fsm.Rb == null)
        {
            CompletePatternCIteration();
            return;
        }

        _stateTimer += Time.deltaTime;
        Vector2 currentPosition = Fsm.Rb.position;
        if (DetectThreadAcrossCharge(_patternCPreviousPosition, currentPosition))
        {
            HandlePatternCCounter();
            return;
        }

        _patternCPreviousPosition = currentPosition;
        TryDealPatternCDirectHit();
        if (CurrentState != State.PatternCCharge) return;

        float castDistance = patternCChargeSpeed * Mathf.Max(Time.deltaTime, Time.fixedDeltaTime) + 0.1f;
        RaycastHit2D impact = Physics2D.CircleCast(
            currentPosition,
            patternCImpactCastRadius,
            _patternCChargeDirection,
            castDistance,
            patternCImpactMask);
        if (impact.collider != null)
        {
            Vector2 impactPosition = impact.point - _patternCChargeDirection * patternCImpactCastRadius;
            SetPosition(impactPosition);
            TriggerPatternCImpact(impact.point);
            return;
        }

        Fsm.Rb.linearVelocity = _patternCChargeDirection * patternCChargeSpeed;
        if (_stateTimer >= patternCChargeMaxDuration)
        {
            TriggerPatternCImpact(Fsm.Rb.position);
        }
    }

    private bool DetectThreadAcrossCharge(Vector2 start, Vector2 end)
    {
        Vector2 delta = end - start;
        float distance = delta.magnitude;
        if (distance < 0.001f) return false;

        RaycastHit2D[] hits = Physics2D.CircleCastAll(
            start,
            patternCThreadDetectionRadius,
            delta / distance,
            distance);
        for (int i = 0; i < hits.Length; i++)
        {
            if (IsPatternCCounterThread(hits[i].collider))
            {
                return true;
            }
        }

        return false;
    }

    private void TryDealPatternCDirectHit()
    {
        if (_patternCDirectHitDealt || Fsm.Player == null) return;

        Collider2D[] overlaps = Physics2D.OverlapCircleAll(Fsm.Rb.position, patternCDirectHitRadius);
        for (int i = 0; i < overlaps.Length; i++)
        {
            if (overlaps[i].transform.root != Fsm.Player.root) continue;
            DealPatternCDirectHit(overlaps[i]);
            return;
        }
    }

    private void DealPatternCDirectHit(Collider2D playerCollider)
    {
        if (_patternCDirectHitDealt || playerCollider == null) return;

        PlayerParry parry = playerCollider.GetComponentInParent<PlayerParry>();
        if (parry != null && parry.IsParryTime)
        {
            IDamageable guardedPlayer = playerCollider.GetComponentInParent<IDamageable>();
            if (guardedPlayer != null) guardedPlayer.TakeDamage(patternCDirectHitDamage, gameObject);
            _patternCDirectHitDealt = true;
            HandlePatternCCounter();
            return;
        }

        IDamageable player = playerCollider.GetComponentInParent<IDamageable>();
        if (player != null) player.TakeDamage(patternCDirectHitDamage, gameObject);
        _patternCDirectHitDealt = true;
    }

    private void TriggerPatternCImpact(Vector2 impactPosition)
    {
        if (CurrentState != State.PatternCCharge) return;

        Fsm.StopAllMovement();
        CheshireShockwaveVisual.Create(
            impactPosition,
            patternCShockwaveRadius,
            patternCShockwaveVisualDuration,
            patternCShockwaveColor);

        bool hitPlayer = ApplyPatternCShockwaveToPlayer(impactPosition);
        ChangeState(hitPlayer ? State.PatternCScratchWindup : State.PatternCImpactPause);
    }

    private bool ApplyPatternCShockwaveToPlayer(Vector2 impactPosition)
    {
        if (Fsm.Player == null) return false;

        Collider2D[] overlaps = Physics2D.OverlapCircleAll(impactPosition, patternCShockwaveRadius);
        for (int i = 0; i < overlaps.Length; i++)
        {
            if (overlaps[i].transform.root != Fsm.Player.root) continue;

            IDamageable player = overlaps[i].GetComponentInParent<IDamageable>();
            if (player != null) player.TakeDamage(patternCShockwaveDamage, gameObject);

            EffectManager statusEffects = Fsm.Player.GetComponent<EffectManager>();
            if (statusEffects == null) statusEffects = Fsm.Player.gameObject.AddComponent<EffectManager>();
            statusEffects.ApplyStatus(
                StatusKeyword.SpeedDown,
                patternCShockwaveSlowDuration,
                patternCShockwaveSlowAmount);
            return true;
        }

        return false;
    }

    private void UpdatePatternCImpactPause()
    {
        _stateTimer += Time.deltaTime;
        if (_stateTimer >= patternCImpactPauseDuration) CompletePatternCIteration();
    }

    private void UpdatePatternCScratchWindup()
    {
        FacePlayer();
        _stateTimer += Time.deltaTime;
        if (_stateTimer < scratchWindupDuration) return;

        _attackTarget = Fsm.Player != null ? Fsm.Player.position : transform.position;
        ChangeState(State.PatternCScratchDash);
    }

    private void UpdatePatternCScratchDash()
    {
        MoveTowards(_attackTarget, scratchDashSpeed);
        _stateTimer += Time.deltaTime;
        if (_stateTimer < scratchDashDuration) return;

        Fsm.StopAllMovement();
        Fsm.PerformAttack(scratchHitRange);
        CompletePatternCIteration();
    }

    private void CompletePatternCIteration()
    {
        _patternCRepeatsCompleted++;
        ChangeState(_patternCRepeatsCompleted >= _patternCRepeatCount
            ? State.Recovery
            : State.PatternCSmokeEnter);
    }

    private void HandlePatternCCounter()
    {
        if (CurrentState != State.PatternCCharge) return;

        _patternCCounterCount++;
        _patternCRepeatsCompleted++;
        Fsm.StopAllMovement();

        if (_patternCCounterCount >= patternCCountersForGroggy)
        {
            ChangeState(State.Groggy);
            return;
        }

        ChangeState(State.Stunned);
        if (Fsm.Rb != null)
        {
            Vector2 reboundDirection = -_patternCChargeDirection;
            _patternCCounterReboundVelocity = reboundDirection * patternCCounterReboundSpeed +
                                             Vector2.up * patternCCounterReboundLift;
            _patternCCounterReboundTimer = patternCCounterReboundDuration;
            Fsm.Rb.linearVelocity = _patternCCounterReboundVelocity;
        }

        _stunDuration = patternCThreadStunDuration;
        _resumePatternCAfterStun = true;
    }

    private void UpdateGroggy()
    {
        _stateTimer += Time.deltaTime;
        if (_stateTimer >= patternCGroggyDuration) ChangeState(State.Recovery);
    }

    private void UpdatePatternBMovement()
    {
        if (Fsm.Rb == null) return;

        _patternBDirectionTimer -= Time.deltaTime;
        if (_patternBDirectionTimer <= 0f) PickPatternBMoveDirection();

        Vector2 center = _fixedTeleportAreaCenter;
        Vector2 halfSize = teleportAreaSize * 0.5f;
        Vector2 position = Fsm.Rb.position;

        float horizontalMargin = Mathf.Min(patternBBoundaryTurnDistance, halfSize.x * 0.45f);
        float verticalMargin = Mathf.Min(patternBBoundaryTurnDistance, halfSize.y * 0.45f);

        if (position.x <= center.x - halfSize.x + horizontalMargin && _patternBMoveDirection.x < 0f ||
            position.x >= center.x + halfSize.x - horizontalMargin && _patternBMoveDirection.x > 0f)
        {
            _patternBMoveDirection.x *= -1f;
        }

        if (position.y <= center.y - halfSize.y + verticalMargin && _patternBMoveDirection.y < 0f ||
            position.y >= center.y + halfSize.y - verticalMargin && _patternBMoveDirection.y > 0f)
        {
            _patternBMoveDirection.y *= -1f;
        }

        Vector2 targetVelocity = _patternBMoveDirection.normalized * patternBMoveSpeed;
        Fsm.Rb.linearVelocity = Vector2.SmoothDamp(
            Fsm.Rb.linearVelocity,
            targetVelocity,
            ref _patternBVelocitySmooth,
            patternBTurnSmoothTime,
            Mathf.Infinity,
            Time.deltaTime);

        if (Fsm.Sr != null && Mathf.Abs(Fsm.Rb.linearVelocity.x) > 0.05f)
        {
            Fsm.Sr.flipX = Fsm.Rb.linearVelocity.x > 0f;
        }
    }

    private void PickPatternBMoveDirection()
    {
        _patternBMoveDirection = Random.insideUnitCircle.normalized;
        if (_patternBMoveDirection.sqrMagnitude < 0.01f) _patternBMoveDirection = Vector2.right;
        _patternBDirectionTimer = Random.Range(patternBDirectionIntervalMin, patternBDirectionIntervalMax);
    }

    private void BeginHover()
    {
        if (Fsm.Rb == null) return;

        _hoverAnchor = Fsm.Rb.position;
        _hoverTargetPosition = _hoverAnchor;
        _hoverDirectionTimer = 0f;
        PickHoverTarget();
    }

    private void UpdateHoverMovement()
    {
        if (Fsm.Rb == null) return;

        _hoverDirectionTimer -= Time.deltaTime;
        if (_hoverDirectionTimer <= 0f || Vector2.Distance(Fsm.Rb.position, _hoverTargetPosition) < 0.08f)
        {
            PickHoverTarget();
        }

        Vector2 targetVelocity = (_hoverTargetPosition - Fsm.Rb.position) * hoverResponsiveness;
        targetVelocity = Vector2.ClampMagnitude(targetVelocity, hoverMaxSpeed);
        float blend = 1f - Mathf.Exp(-hoverResponsiveness * Time.deltaTime);
        Fsm.Rb.linearVelocity = Vector2.Lerp(Fsm.Rb.linearVelocity, targetVelocity, blend);
    }

    private void PickHoverTarget()
    {
        Vector2 direction = Random.insideUnitCircle.normalized;
        if (direction.sqrMagnitude < 0.01f) direction = Vector2.up;

        float distanceScale = Random.Range(0.55f, 1f);
        Vector2 offset = new Vector2(
            direction.x * hoverHorizontalAmplitude,
            direction.y * hoverVerticalAmplitude) * distanceScale;
        Vector2 candidate = _hoverAnchor + offset;
        Vector2 halfSize = teleportAreaSize * 0.5f;
        candidate.x = Mathf.Clamp(candidate.x, _fixedTeleportAreaCenter.x - halfSize.x, _fixedTeleportAreaCenter.x + halfSize.x);
        candidate.y = Mathf.Clamp(candidate.y, _fixedTeleportAreaCenter.y - halfSize.y, _fixedTeleportAreaCenter.y + halfSize.y);

        _hoverTargetPosition = candidate;
        _hoverDirectionTimer = Random.Range(hoverDirectionIntervalMin, hoverDirectionIntervalMax);
    }

    public void FirePatternBProjectile(Vector2 origin, bool fromClone, GameObject source)
    {
        CheshireProjectile selectedPrefab = patternBProjectilePrefab != null ? patternBProjectilePrefab : projectilePrefab;
        if (selectedPrefab == null || Fsm.Player == null) return;

        Vector2 direction = ((Vector2)Fsm.Player.position - origin).normalized;
        if (direction.sqrMagnitude < 0.01f) direction = Vector2.right;
        direction = Rotate(direction, Random.Range(-18f, 18f));

        float damage = fromClone ? 0f : Fsm.Data != null ? Fsm.Data.Damage : 20f;
        float health = fromClone ? patternBCloneProjectileHealth : patternBMainProjectileHealth;
        Color color = fromClone ? patternBCloneProjectileColor : patternBMainProjectileColor;

        CheshireProjectile projectile = Instantiate(selectedPrefab, origin, Quaternion.identity);
        if (patternBProjectilePrefab == null)
        {
            projectile.transform.localScale *= patternBProjectileScaleMultiplier;
        }
        projectile.LaunchHoming(
            Fsm.Player,
            direction,
            patternBHomingSpeed,
            patternBHomingTurnSpeed,
            damage,
            health,
            color,
            fromClone,
            source);
    }

    public void SetPatternBCloneDebuff(StatusKeyword keyword, float duration, float value)
    {
        patternBCloneDebuff = keyword;
        patternBCloneDebuffDuration = Mathf.Max(0.1f, duration);
        patternBCloneDebuffValue = Mathf.Max(0f, value);
    }

    private void HandleCloneDebuffRequested(GameObject target, GameObject source)
    {
        if (patternBCloneDebuff == StatusKeyword.None || target == null || !IsOwnedClone(source)) return;

        EffectManager statusEffects = target.GetComponent<EffectManager>();
        if (statusEffects == null) statusEffects = target.AddComponent<EffectManager>();
        statusEffects.ApplyStatus(patternBCloneDebuff, patternBCloneDebuffDuration, patternBCloneDebuffValue);
    }

    private bool IsOwnedClone(GameObject source)
    {
        if (source == null) return false;

        for (int i = 0; i < _clones.Count; i++)
        {
            if (_clones[i] != null && _clones[i].gameObject == source) return true;
        }

        return false;
    }

    private void FirePatternBVolley()
    {
        FirePatternBProjectile(transform.position, false, gameObject);

        CheshireCatClone[] clones = _clones.ToArray();
        for (int i = 0; i < clones.Length; i++)
        {
            if (clones[i] != null) clones[i].FireProjectile();
        }
    }

    private void FireRandomRangedAttack()
    {
        if (projectilePrefab == null || Fsm.Player == null) return;
        Vector2 origin = projectileSpawnPoint != null ? projectileSpawnPoint.position : transform.position;
        Vector2 targetSnapshot = Fsm.Player.position;
        Vector2 baseDirection = (targetSnapshot - origin).normalized;
        if (baseDirection.sqrMagnitude < 0.01f) baseDirection = Vector2.right;
        float damage = Fsm.Data != null ? Fsm.Data.Damage : 20f;

        if (Random.value < 0.5f)
        {
            SpawnProjectile(origin, baseDirection, damage);
            SpawnProjectile(origin, Rotate(baseDirection, diagonalAngle*1.5f), damage);
            SpawnProjectile(origin, Rotate(baseDirection, -diagonalAngle*1.5f), damage);
            return;
        }

        SpawnProjectile(origin, Rotate(baseDirection, diagonalAngle), damage);
        SpawnProjectile(origin, Rotate(baseDirection, -diagonalAngle), damage);
        SpawnProjectile(origin, Rotate(baseDirection, diagonalAngle*3f), damage);
        SpawnProjectile(origin, Rotate(baseDirection, -diagonalAngle*3f), damage);
    }

    private void SpawnProjectile(Vector2 origin, Vector2 direction, float damage)
    {
        CheshireProjectile projectile = Instantiate(projectilePrefab, origin, Quaternion.identity);
        projectile.Launch(direction, projectileSpeed, damage, gameObject);
    }

    private Vector2 FindPatternBPosition(List<Vector2> occupiedPositions)
    {
        Vector2 halfSize = teleportAreaSize * 0.5f;
        Vector2 center = _fixedTeleportAreaCenter;
        float separation = Mathf.Max(1f, minimumTeleportDistance * 0.65f);

        for (int i = 0; i < teleportSearchAttempts; i++)
        {
            Vector2 candidate = new Vector2(
                Random.Range(center.x - halfSize.x, center.x + halfSize.x),
                Random.Range(center.y - halfSize.y, center.y + halfSize.y));

            if (Physics2D.OverlapCircle(candidate, teleportClearanceRadius, teleportObstacleMask) != null) continue;

            bool overlapsActor = false;
            for (int j = 0; j < occupiedPositions.Count; j++)
            {
                if (Vector2.Distance(candidate, occupiedPositions[j]) >= separation) continue;
                overlapsActor = true;
                break;
            }

            if (!overlapsActor) return candidate;
        }

        float angle = occupiedPositions.Count * 120f * Mathf.Deg2Rad;
        return center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Mathf.Min(halfSize.x, halfSize.y) * 0.5f;
    }

    private void CreateClone(Vector2 position, int index)
    {
        GameObject cloneObject = new GameObject($"CheshireCatClone_{index + 1}");
        cloneObject.tag = "Enemy";
        cloneObject.layer = gameObject.layer;
        cloneObject.transform.position = position;
        cloneObject.transform.localScale = transform.lossyScale;

        SpriteRenderer cloneRenderer = cloneObject.AddComponent<SpriteRenderer>();
        if (Fsm.Sr != null)
        {
            cloneRenderer.sprite = idleSprite != null ? idleSprite : Fsm.Sr.sprite;
            cloneRenderer.color = _normalColor;
            cloneRenderer.sharedMaterial = Fsm.Sr.sharedMaterial;
            cloneRenderer.sortingLayerID = Fsm.Sr.sortingLayerID;
            cloneRenderer.sortingOrder = Fsm.Sr.sortingOrder;
            cloneRenderer.flipX = Fsm.Sr.flipX;
        }

        CapsuleCollider2D cloneCollider = cloneObject.AddComponent<CapsuleCollider2D>();
        cloneCollider.isTrigger = true;
        if (_bodyCollider is CapsuleCollider2D bodyCapsule)
        {
            cloneCollider.offset = bodyCapsule.offset;
            cloneCollider.size = bodyCapsule.size;
            cloneCollider.direction = bodyCapsule.direction;
        }

        Rigidbody2D cloneRigidbody = cloneObject.AddComponent<Rigidbody2D>();
        cloneRigidbody.gravityScale = 0f;
        cloneRigidbody.freezeRotation = true;
        cloneRigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
        cloneRigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        if (Fsm.Anim != null)
        {
            Animator cloneAnimator = cloneObject.AddComponent<Animator>();
            cloneAnimator.runtimeAnimatorController = Fsm.Anim.runtimeAnimatorController;
        }

        if (patternBCloneParticlesEnabled) AddCloneParticles(cloneObject);

        CheshireAfterimageTrail cloneTrail = cloneObject.AddComponent<CheshireAfterimageTrail>();
        cloneTrail.Configure(afterimageInterval, afterimageLifetime, afterimageMinimumDistance, afterimageColor);
        cloneTrail.SetEmitting(false);

        CheshireCatClone clone = cloneObject.AddComponent<CheshireCatClone>();
        clone.Configure(
            this,
            smokeDuration,
            patternBCloneHealth,
            patternBMoveSpeed,
            _fixedTeleportAreaCenter,
            teleportAreaSize,
            patternBDirectionIntervalMin,
            patternBDirectionIntervalMax,
            patternBTurnSmoothTime,
            patternBBoundaryTurnDistance);
        _clones.Add(clone);
    }

    private void AddCloneParticles(GameObject cloneObject)
    {
        ParticleSystem source = GetComponent<ParticleSystem>();
        if (source == null) return;

        ParticleSystem clone = cloneObject.AddComponent<ParticleSystem>();
        clone.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        CopyParticleModules(source, clone);
        CopyParticleRenderer(source, clone);
        clone.Play(true);
    }

    private static void CopyParticleModules(ParticleSystem source, ParticleSystem target)
    {
        ParticleSystem.MainModule sourceMain = source.main;
        ParticleSystem.MainModule targetMain = target.main;
        targetMain.duration = sourceMain.duration;
        targetMain.loop = sourceMain.loop;
        targetMain.prewarm = sourceMain.prewarm;
        targetMain.startDelay = sourceMain.startDelay;
        targetMain.startLifetime = sourceMain.startLifetime;
        targetMain.startSpeed = sourceMain.startSpeed;
        targetMain.startSize3D = sourceMain.startSize3D;
        targetMain.startSize = sourceMain.startSize;
        targetMain.startSizeX = sourceMain.startSizeX;
        targetMain.startSizeY = sourceMain.startSizeY;
        targetMain.startSizeZ = sourceMain.startSizeZ;
        targetMain.startRotation3D = sourceMain.startRotation3D;
        targetMain.startRotation = sourceMain.startRotation;
        targetMain.startRotationX = sourceMain.startRotationX;
        targetMain.startRotationY = sourceMain.startRotationY;
        targetMain.startRotationZ = sourceMain.startRotationZ;
        targetMain.flipRotation = sourceMain.flipRotation;
        targetMain.startColor = sourceMain.startColor;
        targetMain.gravityModifier = sourceMain.gravityModifier;
        targetMain.simulationSpace = sourceMain.simulationSpace;
        targetMain.customSimulationSpace = sourceMain.customSimulationSpace;
        targetMain.simulationSpeed = sourceMain.simulationSpeed;
        targetMain.scalingMode = sourceMain.scalingMode;
        targetMain.playOnAwake = sourceMain.playOnAwake;
        targetMain.maxParticles = sourceMain.maxParticles;

        ParticleSystem.ShapeModule sourceShape = source.shape;
        ParticleSystem.ShapeModule targetShape = target.shape;
        targetShape.enabled = sourceShape.enabled;
        targetShape.shapeType = sourceShape.shapeType;
        targetShape.angle = sourceShape.angle;
        targetShape.radius = sourceShape.radius;
        targetShape.radiusThickness = sourceShape.radiusThickness;
        targetShape.arc = sourceShape.arc;
        targetShape.arcMode = sourceShape.arcMode;
        targetShape.arcSpread = sourceShape.arcSpread;
        targetShape.arcSpeed = sourceShape.arcSpeed;
        targetShape.length = sourceShape.length;
        targetShape.position = sourceShape.position;
        targetShape.rotation = sourceShape.rotation;
        targetShape.scale = sourceShape.scale;
        targetShape.alignToDirection = sourceShape.alignToDirection;
        targetShape.randomDirectionAmount = sourceShape.randomDirectionAmount;
        targetShape.sphericalDirectionAmount = sourceShape.sphericalDirectionAmount;
        targetShape.randomPositionAmount = sourceShape.randomPositionAmount;

        ParticleSystem.EmissionModule sourceEmission = source.emission;
        ParticleSystem.EmissionModule targetEmission = target.emission;
        targetEmission.enabled = sourceEmission.enabled;
        targetEmission.rateOverTime = sourceEmission.rateOverTime;
        targetEmission.rateOverDistance = sourceEmission.rateOverDistance;
        ParticleSystem.Burst[] bursts = new ParticleSystem.Burst[sourceEmission.burstCount];
        sourceEmission.GetBursts(bursts);
        targetEmission.SetBursts(bursts);

        ParticleSystem.ColorOverLifetimeModule sourceColor = source.colorOverLifetime;
        ParticleSystem.ColorOverLifetimeModule targetColor = target.colorOverLifetime;
        targetColor.enabled = sourceColor.enabled;
        targetColor.color = sourceColor.color;
    }

    private static void CopyParticleRenderer(ParticleSystem source, ParticleSystem target)
    {
        ParticleSystemRenderer sourceRenderer = source.GetComponent<ParticleSystemRenderer>();
        ParticleSystemRenderer targetRenderer = target.GetComponent<ParticleSystemRenderer>();
        if (sourceRenderer == null || targetRenderer == null) return;

        targetRenderer.sharedMaterials = sourceRenderer.sharedMaterials;
        targetRenderer.trailMaterial = sourceRenderer.trailMaterial;
        targetRenderer.renderMode = sourceRenderer.renderMode;
        targetRenderer.sortMode = sourceRenderer.sortMode;
        targetRenderer.alignment = sourceRenderer.alignment;
        targetRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
        targetRenderer.sortingOrder = sourceRenderer.sortingOrder;
        targetRenderer.minParticleSize = sourceRenderer.minParticleSize;
        targetRenderer.maxParticleSize = sourceRenderer.maxParticleSize;
        targetRenderer.cameraVelocityScale = sourceRenderer.cameraVelocityScale;
        targetRenderer.velocityScale = sourceRenderer.velocityScale;
        targetRenderer.lengthScale = sourceRenderer.lengthScale;
        targetRenderer.sortingFudge = sourceRenderer.sortingFudge;
        targetRenderer.normalDirection = sourceRenderer.normalDirection;
        targetRenderer.pivot = sourceRenderer.pivot;
        targetRenderer.flip = sourceRenderer.flip;
        targetRenderer.enableGPUInstancing = sourceRenderer.enableGPUInstancing;
        targetRenderer.allowRoll = sourceRenderer.allowRoll;
    }

    private void BeginCloneDisappear()
    {
        CheshireCatClone[] clones = _clones.ToArray();
        for (int i = 0; i < clones.Length; i++)
        {
            if (clones[i] != null) clones[i].BeginDisappear(smokeDuration);
        }
    }

    private void DestroyClonesImmediately()
    {
        CheshireCatClone[] clones = _clones.ToArray();
        _clones.Clear();
        for (int i = 0; i < clones.Length; i++)
        {
            if (clones[i] != null) Destroy(clones[i].gameObject);
        }
    }

    public void NotifyCloneDestroyed(CheshireCatClone clone)
    {
        _clones.Remove(clone);
    }

    private void SetPosition(Vector2 position)
    {
        Fsm.StopAllMovement();
        if (Fsm.Rb != null) Fsm.Rb.position = position;
        else transform.position = position;
    }

    private void SetupAfterimage()
    {
        if (_afterimageTrail == null)
        {
            _afterimageTrail = GetComponent<CheshireAfterimageTrail>();
            if (_afterimageTrail == null) _afterimageTrail = gameObject.AddComponent<CheshireAfterimageTrail>();
        }

        _afterimageTrail.Configure(afterimageInterval, afterimageLifetime, afterimageMinimumDistance, afterimageColor);
        _afterimageTrail.SetEmitting(true);
    }

    private void TeleportToRandomMapPosition()
    {
        Vector2 halfSize = teleportAreaSize * 0.5f;
        Vector2 center = _fixedTeleportAreaCenter;
        Vector2 currentPosition = Fsm.Rb != null ? Fsm.Rb.position : (Vector2)transform.position;
        float bodyRadius = teleportClearanceRadius;
        if (_bodyCollider != null)
        {
            Vector2 bodyExtents = _bodyCollider.bounds.extents;
            bodyRadius = Mathf.Max(bodyRadius, bodyExtents.x, bodyExtents.y);
        }

        for (int i = 0; i < teleportSearchAttempts; i++)
        {
            Vector2 candidate = new Vector2(
                Random.Range(center.x - halfSize.x, center.x + halfSize.x),
                Random.Range(center.y - halfSize.y, center.y + halfSize.y));

            if (Vector2.Distance(candidate, currentPosition) < minimumTeleportDistance) continue;
            if (Physics2D.OverlapCircle(candidate, bodyRadius, teleportObstacleMask) != null) continue;

            Fsm.StopAllMovement();
            if (Fsm.Rb != null) Fsm.Rb.position = candidate;
            else transform.position = candidate;
            return;
        }

        Debug.LogWarning("[CheshireCat] No open teleport position found inside the fixed teleport area.", this);
    }

    private void MoveTowards(Vector2 target, float speed)
    {
        if (Fsm.Rb == null) return;

        Vector2 delta = target - Fsm.Rb.position;
        if (delta.sqrMagnitude < 0.0025f)
        {
            Fsm.StopAllMovement();
            return;
        }

        float speedWithoutOvershoot = delta.magnitude / Mathf.Max(Time.fixedDeltaTime, 0.0001f);
        Fsm.Rb.linearVelocity = delta.normalized * Mathf.Min(speed, speedWithoutOvershoot);
    }

    private void FacePlayer()
    {
        if (Fsm.Player != null && Fsm.Sr != null) Fsm.Sr.flipX = Fsm.Player.position.x > transform.position.x;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandlePatternCContact(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandlePatternCContact(collision.collider);
    }

    private void HandlePatternCContact(Collider2D other)
    {
        if (CurrentState != State.PatternCCharge || other == null) return;

        if (IsPatternCCounterThread(other))
        {
            HandlePatternCCounter();
            return;
        }

        if (Fsm.Player != null && other.transform.root == Fsm.Player.root)
        {
            DealPatternCDirectHit(other);
            return;
        }

        if (IsPatternCImpactSurface(other))
        {
            TriggerPatternCImpact(other.ClosestPoint(transform.position));
        }
    }

    private bool IsPatternCImpactSurface(Collider2D other)
    {
        int layerBit = 1 << other.gameObject.layer;
        return (patternCImpactMask.value & layerBit) != 0 ||
               other.CompareTag("Ground") ||
               other.GetComponentInParent<PlatformEffector2D>() != null;
    }

    private static bool IsPatternCCounterThread(Collider2D other)
    {
        return other != null &&
               (other.GetComponentInParent<NeedleThreadTrap>() != null ||
                other.GetComponentInParent<RopeBridge>() != null);
    }

    private void SetSmokeForm(bool enabled)
    {
        IsSmokeForm = enabled;
        if (Fsm.Sr == null) return;
        Fsm.Sr.color = enabled && Fsm.Anim == null ? smokeTint : _normalColor;
    }

    private void PlaySmokeAnimation(int stateHash)
    {
        if (Fsm.Anim == null) return;
        Fsm.Anim.speed = smokeDuration > 0f ? TeleportAnimationLength / smokeDuration : 1f;
        Fsm.Anim.Play(stateHash, 0, 0f);
    }

    private void PlayIdleAnimation()
    {
        if (Fsm.Sr != null && idleSprite != null) Fsm.Sr.sprite = idleSprite;
        if (Fsm.Anim == null) return;
        Fsm.Anim.speed = 1f;
        Fsm.Anim.Play(IdleAnimationState, 0, 0f);
    }

    private static Vector2 Rotate(Vector2 direction, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);
        return new Vector2(direction.x * cos - direction.y * sin, direction.x * sin + direction.y * cos);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Vector2 center = Application.isPlaying
            ? _fixedTeleportAreaCenter
            : (Vector2)transform.position + teleportAreaOffset;
        Gizmos.DrawWireCube(center, teleportAreaSize);

        Gizmos.color = patternCShockwaveColor;
        Gizmos.DrawWireSphere(transform.position, patternCShockwaveRadius);
    }

    private void OnDestroy()
    {
        DestroyClonesImmediately();
    }

    private void OnValidate()
    {
        idleDuration = Mathf.Max(0f, idleDuration);
        smokeDuration = Mathf.Max(0f, smokeDuration);
        recoveryDuration = Mathf.Max(0f, recoveryDuration);
        teleportCountMin = Mathf.Clamp(teleportCountMin, 1, MaxPatternATeleports);
        teleportCountMax = Mathf.Clamp(teleportCountMax, teleportCountMin, MaxPatternATeleports);
        teleportAreaSize.x = Mathf.Max(1f, teleportAreaSize.x);
        teleportAreaSize.y = Mathf.Max(1f, teleportAreaSize.y);
        minimumTeleportDistance = Mathf.Max(0f, minimumTeleportDistance);
        teleportClearanceRadius = Mathf.Max(0.1f, teleportClearanceRadius);
        teleportSearchAttempts = Mathf.Max(1, teleportSearchAttempts);
        rangedWindupDuration = Mathf.Max(0f, rangedWindupDuration);
        projectileSpeed = Mathf.Max(0.1f, projectileSpeed);
        diagonalAngle = Mathf.Clamp(diagonalAngle, 5f, 75f);
        scratchHitRange = Mathf.Max(0.1f, scratchHitRange);
        meleeTriggerRange = Mathf.Max(meleeTriggerRange, scratchHitRange);
        scratchWindupDuration = Mathf.Max(0f, scratchWindupDuration);
        scratchDashDuration = Mathf.Max(0.01f, scratchDashDuration);
        scratchDashSpeed = Mathf.Max(0.1f, scratchDashSpeed);
        hoverHorizontalAmplitude = Mathf.Max(0f, hoverHorizontalAmplitude);
        hoverVerticalAmplitude = Mathf.Max(0f, hoverVerticalAmplitude);
        hoverDirectionIntervalMin = Mathf.Max(0.1f, hoverDirectionIntervalMin);
        hoverDirectionIntervalMax = Mathf.Max(hoverDirectionIntervalMin, hoverDirectionIntervalMax);
        hoverMaxSpeed = Mathf.Max(0.1f, hoverMaxSpeed);
        hoverResponsiveness = Mathf.Max(0.1f, hoverResponsiveness);
        patternBInitialShotDelay = Mathf.Max(0f, patternBInitialShotDelay);
        patternBMoveDurationAfterShot = Mathf.Max(0f, patternBMoveDurationAfterShot);
        patternBMoveSpeed = Mathf.Max(0f, patternBMoveSpeed);
        patternBDirectionIntervalMin = Mathf.Max(0.1f, patternBDirectionIntervalMin);
        patternBDirectionIntervalMax = Mathf.Max(patternBDirectionIntervalMin, patternBDirectionIntervalMax);
        patternBTurnSmoothTime = Mathf.Max(0.01f, patternBTurnSmoothTime);
        patternBBoundaryTurnDistance = Mathf.Max(0f, patternBBoundaryTurnDistance);
        patternBHomingSpeed = Mathf.Max(0.1f, patternBHomingSpeed);
        patternBHomingTurnSpeed = Mathf.Max(0f, patternBHomingTurnSpeed);
        patternBMainProjectileHealth = Mathf.Max(0.1f, patternBMainProjectileHealth);
        patternBCloneProjectileHealth = Mathf.Max(0.1f, patternBCloneProjectileHealth);
        patternBCloneHealth = Mathf.Max(0.1f, patternBCloneHealth);
        patternBCloneDebuffDuration = Mathf.Max(0.1f, patternBCloneDebuffDuration);
        patternBCloneDebuffValue = Mathf.Max(0f, patternBCloneDebuffValue);
        patternBProjectileScaleMultiplier = Mathf.Max(0.1f, patternBProjectileScaleMultiplier);
        patternCRepeatMin = Mathf.Max(1, patternCRepeatMin);
        patternCRepeatMax = Mathf.Max(patternCRepeatMin, patternCRepeatMax);
        patternCChargeSpeed = Mathf.Max(0.1f, patternCChargeSpeed);
        patternCChargeMaxDuration = Mathf.Max(0.1f, patternCChargeMaxDuration);
        patternCImpactCastRadius = Mathf.Max(0.1f, patternCImpactCastRadius);
        patternCDirectHitRadius = Mathf.Max(0.1f, patternCDirectHitRadius);
        patternCDirectHitDamage = Mathf.Max(0f, patternCDirectHitDamage);
        patternCShockwaveRadius = Mathf.Max(0.1f, patternCShockwaveRadius);
        patternCShockwaveDamage = Mathf.Max(0f, patternCShockwaveDamage);
        patternCShockwaveSlowAmount = Mathf.Clamp01(patternCShockwaveSlowAmount);
        patternCShockwaveSlowDuration = Mathf.Max(0.1f, patternCShockwaveSlowDuration);
        patternCImpactPauseDuration = Mathf.Max(0f, patternCImpactPauseDuration);
        patternCThreadStunDuration = Mathf.Max(0.1f, patternCThreadStunDuration);
        patternCThreadDetectionRadius = Mathf.Max(0.01f, patternCThreadDetectionRadius);
        patternCCountersForGroggy = Mathf.Max(1, patternCCountersForGroggy);
        patternCGroggyDuration = Mathf.Max(0.1f, patternCGroggyDuration);
        patternCGroggyNeedleDamageMultiplier = Mathf.Max(1f, patternCGroggyNeedleDamageMultiplier);
        patternCCounterReboundSpeed = Mathf.Max(0f, patternCCounterReboundSpeed);
        patternCCounterReboundDuration = Mathf.Max(0.01f, patternCCounterReboundDuration);
        patternCCounterReboundLift = Mathf.Max(0f, patternCCounterReboundLift);
        patternCShockwaveVisualDuration = Mathf.Max(0.05f, patternCShockwaveVisualDuration);
        afterimageInterval = Mathf.Max(0.05f, afterimageInterval);
        afterimageLifetime = Mathf.Max(0.05f, afterimageLifetime);
        afterimageMinimumDistance = Mathf.Max(0.01f, afterimageMinimumDistance);
    }
}

[RequireComponent(typeof(LineRenderer))]
public sealed class CheshireShockwaveVisual : MonoBehaviour
{
    private const int SegmentCount = 64;

    private LineRenderer _line;
    private Material _material;
    private float _radius;
    private float _duration;
    private float _elapsed;
    private Color _color;

    public static void Create(Vector2 position, float radius, float duration, Color color)
    {
        GameObject visual = new GameObject("CheshireShockwave");
        visual.transform.position = position;
        CheshireShockwaveVisual shockwave = visual.AddComponent<CheshireShockwaveVisual>();
        shockwave.Initialize(radius, duration, color);
    }

    private void Awake()
    {
        _line = GetComponent<LineRenderer>();
        _line.useWorldSpace = false;
        _line.loop = true;
        _line.positionCount = SegmentCount;
        _line.numCornerVertices = 2;
        _line.numCapVertices = 2;
        _line.sortingOrder = 10;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            _material = new Material(shader);
            _line.sharedMaterial = _material;
        }
    }

    private void Initialize(float radius, float duration, Color color)
    {
        _radius = Mathf.Max(0.1f, radius);
        _duration = Mathf.Max(0.05f, duration);
        _color = color;
        DrawCircle(0f);
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(_elapsed / _duration);
        float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
        DrawCircle(_radius * easedProgress);

        Color fadedColor = _color;
        fadedColor.a *= 1f - progress;
        _line.startColor = fadedColor;
        _line.endColor = fadedColor;
        _line.widthMultiplier = Mathf.Lerp(0.28f, 0.05f, progress);

        if (progress >= 1f) Destroy(gameObject);
    }

    private void DrawCircle(float radius)
    {
        for (int i = 0; i < SegmentCount; i++)
        {
            float angle = i * Mathf.PI * 2f / SegmentCount;
            _line.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
        }
    }

    private void OnDestroy()
    {
        if (_material != null) Destroy(_material);
    }
}
