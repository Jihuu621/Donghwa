using UnityEngine;
using UnityEngine.Serialization;

public class CheshireCatAI : EnemyAIBase
{
    private const float TeleportAnimationLength = 0.3f;
    private static readonly int IdleAnimationState = Animator.StringToHash("Base Layer.Cat_Idle");
    private static readonly int TeleportAnimationState = Animator.StringToHash("Base Layer.Cat_Attack1");
    private static readonly int TeleportAppearAnimationState = Animator.StringToHash("Base Layer.Cat_TeleportAppear");

    public enum State { None, Idle, SmokeEnter, Teleport, SmokeAppear, RangedAttack, ScratchWindup, ScratchDash, Recovery, Stunned }

    [Header("Pattern")]
    [SerializeField, Min(0f)] private float idleDuration = 0.75f;
    [SerializeField, Min(0f)] private float smokeDuration = 0.45f;
    [SerializeField, Min(0f)] private float recoveryDuration = 0.8f;
    [SerializeField, Min(1)] private int teleportCountMin = 3;
    [SerializeField, Min(1)] private int teleportCountMax = 6;

    [Header("Teleport Area")]
    [FormerlySerializedAs("teleportAreaCenter")]
    [Tooltip("Offset from the player's current position.")]
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

    [Header("Smoke Visual")]
    [SerializeField] private Color smokeTint = new Color(0.45f, 0.45f, 0.45f, 0.3f);
    [SerializeField] private Sprite idleSprite;

    public State CurrentState { get; private set; }
    public bool IsSmokeForm { get; private set; }

    private float _stateTimer;
    private float _stunDuration;
    private int _teleportCount;
    private int _teleportsCompleted;
    private Vector2 _attackTarget;
    private Color _normalColor;
    private bool _hasAttacked;
    private Vector2 _fallbackTeleportOrigin;
    private Collider2D _bodyCollider;

    private void Start()
    {
        _normalColor = Fsm.Sr != null ? Fsm.Sr.color : Color.white;
        _fallbackTeleportOrigin = Fsm.Rb != null ? Fsm.Rb.position : (Vector2)transform.position;
        _bodyCollider = GetComponent<Collider2D>();
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
            case State.Recovery: UpdateRecovery(); break;
            case State.Stunned: UpdateStunned(); break;
        }
    }

    public override bool TryStun(float duration)
    {
        if (IsSmokeForm) return false;
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

        if (CurrentState == State.ScratchDash) Fsm.StopAllMovement();
        CurrentState = next;
        _stateTimer = 0f;
        _hasAttacked = false;

        switch (next)
        {
            case State.Idle:
            case State.Recovery:
                SetSmokeForm(false);
                Fsm.StopAllMovement();
                break;
            case State.SmokeEnter:
                SetSmokeForm(true);
                Fsm.StopAllMovement();
                PlaySmokeAnimation(TeleportAnimationState);
                break;
            case State.SmokeAppear:
                SetSmokeForm(true);
                Fsm.StopAllMovement();
                PlaySmokeAnimation(TeleportAppearAnimationState);
                break;
            case State.RangedAttack:
            case State.ScratchWindup:
            case State.Stunned:
                SetSmokeForm(false);
                Fsm.StopAllMovement();
                break;
        }
    }

    private void UpdateIdle()
    {
        _stateTimer += Time.deltaTime;
        if (_stateTimer < idleDuration) return;
        int min = Mathf.Max(1, teleportCountMin);
        int max = Mathf.Max(min, teleportCountMax);
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
        _stateTimer += Time.deltaTime;
        if (_hasAttacked || _stateTimer < rangedWindupDuration) return;
        FireRandomRangedAttack();
        _hasAttacked = true;
        CompleteAttack();
    }

    private void UpdateScratchWindup()
    {
        FacePlayer();
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
        _stateTimer += Time.deltaTime;
        if (_stateTimer >= recoveryDuration) ChangeState(State.Idle);
    }

    private void UpdateStunned()
    {
        _stateTimer += Time.deltaTime;
        if (_stateTimer < _stunDuration) return;
        _stunDuration = 0f;
        ChangeState(State.Recovery);
    }

    private void CompleteAttack()
    {
        _teleportsCompleted++;
        ChangeState(_teleportsCompleted >= _teleportCount ? State.Recovery : State.SmokeEnter);
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
    }

    private void SpawnProjectile(Vector2 origin, Vector2 direction, float damage)
    {
        CheshireProjectile projectile = Instantiate(projectilePrefab, origin, Quaternion.identity);
        projectile.Launch(direction, projectileSpeed, damage, gameObject);
    }

    private void TeleportToRandomMapPosition()
    {
        Vector2 halfSize = teleportAreaSize * 0.5f;
        Vector2 followTarget = Fsm.Player != null ? (Vector2)Fsm.Player.position : _fallbackTeleportOrigin;
        Vector2 center = followTarget + teleportAreaOffset;
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

        Debug.LogWarning("[CheshireCat] No open teleport position found inside the player-following area.", this);
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
        Vector2 origin = Application.isPlaying && Fsm != null && Fsm.Player != null
            ? (Vector2)Fsm.Player.position
            : (Vector2)transform.position;
        Gizmos.DrawWireCube(origin + teleportAreaOffset, teleportAreaSize);
    }

    private void OnValidate()
    {
        idleDuration = Mathf.Max(0f, idleDuration);
        smokeDuration = Mathf.Max(0f, smokeDuration);
        recoveryDuration = Mathf.Max(0f, recoveryDuration);
        teleportCountMin = Mathf.Max(1, teleportCountMin);
        teleportCountMax = Mathf.Max(teleportCountMin, teleportCountMax);
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
    }
}
