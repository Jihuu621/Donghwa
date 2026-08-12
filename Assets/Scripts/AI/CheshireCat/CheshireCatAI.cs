using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class CheshireCatAI : EnemyAIBase
{
    private const float TeleportAnimationLength = 0.3f;
    private const int MaxPatternATeleports = 60;
    private const int PatternBCloneCount = 2;
    private const int PhysicsQueryBufferSize = 64;
    private static readonly int IdleAnimationState = Animator.StringToHash("Base Layer.Cat_Idle");
    private static readonly int TeleportAnimationState = Animator.StringToHash("Base Layer.Cat_Attack1");
    private static readonly int TeleportAppearAnimationState = Animator.StringToHash("Base Layer.Cat_TeleportAppear");
    private static readonly int ScratchDashAnimationState = Animator.StringToHash("Base Layer.Cat_Yakzin");
    private static readonly int PatternCChargeAnimationState = Animator.StringToHash("Base Layer.Cat_Gangzin");
    private const float ScratchDashAnimationLength = 0.4166667f;

    public enum State
    {
        None, Idle, SmokeEnter, Teleport, SmokeAppear, RangedAttack, ScratchWindup, ScratchDash,
        PatternBSmokeEnter, PatternBSetup, PatternBSmokeAppear, PatternBActive, PatternBExit,
        PatternCSmokeEnter, PatternCTeleport, PatternCSmokeAppear, PatternCCharge, PatternCImpactPause,
        PatternCScratchWindup, PatternCScratchDash,
        PatternDSmokeEnter, PatternDActive, PatternDSmokeAppear,
        Recovery, Stunned, Groggy
    }

    public enum FallingObjectKind
    {
        Hazard,
        Target,
        Fake
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
    [SerializeField, Min(0f)] private float scratchWindupDuration = 1f;
    [SerializeField, Min(0.01f)] private float scratchDashDuration = 0.25f;
    [SerializeField, Min(0.1f)] private float scratchDashDistance = 3.25f;

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
    [SerializeField, Min(0f)] private float patternBInitialShotDelay = 1.25f;
    [SerializeField, Min(0f)] private float patternBMoveDurationAfterShot = 16f;
    [SerializeField, Min(0f)] private float patternBPlayerSpawnMinimumDistance = 8f;
    [SerializeField, Min(0.1f)] private float patternBActorMinimumSeparation = 6f;
    [SerializeField, Min(0f)] private float patternBMoveSpeed = 3.5f;
    [SerializeField, Min(0.1f)] private float patternBDirectionIntervalMin = 1.25f;
    [SerializeField, Min(0.1f)] private float patternBDirectionIntervalMax = 2.5f;
    [SerializeField, Min(0.01f)] private float patternBTurnSmoothTime = 0.35f;
    [SerializeField, Min(0f)] private float patternBBoundaryTurnDistance = 1.25f;
    [SerializeField, Min(0.1f)] private float patternBHomingSpeed = 4.5f;
    [SerializeField, Min(0f)] private float patternBHomingTurnSpeed = 75f;
    [SerializeField, Min(1)] private int patternBMainProjectileSuccessesToDestroy = 3;
    [SerializeField, Min(1)] private int patternBCloneProjectileSuccessesToDestroy = 2;
    [SerializeField, Min(0f)] private float patternBProjectileDeflectSpeedMultiplier = 1.4f;
    [SerializeField, Min(0f)] private float patternBProjectileDeflectHomingDelay = 0.22f;
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
    [SerializeField, Min(0f)] private float patternCPlayerTeleportMinimumDistance = 8f;
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

    [Header("Pattern D - Scissor Rain")]
    [SerializeField, Min(1f)] private float patternDDuration = 14f;
    [SerializeField, Min(0.05f)] private float patternDHazardSpawnInterval = 0.38f;
    [SerializeField, Min(0.05f)] private float patternDSpecialSpawnInterval = 0.7f;
    [SerializeField, Min(1)] private int patternDTargetAppearanceMin = 5;
    [SerializeField, Min(1)] private int patternDTargetAppearanceMax = 7;
    [SerializeField, Min(1)] private int patternDFakeAppearanceMin = 5;
    [SerializeField, Min(1)] private int patternDFakeAppearanceMax = 7;
    [SerializeField, Min(1)] private int patternDRequiredTargetCuts = 3;
    [SerializeField, Min(1)] private int patternDFakeCutsToFail = 2;
    [SerializeField, Min(0f)] private float patternDFallingObjectDamage = 10f;
    [SerializeField, Min(0f)] private float patternDFakeCutDamage = 20f;
    [SerializeField, Min(0f)] private float patternDSpawnHeightOffset = 4f;
    [SerializeField, Min(0f)] private float patternDDespawnPadding = 2f;
    [SerializeField, Min(1f)] private float patternDSpawnWidthMultiplier = 1.35f;
    [SerializeField, Min(0.1f)] private float patternDFallSpeedMin = 4f;
    [SerializeField, Min(0.1f)] private float patternDFallSpeedMax = 7f;
    [SerializeField, Min(0f)] private float patternDHorizontalDrift = 2.2f;
    [SerializeField, Min(0f)] private float patternDAngularSpeed = 120f;
    [SerializeField, Min(0.1f)] private float patternDObjectScale = 1f;
    [SerializeField, Min(0)] private int patternDCutParticleBurstCount = 10;
    [SerializeField, Range(0f, 1f)] private float patternDFakePinkChance = 0.5f;
    [SerializeField] private Color patternDHazardParticleColor = new Color(0.68f, 0.72f, 0.78f, 0.72f);
    [FormerlySerializedAs("patternDTargetAuraColor")]
    [SerializeField] private Color patternDTargetParticleColor = new Color(1f, 0.2f, 0.62f, 0.95f);
    [FormerlySerializedAs("patternDFakeAuraColor")]
    [SerializeField] private Color patternDFakeParticleColor = new Color(0.58f, 0.18f, 0.9f, 0.95f);
    [FormerlySerializedAs("patternDFakePinkColor")]
    [SerializeField] private Color patternDFakePinkParticleColor = new Color(0.9f, 0.16f, 0.72f, 0.95f);

    [Header("Afterimage")]
    [SerializeField, Min(0.05f)] private float afterimageInterval = 0.14f;
    [SerializeField, Min(0.05f)] private float afterimageLifetime = 0.32f;
    [SerializeField, Min(0.01f)] private float afterimageMinimumDistance = 0.25f;
    [SerializeField] private Color afterimageColor = new Color(0.75f, 0.9f, 1f, 0.16f);

    [Header("Performance")]
    [SerializeField, Min(0)] private int projectilePoolSize = 24;
    [SerializeField, Min(0)] private int patternBProjectilePoolSize = 3;
    [SerializeField, Min(1)] private int patternDFallingObjectPoolSize = 32;

    [Header("Debug")]
    [SerializeField] private bool debugStartWithPatternD;

    public State CurrentState { get; private set; }
    public bool IsSmokeForm { get; private set; }
    public bool IsGroggy => CurrentState == State.Groggy;
    public float GroggyNeedleDamageMultiplier => patternCGroggyNeedleDamageMultiplier;

    private float _stateTimer;
    private float _stunDuration;
    private int _teleportCount;
    private int _teleportsCompleted;
    private Vector2 _scratchDashDirection;
    private float _scratchDashTravelSpeed;
    private Color _normalColor;
    private bool _hasAttacked;
    private bool _hasWarnedNoTeleportPosition;
    private Vector2 _fixedTeleportAreaCenter;
    private Collider2D _bodyCollider;
    private ParticleSystem _bodyParticleSystem;
    private readonly List<CheshireCatClone> _clones = new List<CheshireCatClone>(PatternBCloneCount);
    private readonly List<CheshireCatClone> _clonePool = new List<CheshireCatClone>(PatternBCloneCount);
    private readonly List<Vector2> _patternBOccupiedPositions = new List<Vector2>(PatternBCloneCount + 1);
    private readonly List<CheshireProjectile> _projectilePool = new List<CheshireProjectile>(24);
    private readonly List<CheshireProjectile> _patternBProjectilePool = new List<CheshireProjectile>(3);
    private readonly List<CheshireFallingObject> _patternDObjectPool = new List<CheshireFallingObject>(32);
    private readonly List<CheshireFallingObject> _activePatternDObjects = new List<CheshireFallingObject>(32);
    private readonly RaycastHit2D[] _threadHitBuffer = new RaycastHit2D[PhysicsQueryBufferSize];
    private readonly Collider2D[] _overlapBuffer = new Collider2D[PhysicsQueryBufferSize];
    private ContactFilter2D _unfilteredContactFilter;
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
    private CheshireShockwaveVisual _shockwaveVisual;
    private EffectManager _playerStatusEffects;
    private float _patternDElapsed;
    private float _patternDHazardSpawnTimer;
    private float _patternDSpecialSpawnTimer;
    private int _patternDTargetAppearanceCount;
    private int _patternDFakeAppearanceCount;
    private int _patternDTargetsSpawned;
    private int _patternDFakesSpawned;
    private int _patternDTargetCuts;
    private int _patternDFakeCuts;

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
        _bodyParticleSystem = GetComponent<ParticleSystem>();
        _unfilteredContactFilter = ContactFilter2D.noFilter;
        if (Fsm.Player != null)
        {
            _playerStatusEffects = Fsm.Player.GetComponent<EffectManager>();
            if (_playerStatusEffects == null)
            {
                _playerStatusEffects = Fsm.Player.gameObject.AddComponent<EffectManager>();
            }
        }
        SetupAfterimage();
        PrewarmPatternBClones();
        PrewarmShockwaveVisual();
        PrewarmPatternDObjects();
        PrewarmProjectilePools();
        PlayIdleAnimation();
        if (debugStartWithPatternD) BeginPatternD();
        else ChangeState(State.Idle);
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
            case State.PatternDSmokeEnter: UpdatePatternDSmokeEnter(); break;
            case State.PatternDActive: UpdatePatternDActive(); break;
            case State.PatternDSmokeAppear: UpdatePatternDSmokeAppear(); break;
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
            case State.PatternDSmokeEnter:
                SetSmokeForm(true);
                Fsm.StopAllMovement();
                PlaySmokeAnimation(TeleportAnimationState);
                break;
            case State.PatternCCharge:
                SetSmokeForm(false);
                PlayPatternCChargeAnimation();
                BeginPatternCCharge();
                break;
            case State.PatternCImpactPause:
                SetSmokeForm(false);
                Fsm.StopAllMovement();
                break;
            case State.ScratchDash:
            case State.PatternCScratchDash:
                SetSmokeForm(false);
                PlayScratchDashAnimation();
                break;
            case State.PatternCScratchWindup:
                SetSmokeForm(false);
                Fsm.StopAllMovement();
                break;
            case State.PatternDActive:
                SetSmokeForm(true);
                SetPatternDBodyHidden(true);
                Fsm.StopAllMovement();
                BeginPatternDActive();
                break;
            case State.PatternDSmokeAppear:
                ReleaseAllPatternDObjects();
                TeleportToRandomMapPosition();
                SetSmokeForm(true);
                Fsm.StopAllMovement();
                PlaySmokeAnimation(TeleportAppearAnimationState);
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
        PrepareScratchDash();
        ChangeState(State.ScratchDash);
    }

    private void UpdateScratchDash()
    {
        MoveScratchDash();
        CheckScratchDashContact();
        _stateTimer += Time.deltaTime;
        if (_stateTimer < scratchDashDuration) return;
        Fsm.StopAllMovement();
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
            if (_patternCRepeatsCompleted >= _patternCRepeatCount)
            {
                BeginPatternD();
            }
            else
            {
                ChangeState(State.PatternCSmokeEnter);
            }
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

        _patternBOccupiedPositions.Clear();
        for (int i = 0; i <= PatternBCloneCount; i++)
        {
            if (!TryFindPatternBPosition(_patternBOccupiedPositions, out Vector2 position))
            {
                ChangeState(State.PatternBSmokeEnter);
                return;
            }

            _patternBOccupiedPositions.Add(position);
        }

        ReleaseAttachedNeedles();
        SetPosition(_patternBOccupiedPositions[0]);
        for (int i = 0; i < PatternBCloneCount; i++)
        {
            CreateClone(_patternBOccupiedPositions[i + 1], i);
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
        TeleportToRandomMapPosition(patternCPlayerTeleportMinimumDistance);
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

        int hitCount = Physics2D.CircleCast(
            start,
            patternCThreadDetectionRadius,
            delta / distance,
            _unfilteredContactFilter,
            _threadHitBuffer,
            distance);
        for (int i = 0; i < hitCount; i++)
        {
            if (IsPatternCCounterThread(_threadHitBuffer[i].collider))
            {
                return true;
            }
        }

        return false;
    }

    private void TryDealPatternCDirectHit()
    {
        if (_patternCDirectHitDealt || Fsm.Player == null) return;

        int overlapCount = Physics2D.OverlapCircle(
            Fsm.Rb.position,
            patternCDirectHitRadius,
            _unfilteredContactFilter,
            _overlapBuffer);
        for (int i = 0; i < overlapCount; i++)
        {
            Collider2D overlap = _overlapBuffer[i];
            if (overlap == null || overlap.transform.root != Fsm.Player.root) continue;
            DealPatternCDirectHit(overlap);
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
        if (_shockwaveVisual == null) PrewarmShockwaveVisual();
        _shockwaveVisual.Play(
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

        int overlapCount = Physics2D.OverlapCircle(
            impactPosition,
            patternCShockwaveRadius,
            _unfilteredContactFilter,
            _overlapBuffer);
        for (int i = 0; i < overlapCount; i++)
        {
            Collider2D overlap = _overlapBuffer[i];
            if (overlap == null || overlap.transform.root != Fsm.Player.root) continue;

            IDamageable player = overlap.GetComponentInParent<IDamageable>();
            if (player != null) player.TakeDamage(patternCShockwaveDamage, gameObject);

            if (_playerStatusEffects == null)
            {
                _playerStatusEffects = Fsm.Player.GetComponent<EffectManager>();
                if (_playerStatusEffects == null)
                {
                    _playerStatusEffects = Fsm.Player.gameObject.AddComponent<EffectManager>();
                }
            }
            _playerStatusEffects.ApplyStatus(
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

        PrepareScratchDash();
        ChangeState(State.PatternCScratchDash);
    }

    private void UpdatePatternCScratchDash()
    {
        MoveScratchDash();
        CheckScratchDashContact();
        _stateTimer += Time.deltaTime;
        if (_stateTimer < scratchDashDuration) return;

        Fsm.StopAllMovement();
        CompletePatternCIteration();
    }

    private void CompletePatternCIteration()
    {
        _patternCRepeatsCompleted++;
        if (_patternCRepeatsCompleted >= _patternCRepeatCount)
        {
            BeginPatternD();
            return;
        }

        ChangeState(State.PatternCSmokeEnter);
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
        if (_stateTimer >= patternCGroggyDuration) BeginPatternD();
    }

    private void BeginPatternD()
    {
        ChangeState(State.PatternDSmokeEnter);
    }

    private void UpdatePatternDSmokeEnter()
    {
        _stateTimer += Time.deltaTime;
        if (_stateTimer >= smokeDuration) ChangeState(State.PatternDActive);
    }

    private void BeginPatternDActive()
    {
        _patternDElapsed = 0f;
        _patternDHazardSpawnTimer = 0.15f;
        _patternDSpecialSpawnTimer = 0.45f;
        _patternDTargetAppearanceCount = Random.Range(
            patternDTargetAppearanceMin,
            patternDTargetAppearanceMax + 1);
        _patternDFakeAppearanceCount = Random.Range(
            patternDFakeAppearanceMin,
            patternDFakeAppearanceMax + 1);
        _patternDTargetsSpawned = 0;
        _patternDFakesSpawned = 0;
        _patternDTargetCuts = 0;
        _patternDFakeCuts = 0;
    }

    private void UpdatePatternDActive()
    {
        _patternDElapsed += Time.deltaTime;
        _patternDHazardSpawnTimer -= Time.deltaTime;
        _patternDSpecialSpawnTimer -= Time.deltaTime;

        if (_patternDHazardSpawnTimer <= 0f)
        {
            SpawnPatternDObject(FallingObjectKind.Hazard);
            _patternDHazardSpawnTimer = patternDHazardSpawnInterval;
        }

        if (_patternDSpecialSpawnTimer <= 0f && HasUnspawnedPatternDSpecials())
        {
            SpawnNextPatternDSpecial();
            _patternDSpecialSpawnTimer = patternDSpecialSpawnInterval;
        }

        bool allSpecialsSpawned = !HasUnspawnedPatternDSpecials();
        if (_patternDElapsed >= patternDDuration ||
            (allSpecialsSpawned && !HasActivePatternDSpecial()))
        {
            FinishPatternD(false);
        }
    }

    private void UpdatePatternDSmokeAppear()
    {
        _stateTimer += Time.deltaTime;
        if (_stateTimer < smokeDuration) return;

        PlayIdleAnimation();
        SetPatternDBodyHidden(false);
        SetSmokeForm(false);
        ChangeState(State.Recovery);
    }

    private bool HasUnspawnedPatternDSpecials()
    {
        return _patternDTargetsSpawned < _patternDTargetAppearanceCount ||
               _patternDFakesSpawned < _patternDFakeAppearanceCount;
    }

    private void SpawnNextPatternDSpecial()
    {
        bool canSpawnTarget = _patternDTargetsSpawned < _patternDTargetAppearanceCount;
        bool canSpawnFake = _patternDFakesSpawned < _patternDFakeAppearanceCount;
        bool spawnTarget = canSpawnTarget && (!canSpawnFake || Random.value < 0.5f);

        if (spawnTarget)
        {
            _patternDTargetsSpawned++;
            SpawnPatternDObject(FallingObjectKind.Target);
            return;
        }

        _patternDFakesSpawned++;
        SpawnPatternDObject(FallingObjectKind.Fake);
    }

    private bool HasActivePatternDSpecial()
    {
        for (int i = 0; i < _activePatternDObjects.Count; i++)
        {
            CheshireFallingObject fallingObject = _activePatternDObjects[i];
            if (fallingObject != null && fallingObject.Kind != FallingObjectKind.Hazard) return true;
        }

        return false;
    }

    private void SpawnPatternDObject(FallingObjectKind kind)
    {
        CheshireFallingObject fallingObject = GetPatternDObject();
        Vector2 halfSize = teleportAreaSize * 0.5f;
        float spawnHalfWidth = halfSize.x * patternDSpawnWidthMultiplier;
        Vector2 spawnPosition = new Vector2(
            Random.Range(_fixedTeleportAreaCenter.x - spawnHalfWidth, _fixedTeleportAreaCenter.x + spawnHalfWidth),
            _fixedTeleportAreaCenter.y + halfSize.y + patternDSpawnHeightOffset);
        Vector2 velocity = new Vector2(
            Random.Range(-patternDHorizontalDrift, patternDHorizontalDrift),
            -Random.Range(patternDFallSpeedMin, patternDFallSpeedMax));
        float angularVelocity = Random.Range(-patternDAngularSpeed, patternDAngularSpeed);
        float despawnY = _fixedTeleportAreaCenter.y - halfSize.y - patternDDespawnPadding;
        Color particleColor = patternDHazardParticleColor;
        if (kind == FallingObjectKind.Target)
        {
            particleColor = patternDTargetParticleColor;
        }
        else if (kind == FallingObjectKind.Fake)
        {
            particleColor = Random.value < patternDFakePinkChance
                ? patternDFakePinkParticleColor
                : patternDFakeParticleColor;
        }

        fallingObject.gameObject.SetActive(true);
        fallingObject.Configure(
            this,
            kind,
            spawnPosition,
            velocity,
            angularVelocity,
            despawnY,
            patternDObjectScale,
            particleColor);
        _activePatternDObjects.Add(fallingObject);
    }

    private CheshireFallingObject GetPatternDObject()
    {
        for (int i = 0; i < _patternDObjectPool.Count; i++)
        {
            if (_patternDObjectPool[i] != null && !_patternDObjectPool[i].gameObject.activeSelf)
            {
                return _patternDObjectPool[i];
            }
        }

        CheshireFallingObject created = CreatePatternDObject(_patternDObjectPool.Count);
        _patternDObjectPool.Add(created);
        return created;
    }

    public bool HandlePatternDScissorCut(CheshireFallingObject fallingObject)
    {
        if (CurrentState != State.PatternDActive || fallingObject == null) return false;

        FallingObjectKind kind = fallingObject.Kind;
        if (!_activePatternDObjects.Remove(fallingObject)) return false;
        fallingObject.BeginCutDisappear(patternDCutParticleBurstCount);

        if (kind == FallingObjectKind.Target)
        {
            _patternDTargetCuts++;
            if (_patternDTargetCuts >= patternDRequiredTargetCuts) FinishPatternD(false);
        }
        else if (kind == FallingObjectKind.Fake)
        {
            _patternDFakeCuts++;
            if (_patternDFakeCuts >= patternDFakeCutsToFail) FinishPatternD(true);
        }

        return true;
    }

    public void HandlePatternDPlayerHit(CheshireFallingObject fallingObject, Collider2D playerCollider)
    {
        if (CurrentState != State.PatternDActive || fallingObject == null) return;

        IDamageable player = playerCollider != null
            ? playerCollider.GetComponentInParent<IDamageable>()
            : null;
        if (player != null) player.TakeDamage(patternDFallingObjectDamage, gameObject);
        ReleasePatternDObject(fallingObject);
    }

    public void ReleasePatternDObject(CheshireFallingObject fallingObject)
    {
        if (fallingObject == null) return;
        _activePatternDObjects.Remove(fallingObject);
        fallingObject.Deactivate();
    }

    private void FinishPatternD(bool fakeCutFailure)
    {
        if (CurrentState != State.PatternDActive) return;

        if (fakeCutFailure && Fsm.Player != null)
        {
            IDamageable player = Fsm.Player.GetComponentInParent<IDamageable>();
            if (player != null) player.TakeDamage(patternDFakeCutDamage, gameObject);
        }

        ReleaseAllPatternDObjects();
        ChangeState(State.PatternDSmokeAppear);
    }

    private void ReleaseAllPatternDObjects()
    {
        while (_activePatternDObjects.Count > 0)
        {
            int lastIndex = _activePatternDObjects.Count - 1;
            CheshireFallingObject fallingObject = _activePatternDObjects[lastIndex];
            _activePatternDObjects.RemoveAt(lastIndex);
            if (fallingObject != null) fallingObject.Deactivate();
        }
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
        int successesToDestroy = fromClone
            ? patternBCloneProjectileSuccessesToDestroy
            : patternBMainProjectileSuccessesToDestroy;
        Color color = fromClone ? patternBCloneProjectileColor : patternBMainProjectileColor;

        float scaleMultiplier = patternBProjectilePrefab == null ? patternBProjectileScaleMultiplier : 1f;
        CheshireProjectile projectile = GetProjectile(
            selectedPrefab,
            _patternBProjectilePool,
            origin,
            scaleMultiplier);
        projectile.LaunchHoming(
            Fsm.Player,
            direction,
            patternBHomingSpeed,
            patternBHomingTurnSpeed,
            damage,
            successesToDestroy,
            color,
            fromClone,
            source,
            patternBProjectileDeflectSpeedMultiplier,
            patternBProjectileDeflectHomingDelay);
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

        for (int i = 0; i < _clones.Count; i++)
        {
            if (_clones[i] != null) _clones[i].FireProjectile();
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
        CheshireProjectile projectile = GetProjectile(projectilePrefab, _projectilePool, origin, 1f);
        projectile.Launch(direction, projectileSpeed, damage, gameObject);
    }

    private void PrewarmProjectilePools()
    {
        PrewarmProjectilePool(projectilePrefab, _projectilePool, projectilePoolSize);
        CheshireProjectile selectedPatternBPrefab = patternBProjectilePrefab != null
            ? patternBProjectilePrefab
            : projectilePrefab;
        PrewarmProjectilePool(
            selectedPatternBPrefab,
            _patternBProjectilePool,
            patternBProjectilePoolSize);
    }

    private void PrewarmProjectilePool(
        CheshireProjectile prefab,
        List<CheshireProjectile> pool,
        int targetSize)
    {
        if (prefab == null) return;
        while (pool.Count < targetSize) CreatePooledProjectile(prefab, pool);
    }

    private CheshireProjectile GetProjectile(
        CheshireProjectile prefab,
        List<CheshireProjectile> pool,
        Vector2 position,
        float scaleMultiplier)
    {
        CheshireProjectile projectile = null;
        for (int i = 0; i < pool.Count; i++)
        {
            if (pool[i] == null || pool[i].gameObject.activeSelf) continue;
            projectile = pool[i];
            break;
        }

        if (projectile == null) projectile = CreatePooledProjectile(prefab, pool);
        projectile.transform.SetPositionAndRotation(position, Quaternion.identity);
        projectile.transform.localScale = prefab.transform.localScale * scaleMultiplier;
        projectile.gameObject.SetActive(true);
        return projectile;
    }

    private CheshireProjectile CreatePooledProjectile(
        CheshireProjectile prefab,
        List<CheshireProjectile> pool)
    {
        CheshireProjectile projectile = Instantiate(prefab);
        projectile.name = $"{prefab.name}_Pooled";
        projectile.SetPoolOwner(this);
        projectile.gameObject.SetActive(false);
        pool.Add(projectile);
        return projectile;
    }

    public void ReleaseProjectile(CheshireProjectile projectile)
    {
        if (projectile != null) projectile.gameObject.SetActive(false);
    }

    private bool TryFindPatternBPosition(List<Vector2> occupiedPositions, out Vector2 position)
    {
        Vector2 halfSize = teleportAreaSize * 0.5f;
        Vector2 center = _fixedTeleportAreaCenter;

        for (int i = 0; i < teleportSearchAttempts; i++)
        {
            Vector2 candidate = new Vector2(
                Random.Range(center.x - halfSize.x, center.x + halfSize.x),
                Random.Range(center.y - halfSize.y, center.y + halfSize.y));

            if (Physics2D.OverlapCircle(candidate, teleportClearanceRadius, teleportObstacleMask) != null) continue;
            if (Fsm.Player != null &&
                Vector2.Distance(candidate, Fsm.Player.position) < patternBPlayerSpawnMinimumDistance) continue;

            bool overlapsActor = false;
            for (int j = 0; j < occupiedPositions.Count; j++)
            {
                if (Vector2.Distance(candidate, occupiedPositions[j]) >= patternBActorMinimumSeparation) continue;
                overlapsActor = true;
                break;
            }

            if (!overlapsActor)
            {
                position = candidate;
                return true;
            }
        }

        position = default;
        return false;
    }

    private void CreateClone(Vector2 position, int index)
    {
        if (index >= _clonePool.Count) _clonePool.Add(CreateCloneObject(index));

        CheshireCatClone clone = _clonePool[index];
        GameObject cloneObject = clone.gameObject;
        cloneObject.transform.SetPositionAndRotation(position, Quaternion.identity);
        cloneObject.transform.localScale = transform.lossyScale;

        SpriteRenderer cloneRenderer = cloneObject.GetComponent<SpriteRenderer>();
        if (Fsm.Sr != null)
        {
            cloneRenderer.sprite = idleSprite != null ? idleSprite : Fsm.Sr.sprite;
            cloneRenderer.color = _normalColor;
            cloneRenderer.sharedMaterial = Fsm.Sr.sharedMaterial;
            cloneRenderer.sortingLayerID = Fsm.Sr.sortingLayerID;
            cloneRenderer.sortingOrder = Fsm.Sr.sortingOrder;
            cloneRenderer.flipX = Fsm.Sr.flipX;
        }

        cloneObject.SetActive(true);
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

    private void PrewarmPatternBClones()
    {
        for (int i = _clonePool.Count; i < PatternBCloneCount; i++)
        {
            _clonePool.Add(CreateCloneObject(i));
        }
    }

    private CheshireCatClone CreateCloneObject(int index)
    {
        GameObject cloneObject = new GameObject($"CheshireCatClone_{index + 1}");
        cloneObject.tag = "Enemy";
        cloneObject.layer = gameObject.layer;
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
        cloneObject.SetActive(false);
        return clone;
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
        for (int i = 0; i < _clones.Count; i++)
        {
            if (_clones[i] != null) _clones[i].BeginDisappear(smokeDuration);
        }
    }

    private void DestroyClonesImmediately()
    {
        while (_clones.Count > 0)
        {
            int lastIndex = _clones.Count - 1;
            CheshireCatClone clone = _clones[lastIndex];
            _clones.RemoveAt(lastIndex);
            if (clone != null) clone.DeactivateImmediately();
        }
    }

    public void NotifyCloneReleased(CheshireCatClone clone)
    {
        _clones.Remove(clone);
    }

    public void NotifyCloneDestroyed(CheshireCatClone clone)
    {
        _clones.Remove(clone);
        _clonePool.Remove(clone);
    }

    private void SetPosition(Vector2 position)
    {
        Fsm.StopAllMovement();
        if (Fsm.Rb != null) Fsm.Rb.position = position;
        else transform.position = position;
    }

    private void ReleaseAttachedNeedles()
    {
        NeedleProjectile[] attachedNeedles = GetComponentsInChildren<NeedleProjectile>(true);
        for (int i = 0; i < attachedNeedles.Length; i++)
        {
            NeedleProjectile needle = attachedNeedles[i];
            if (needle != null) needle.ReturnToPool();
        }
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

    private void PrewarmShockwaveVisual()
    {
        if (_shockwaveVisual != null) return;
        _shockwaveVisual = CheshireShockwaveVisual.CreateReusable();
    }

    private void PrewarmPatternDObjects()
    {
        while (_patternDObjectPool.Count < patternDFallingObjectPoolSize)
        {
            _patternDObjectPool.Add(CreatePatternDObject(_patternDObjectPool.Count));
        }
    }

    private CheshireFallingObject CreatePatternDObject(int index)
    {
        CheshireProjectile fallingOrbPrefab = patternBProjectilePrefab != null
            ? patternBProjectilePrefab
            : projectilePrefab;
        GameObject fallingObject = fallingOrbPrefab != null
            ? Instantiate(fallingOrbPrefab.gameObject)
            : new GameObject();
        fallingObject.name = $"CheshireFallingOrb_{index + 1}";
        fallingObject.tag = "Untagged";
        fallingObject.layer = gameObject.layer;

        CheshireProjectile projectileLogic = fallingObject.GetComponent<CheshireProjectile>();
        if (projectileLogic != null)
        {
            projectileLogic.enabled = false;
            Destroy(projectileLogic);
        }

        SpriteRenderer spriteRenderer = fallingObject.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = fallingObject.AddComponent<SpriteRenderer>();
            if (Fsm.Sr != null)
            {
                spriteRenderer.sharedMaterial = Fsm.Sr.sharedMaterial;
                spriteRenderer.sortingLayerID = Fsm.Sr.sortingLayerID;
                spriteRenderer.sortingOrder = Fsm.Sr.sortingOrder + 1;
            }
        }

        CircleCollider2D objectCollider = fallingObject.GetComponent<CircleCollider2D>();
        if (objectCollider == null) objectCollider = fallingObject.AddComponent<CircleCollider2D>();
        objectCollider.isTrigger = true;

        Rigidbody2D objectRigidbody = fallingObject.GetComponent<Rigidbody2D>();
        if (objectRigidbody == null) objectRigidbody = fallingObject.AddComponent<Rigidbody2D>();
        objectRigidbody.gravityScale = 0f;
        objectRigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
        objectRigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        objectRigidbody.constraints = RigidbodyConstraints2D.None;

        CheshireFallingObject component = fallingObject.GetComponent<CheshireFallingObject>();
        if (component == null) component = fallingObject.AddComponent<CheshireFallingObject>();
        fallingObject.SetActive(false);
        return component;
    }

    private void TeleportToRandomMapPosition(float minimumPlayerDistance = 0f)
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
            if (Fsm.Player != null && Vector2.Distance(candidate, Fsm.Player.position) < minimumPlayerDistance) continue;
            if (Physics2D.OverlapCircle(candidate, bodyRadius, teleportObstacleMask) != null) continue;

            ReleaseAttachedNeedles();
            Fsm.StopAllMovement();
            if (Fsm.Rb != null) Fsm.Rb.position = candidate;
            else transform.position = candidate;
            return;
        }

        if (_hasWarnedNoTeleportPosition) return;
        _hasWarnedNoTeleportPosition = true;
        Debug.LogWarning("[CheshireCat] No open teleport position found inside the fixed teleport area.", this);
    }

    private void PrepareScratchDash()
    {
        Vector2 origin = Fsm.Rb != null ? Fsm.Rb.position : (Vector2)transform.position;
        Vector2 direction = Fsm.Player != null
            ? (Vector2)Fsm.Player.position - origin
            : Fsm.Sr != null && Fsm.Sr.flipX ? Vector2.right : Vector2.left;
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Fsm.Sr != null && Fsm.Sr.flipX ? Vector2.right : Vector2.left;
        }

        _scratchDashDirection = direction.normalized;
        _scratchDashTravelSpeed = scratchDashDistance / Mathf.Max(scratchDashDuration, 0.01f);
    }

    private void MoveScratchDash()
    {
        if (Fsm.Rb != null)
        {
            Fsm.Rb.linearVelocity = _scratchDashDirection * _scratchDashTravelSpeed;
            return;
        }

        transform.position += (Vector3)(_scratchDashDirection * _scratchDashTravelSpeed * Time.deltaTime);
    }

    private void FacePlayer()
    {
        if (Fsm.Player != null && Fsm.Sr != null) Fsm.Sr.flipX = Fsm.Player.position.x > transform.position.x;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleScratchDashContact(other);
        HandlePatternCContact(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleScratchDashContact(collision.collider);
        HandlePatternCContact(collision.collider);
    }

    private void CheckScratchDashContact()
    {
        if (_hasAttacked || _bodyCollider == null) return;

        int overlapCount = _bodyCollider.Overlap(_unfilteredContactFilter, _overlapBuffer);
        for (int i = 0; i < overlapCount; i++)
        {
            HandleScratchDashContact(_overlapBuffer[i]);
            if (_hasAttacked) return;
        }
    }

    private void HandleScratchDashContact(Collider2D other)
    {
        bool isScratchDash = CurrentState == State.ScratchDash ||
                             CurrentState == State.PatternCScratchDash;
        if (!isScratchDash || _hasAttacked || other == null || Fsm.Player == null) return;
        if (other.transform.root != Fsm.Player.root) return;

        IDamageable player = other.GetComponentInParent<IDamageable>();
        if (player == null) return;

        _hasAttacked = true;
        float damage = Fsm.Data != null ? Fsm.Data.Damage : 10f;
        player.TakeDamage(damage, gameObject);
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

    private void SetPatternDBodyHidden(bool hidden)
    {
        if (hidden) ReleaseAttachedNeedles();
        if (_bodyCollider != null) _bodyCollider.enabled = !hidden;
        if (_bodyParticleSystem == null) return;

        if (hidden)
        {
            _bodyParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        else
        {
            _bodyParticleSystem.Play(true);
        }
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

    private void PlayScratchDashAnimation()
    {
        if (Fsm.Anim == null) return;
        Fsm.Anim.speed = ScratchDashAnimationLength / Mathf.Max(scratchDashDuration, 0.01f);
        Fsm.Anim.Play(ScratchDashAnimationState, 0, 0f);
    }

    private void PlayPatternCChargeAnimation()
    {
        if (Fsm.Anim == null) return;
        Fsm.Anim.speed = 1f;
        Fsm.Anim.Play(PatternCChargeAnimationState, 0, 0f);
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
        ReleaseAllPatternDObjects();
        while (_clonePool.Count > 0)
        {
            int lastIndex = _clonePool.Count - 1;
            CheshireCatClone clone = _clonePool[lastIndex];
            _clonePool.RemoveAt(lastIndex);
            if (clone != null) Destroy(clone.gameObject);
        }

        DestroyProjectilePool(_projectilePool);
        DestroyProjectilePool(_patternBProjectilePool);
        while (_patternDObjectPool.Count > 0)
        {
            int lastIndex = _patternDObjectPool.Count - 1;
            CheshireFallingObject fallingObject = _patternDObjectPool[lastIndex];
            _patternDObjectPool.RemoveAt(lastIndex);
            if (fallingObject != null) Destroy(fallingObject.gameObject);
        }

        if (_shockwaveVisual != null) Destroy(_shockwaveVisual.gameObject);
    }

    private static void DestroyProjectilePool(List<CheshireProjectile> pool)
    {
        while (pool.Count > 0)
        {
            int lastIndex = pool.Count - 1;
            CheshireProjectile projectile = pool[lastIndex];
            pool.RemoveAt(lastIndex);
            if (projectile != null) Destroy(projectile.gameObject);
        }
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
        meleeTriggerRange = Mathf.Max(0.1f, meleeTriggerRange);
        scratchWindupDuration = Mathf.Max(0f, scratchWindupDuration);
        scratchDashDuration = Mathf.Max(0.01f, scratchDashDuration);
        scratchDashDistance = Mathf.Max(0.1f, scratchDashDistance);
        hoverHorizontalAmplitude = Mathf.Max(0f, hoverHorizontalAmplitude);
        hoverVerticalAmplitude = Mathf.Max(0f, hoverVerticalAmplitude);
        hoverDirectionIntervalMin = Mathf.Max(0.1f, hoverDirectionIntervalMin);
        hoverDirectionIntervalMax = Mathf.Max(hoverDirectionIntervalMin, hoverDirectionIntervalMax);
        hoverMaxSpeed = Mathf.Max(0.1f, hoverMaxSpeed);
        hoverResponsiveness = Mathf.Max(0.1f, hoverResponsiveness);
        patternBInitialShotDelay = Mathf.Max(0f, patternBInitialShotDelay);
        patternBMoveDurationAfterShot = Mathf.Max(0f, patternBMoveDurationAfterShot);
        patternBPlayerSpawnMinimumDistance = Mathf.Max(0f, patternBPlayerSpawnMinimumDistance);
        patternBActorMinimumSeparation = Mathf.Max(0.1f, patternBActorMinimumSeparation);
        patternBMoveSpeed = Mathf.Max(0f, patternBMoveSpeed);
        patternBDirectionIntervalMin = Mathf.Max(0.1f, patternBDirectionIntervalMin);
        patternBDirectionIntervalMax = Mathf.Max(patternBDirectionIntervalMin, patternBDirectionIntervalMax);
        patternBTurnSmoothTime = Mathf.Max(0.01f, patternBTurnSmoothTime);
        patternBBoundaryTurnDistance = Mathf.Max(0f, patternBBoundaryTurnDistance);
        patternBHomingSpeed = Mathf.Max(0.1f, patternBHomingSpeed);
        patternBHomingTurnSpeed = Mathf.Max(0f, patternBHomingTurnSpeed);
        patternBMainProjectileSuccessesToDestroy = Mathf.Max(1, patternBMainProjectileSuccessesToDestroy);
        patternBCloneProjectileSuccessesToDestroy = Mathf.Max(1, patternBCloneProjectileSuccessesToDestroy);
        patternBProjectileDeflectSpeedMultiplier = Mathf.Max(0f, patternBProjectileDeflectSpeedMultiplier);
        patternBProjectileDeflectHomingDelay = Mathf.Max(0f, patternBProjectileDeflectHomingDelay);
        patternBCloneHealth = Mathf.Max(0.1f, patternBCloneHealth);
        patternBCloneDebuffDuration = Mathf.Max(0.1f, patternBCloneDebuffDuration);
        patternBCloneDebuffValue = Mathf.Max(0f, patternBCloneDebuffValue);
        patternBProjectileScaleMultiplier = Mathf.Max(0.1f, patternBProjectileScaleMultiplier);
        patternCPlayerTeleportMinimumDistance = Mathf.Max(0f, patternCPlayerTeleportMinimumDistance);
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
        patternDDuration = Mathf.Max(1f, patternDDuration);
        patternDHazardSpawnInterval = Mathf.Max(0.05f, patternDHazardSpawnInterval);
        patternDSpecialSpawnInterval = Mathf.Max(0.05f, patternDSpecialSpawnInterval);
        patternDTargetAppearanceMin = Mathf.Max(1, patternDTargetAppearanceMin);
        patternDTargetAppearanceMax = Mathf.Max(patternDTargetAppearanceMin, patternDTargetAppearanceMax);
        patternDFakeAppearanceMin = Mathf.Max(1, patternDFakeAppearanceMin);
        patternDFakeAppearanceMax = Mathf.Max(patternDFakeAppearanceMin, patternDFakeAppearanceMax);
        patternDRequiredTargetCuts = Mathf.Max(1, patternDRequiredTargetCuts);
        patternDFakeCutsToFail = Mathf.Max(1, patternDFakeCutsToFail);
        patternDFallingObjectDamage = Mathf.Max(0f, patternDFallingObjectDamage);
        patternDFakeCutDamage = Mathf.Max(0f, patternDFakeCutDamage);
        patternDSpawnHeightOffset = Mathf.Max(0f, patternDSpawnHeightOffset);
        patternDDespawnPadding = Mathf.Max(0f, patternDDespawnPadding);
        patternDSpawnWidthMultiplier = Mathf.Max(1f, patternDSpawnWidthMultiplier);
        patternDFallSpeedMin = Mathf.Max(0.1f, patternDFallSpeedMin);
        patternDFallSpeedMax = Mathf.Max(patternDFallSpeedMin, patternDFallSpeedMax);
        patternDHorizontalDrift = Mathf.Max(0f, patternDHorizontalDrift);
        patternDAngularSpeed = Mathf.Max(0f, patternDAngularSpeed);
        patternDObjectScale = Mathf.Max(0.1f, patternDObjectScale);
        patternDCutParticleBurstCount = Mathf.Max(0, patternDCutParticleBurstCount);
        patternDFakePinkChance = Mathf.Clamp01(patternDFakePinkChance);
        afterimageInterval = Mathf.Max(0.05f, afterimageInterval);
        afterimageLifetime = Mathf.Max(0.05f, afterimageLifetime);
        afterimageMinimumDistance = Mathf.Max(0.01f, afterimageMinimumDistance);
        projectilePoolSize = Mathf.Max(0, projectilePoolSize);
        patternBProjectilePoolSize = Mathf.Max(0, patternBProjectilePoolSize);
        patternDFallingObjectPoolSize = Mathf.Max(1, patternDFallingObjectPoolSize);
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

    public static CheshireShockwaveVisual CreateReusable()
    {
        GameObject visual = new GameObject("CheshireShockwave");
        CheshireShockwaveVisual shockwave = visual.AddComponent<CheshireShockwaveVisual>();
        visual.SetActive(false);
        return shockwave;
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

    public void Play(Vector2 position, float radius, float duration, Color color)
    {
        transform.position = position;
        _radius = Mathf.Max(0.1f, radius);
        _duration = Mathf.Max(0.05f, duration);
        _color = color;
        _elapsed = 0f;
        gameObject.SetActive(true);
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

        if (progress >= 1f) gameObject.SetActive(false);
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

public sealed class CheshireFallingObject : MonoBehaviour, IScissorCutTarget
{
    public CheshireCatAI.FallingObjectKind Kind { get; private set; }

    private CheshireCatAI _owner;
    private Rigidbody2D _rigidbody;
    private CircleCollider2D _collider;
    private SpriteRenderer _spriteRenderer;
    private ParticleSystem[] _particleSystems;
    private TrailRenderer _trail;
    private Vector3 _baseScale;
    private float _baseColliderRadius;
    private float _despawnY;
    private bool _active;
    private bool _waitingForParticles;
    private bool _hasBeenCut;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _collider = GetComponent<CircleCollider2D>();
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        _particleSystems = GetComponentsInChildren<ParticleSystem>(true);
        _trail = GetComponentInChildren<TrailRenderer>(true);
        _baseScale = transform.localScale;
        _baseColliderRadius = Mathf.Max(_collider.radius, 0.1f);
    }

    public void Configure(
        CheshireCatAI owner,
        CheshireCatAI.FallingObjectKind kind,
        Vector2 position,
        Vector2 velocity,
        float angularVelocity,
        float despawnY,
        float scale,
        Color particleColor)
    {
        _owner = owner;
        Kind = kind;
        _despawnY = despawnY;
        _active = true;
        _waitingForParticles = false;
        _hasBeenCut = false;

        transform.SetPositionAndRotation(position, Quaternion.identity);
        transform.localScale = new Vector3(_baseScale.x * scale, _baseScale.y * scale, _baseScale.z);
        if (_spriteRenderer != null) _spriteRenderer.enabled = true;

        _collider.radius = _baseColliderRadius;
        _collider.enabled = true;
        _rigidbody.linearVelocity = velocity;
        _rigidbody.angularVelocity = angularVelocity;
        RestartVisuals(particleColor);
    }

    private void Update()
    {
        if (_waitingForParticles)
        {
            for (int i = 0; i < _particleSystems.Length; i++)
            {
                if (_particleSystems[i].IsAlive(true)) return;
            }

            _owner?.ReleasePatternDObject(this);
            return;
        }

        if (_active && transform.position.y < _despawnY)
        {
            _owner?.ReleasePatternDObject(this);
        }
    }

    public bool TryScissorCut(Vector2 start, Vector2 end)
    {
        if (!_active || _hasBeenCut || _owner == null) return false;
        _hasBeenCut = true;
        bool accepted = _owner.HandlePatternDScissorCut(this);
        if (!accepted) _hasBeenCut = false;
        return accepted;
    }

    public void BeginCutDisappear(int particleBurstCount)
    {
        if (!_active) return;

        _active = false;
        _waitingForParticles = true;
        _collider.enabled = false;
        _rigidbody.linearVelocity = Vector2.zero;
        _rigidbody.angularVelocity = 0f;
        if (_spriteRenderer != null) _spriteRenderer.enabled = false;
        if (_trail != null) _trail.Clear();

        for (int i = 0; i < _particleSystems.Length; i++)
        {
            if (particleBurstCount > 0) _particleSystems[i].Emit(particleBurstCount);
            _particleSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_active || other == null) return;
        Transform root = other.transform.root;
        if (!other.CompareTag("Player") && !root.CompareTag("Player")) return;
        _owner?.HandlePatternDPlayerHit(this, other);
    }

    public void Deactivate()
    {
        if (!_active && !gameObject.activeSelf) return;
        _active = false;
        _waitingForParticles = false;
        _hasBeenCut = false;
        _collider.enabled = false;
        _rigidbody.linearVelocity = Vector2.zero;
        _rigidbody.angularVelocity = 0f;
        if (_trail != null) _trail.Clear();
        for (int i = 0; i < _particleSystems.Length; i++)
        {
            _particleSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        gameObject.SetActive(false);
    }

    private void RestartVisuals(Color particleColor)
    {
        if (_trail != null) _trail.Clear();
        for (int i = 0; i < _particleSystems.Length; i++)
        {
            ParticleSystem.MainModule main = _particleSystems[i].main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = particleColor;
            _particleSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _particleSystems[i].Play(true);
        }
    }
}
