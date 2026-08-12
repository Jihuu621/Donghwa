using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(Collider2D))]
public class CheshireCatClone : MonoBehaviour, IDamageable
{
    private const float TeleportAnimationLength = 0.3f;
    private static readonly int IdleAnimationState = Animator.StringToHash("Base Layer.Cat_Idle");
    private static readonly int TeleportAnimationState = Animator.StringToHash("Base Layer.Cat_Attack1");
    private static readonly int TeleportAppearAnimationState = Animator.StringToHash("Base Layer.Cat_TeleportAppear");
    private static readonly int PatternBFireAnimationState = Animator.StringToHash("Base Layer.Cat_PatternB");
    private const float PatternBFireAnimationLength = 1f;

    private CheshireCatAI _owner;
    private Rigidbody2D _rigidbody;
    private SpriteRenderer _spriteRenderer;
    private Collider2D _collider;
    private Animator _animator;
    private CheshireAfterimageTrail _afterimageTrail;
    private ParticleSystem _particleSystem;
    private Vector2 _areaCenter;
    private Vector2 _areaSize;
    private Vector2 _moveDirection;
    private float _moveSpeed;
    private float _directionIntervalMin;
    private float _directionIntervalMax;
    private float _turnSmoothTime;
    private float _boundaryTurnDistance;
    private float _directionTimer;
    private Vector2 _velocitySmooth;
    private float _activationTimer;
    private float _health;
    private bool _active;
    private bool _ending;
    private float _deactivationTimer;
    private float _fireAnimationTimer;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _collider = GetComponent<Collider2D>();
        _animator = GetComponent<Animator>();
        _afterimageTrail = GetComponent<CheshireAfterimageTrail>();
        _particleSystem = GetComponent<ParticleSystem>();
    }

    public void Configure(
        CheshireCatAI owner,
        float revealDuration,
        float health,
        float moveSpeed,
        Vector2 areaCenter,
        Vector2 areaSize,
        float directionIntervalMin,
        float directionIntervalMax,
        float turnSmoothTime,
        float boundaryTurnDistance)
    {
        _owner = owner;
        _health = Mathf.Max(0.1f, health);
        _moveSpeed = Mathf.Max(0f, moveSpeed);
        _areaCenter = areaCenter;
        _areaSize = areaSize;
        _directionIntervalMin = Mathf.Max(0.1f, directionIntervalMin);
        _directionIntervalMax = Mathf.Max(_directionIntervalMin, directionIntervalMax);
        _turnSmoothTime = Mathf.Max(0.01f, turnSmoothTime);
        _boundaryTurnDistance = Mathf.Max(0f, boundaryTurnDistance);
        _activationTimer = Mathf.Max(0f, revealDuration);
        _deactivationTimer = 0f;
        _active = false;
        _ending = false;
        _velocitySmooth = Vector2.zero;
        _rigidbody.linearVelocity = Vector2.zero;
        _collider.enabled = false;
        if (_afterimageTrail != null) _afterimageTrail.SetEmitting(false);
        if (_particleSystem != null)
        {
            _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _particleSystem.Play(true);
        }
        PlayAnimation(TeleportAppearAnimationState, revealDuration);
    }

    private void Update()
    {
        if (_ending)
        {
            _deactivationTimer -= Time.deltaTime;
            if (_deactivationTimer <= 0f) DeactivateImmediately();
            return;
        }
        if (_owner == null)
        {
            DeactivateImmediately();
            return;
        }

        if (!_active)
        {
            _activationTimer -= Time.deltaTime;
            if (_activationTimer > 0f) return;

            _active = true;
            _collider.enabled = true;
            if (_afterimageTrail != null) _afterimageTrail.SetEmitting(true);
            PlayIdleAnimation();
            PickMoveDirection();
        }

        if (_fireAnimationTimer > 0f)
        {
            _fireAnimationTimer -= Time.deltaTime;
            if (_fireAnimationTimer <= 0f) PlayIdleAnimation();
        }

        _directionTimer -= Time.deltaTime;
        if (_directionTimer <= 0f) PickMoveDirection();
    }

    private void FixedUpdate()
    {
        if (!_active || _ending)
        {
            _rigidbody.linearVelocity = Vector2.zero;
            _velocitySmooth = Vector2.zero;
            return;
        }

        Vector2 center = _areaCenter;
        Vector2 halfSize = _areaSize * 0.5f;
        Vector2 position = _rigidbody.position;

        float horizontalMargin = Mathf.Min(_boundaryTurnDistance, halfSize.x * 0.45f);
        float verticalMargin = Mathf.Min(_boundaryTurnDistance, halfSize.y * 0.45f);

        if (position.x <= center.x - halfSize.x + horizontalMargin && _moveDirection.x < 0f ||
            position.x >= center.x + halfSize.x - horizontalMargin && _moveDirection.x > 0f)
        {
            _moveDirection.x *= -1f;
        }

        if (position.y <= center.y - halfSize.y + verticalMargin && _moveDirection.y < 0f ||
            position.y >= center.y + halfSize.y - verticalMargin && _moveDirection.y > 0f)
        {
            _moveDirection.y *= -1f;
        }

        Vector2 targetVelocity = _moveDirection.normalized * _moveSpeed;
        _rigidbody.linearVelocity = Vector2.SmoothDamp(
            _rigidbody.linearVelocity,
            targetVelocity,
            ref _velocitySmooth,
            _turnSmoothTime,
            Mathf.Infinity,
            Time.fixedDeltaTime);

        if (_spriteRenderer != null && Mathf.Abs(_rigidbody.linearVelocity.x) > 0.05f)
        {
            _spriteRenderer.flipX = _rigidbody.linearVelocity.x > 0f;
        }
    }

    public void BeginDisappear(float duration)
    {
        if (_ending) return;
        _ending = true;
        _active = false;
        _collider.enabled = false;
        if (_afterimageTrail != null) _afterimageTrail.SetEmitting(false);
        _rigidbody.linearVelocity = Vector2.zero;
        PlayAnimation(TeleportAnimationState, duration);
        _deactivationTimer = Mathf.Max(0.01f, duration);
    }

    public void FireProjectile()
    {
        if (_active && !_ending && _owner != null)
        {
            _fireAnimationTimer = PatternBFireAnimationLength;
            if (_animator != null)
            {
                _animator.speed = 1f;
                _animator.Play(PatternBFireAnimationState, 0, 0f);
            }
            _owner.FirePatternBProjectile(transform.position, true, gameObject);
        }
    }

    public void TakeDamage(float damage)
    {
        TakeDamage(damage, null);
    }

    public void TakeDamage(float damage, GameObject source)
    {
        if (!_active || _ending || damage <= 0f) return;
        _health -= damage;
        if (_health <= 0f) DeactivateImmediately();
    }

    public void DeactivateImmediately()
    {
        if (!gameObject.activeSelf) return;

        ReleaseAttachedNeedles();
        _active = false;
        _ending = false;
        _deactivationTimer = 0f;
        _collider.enabled = false;
        _rigidbody.linearVelocity = Vector2.zero;
        _velocitySmooth = Vector2.zero;
        if (_afterimageTrail != null) _afterimageTrail.SetEmitting(false);
        if (_particleSystem != null)
        {
            _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        CheshireCatAI owner = _owner;
        gameObject.SetActive(false);
        if (owner != null) owner.NotifyCloneReleased(this);
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

    private void PickMoveDirection()
    {
        _moveDirection = Random.insideUnitCircle.normalized;
        if (_moveDirection.sqrMagnitude < 0.01f) _moveDirection = Vector2.right;
        _directionTimer = Random.Range(_directionIntervalMin, _directionIntervalMax);
    }

    private void PlayAnimation(int stateHash, float duration)
    {
        if (_animator == null) return;
        _animator.speed = duration > 0f ? TeleportAnimationLength / duration : 1f;
        _animator.Play(stateHash, 0, 0f);
    }

    private void PlayIdleAnimation()
    {
        if (_animator == null) return;
        _animator.speed = 1f;
        _animator.Play(IdleAnimationState, 0, 0f);
    }

    private void OnDestroy()
    {
        if (_owner != null) _owner.NotifyCloneDestroyed(this);
    }
}
