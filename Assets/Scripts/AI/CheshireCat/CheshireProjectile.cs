using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class CheshireProjectile : MonoBehaviour, IDamageable
{
    [SerializeField, Min(0.1f)] private float lifetime = 5f;

    public static event Action<GameObject, GameObject> CloneDebuffRequested;

    private Rigidbody2D _rigidbody;
    private GameObject _source;
    private Transform _homingTarget;
    private float _damage;
    private float _speed;
    private float _turnSpeed;
    private int _remainingSuccessesToDestroy = 1;
    private float _deflectSpeedMultiplier = 1f;
    private float _deflectHomingDelay;
    private float _homingPauseTimer;
    private bool _requestsCloneDebuff;
    private bool _hasHit;
    private float _remainingLifetime;
    private CheshireCatAI _poolOwner;
    private SpriteRenderer _spriteRenderer;
    private TrailRenderer _trail;
    private ParticleSystem[] _particleSystems;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _trail = GetComponentInChildren<TrailRenderer>();
        _particleSystems = GetComponentsInChildren<ParticleSystem>(true);
        SetParticlesToWorldSpace();
    }

    private void Update()
    {
        if (_remainingLifetime <= 0f) return;

        _remainingLifetime -= Time.deltaTime;
        if (_remainingLifetime <= 0f) Despawn();
    }

    public void SetPoolOwner(CheshireCatAI owner)
    {
        _poolOwner = owner;
    }

    public void Launch(Vector2 direction, float speed, float damage, GameObject source)
    {
        if (direction.sqrMagnitude < 0.01f) direction = Vector2.right;
        _source = source;
        _damage = damage;
        _homingTarget = null;
        _speed = speed;
        _turnSpeed = 0f;
        _remainingSuccessesToDestroy = 1;
        _deflectSpeedMultiplier = 1f;
        _deflectHomingDelay = 0f;
        _homingPauseTimer = 0f;
        _requestsCloneDebuff = false;
        _hasHit = false;
        _remainingLifetime = lifetime;
        RestartVisuals();
        _rigidbody.linearVelocity = direction.normalized * speed;
        transform.up = direction;
    }

    public void LaunchHoming(
        Transform target,
        Vector2 initialDirection,
        float speed,
        float turnSpeed,
        float damage,
        int successesToDestroy,
        Color color,
        bool requestsCloneDebuff,
        GameObject source,
        float deflectSpeedMultiplier,
        float deflectHomingDelay,
        float minimumLifetime)
    {
        Launch(initialDirection, speed, damage, source);
        _homingTarget = target;
        _turnSpeed = Mathf.Max(0f, turnSpeed);
        _remainingSuccessesToDestroy = Mathf.Max(1, successesToDestroy);
        _deflectSpeedMultiplier = Mathf.Max(0f, deflectSpeedMultiplier);
        _homingPauseTimer = 0f;
        _requestsCloneDebuff = requestsCloneDebuff;
        _deflectHomingDelay = Mathf.Max(0f, deflectHomingDelay);
        _remainingLifetime = Mathf.Max(_remainingLifetime, minimumLifetime);
        ApplyColor(color);
    }

    private void FixedUpdate()
    {
        if (_homingTarget == null || _turnSpeed <= 0f || _hasHit) return;
        if (_homingPauseTimer > 0f)
        {
            _homingPauseTimer = Mathf.Max(0f, _homingPauseTimer - Time.fixedDeltaTime);
            return;
        }

        Vector2 currentDirection = _rigidbody.linearVelocity.normalized;
        Vector2 desiredDirection = ((Vector2)_homingTarget.position - _rigidbody.position).normalized;
        if (desiredDirection.sqrMagnitude < 0.01f) return;
        if (currentDirection.sqrMagnitude < 0.01f) currentDirection = desiredDirection;

        float maxRadians = _turnSpeed * Mathf.Deg2Rad * Time.fixedDeltaTime;
        Vector2 steeredDirection = Vector3.RotateTowards(currentDirection, desiredDirection, maxRadians, 0f);
        _rigidbody.linearVelocity = steeredDirection.normalized * _speed;
        transform.up = steeredDirection;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_hasHit) return;
        if (_source != null && (other.gameObject == _source || other.transform.IsChildOf(_source.transform))) return;
        if (other.CompareTag("Enemy")) return;

        Transform root = other.transform.root;
        if (other.CompareTag("Player") || root.CompareTag("Player"))
        {
            PlayerParry parry = other.GetComponentInParent<PlayerParry>();
            if (parry != null && (parry.IsParryTime || parry.IsGuardTime))
            {
                IDamageable guardedTarget = other.GetComponentInParent<IDamageable>();
                guardedTarget?.TakeDamage(_damage, _source);
                RegisterDefenseSuccess(root.position);
                return;
            }

            if (_requestsCloneDebuff)
            {
                _hasHit = true;
                CloneDebuffRequested?.Invoke(root.gameObject, _source);
                Despawn();
                return;
            }

            IDamageable target = other.GetComponentInParent<IDamageable>();
            if (target == null) return;

            _hasHit = true;
            target.TakeDamage(_damage, _source);
            Despawn();
            return;
        }

    }

    public void TakeDamage(float damage)
    {
        TakeDamage(damage, null);
    }

    public void TakeDamage(float damage, GameObject source)
    {
        if (_hasHit || damage <= 0f) return;
        Vector2 defenderPosition = source != null ? source.transform.position : transform.position;
        RegisterDefenseSuccess(defenderPosition);
    }

    private void RegisterDefenseSuccess(Vector2 defenderPosition)
    {
        if (_hasHit) return;

        _remainingSuccessesToDestroy--;
        if (_remainingSuccessesToDestroy <= 0)
        {
            _hasHit = true;
            Despawn();
            return;
        }

        Vector2 deflectDirection = (Vector2)transform.position - defenderPosition;
        if (deflectDirection.sqrMagnitude < 0.01f)
        {
            deflectDirection = -_rigidbody.linearVelocity;
        }
        if (deflectDirection.sqrMagnitude < 0.01f) deflectDirection = Vector2.right;

        deflectDirection.Normalize();
        _rigidbody.linearVelocity = deflectDirection * _speed * _deflectSpeedMultiplier;
        transform.up = deflectDirection;
        _homingPauseTimer = _deflectHomingDelay;
    }

    private void ApplyColor(Color color)
    {
        if (_spriteRenderer != null) _spriteRenderer.color = color;

        if (_trail == null) return;
        _trail.startColor = color;
        _trail.endColor = new Color(color.r, color.g, color.b, 0f);
    }

    private void SetParticlesToWorldSpace()
    {
        for (int i = 0; i < _particleSystems.Length; i++)
        {
            ParticleSystem.MainModule main = _particleSystems[i].main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
        }
    }

    private void RestartVisuals()
    {
        if (_trail != null) _trail.Clear();
        for (int i = 0; i < _particleSystems.Length; i++)
        {
            _particleSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _particleSystems[i].Play(true);
        }
    }

    private void Despawn()
    {
        _remainingLifetime = 0f;
        _homingTarget = null;
        _rigidbody.linearVelocity = Vector2.zero;
        if (_trail != null) _trail.Clear();
        for (int i = 0; i < _particleSystems.Length; i++)
        {
            _particleSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (_poolOwner != null)
        {
            _poolOwner.ReleaseProjectile(this);
            return;
        }

        Destroy(gameObject);
    }

    private void OnValidate()
    {
        lifetime = Mathf.Max(0.1f, lifetime);
    }
}
