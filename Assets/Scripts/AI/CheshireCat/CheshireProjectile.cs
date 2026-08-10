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
    private float _health = 1f;
    private bool _requestsCloneDebuff;
    private bool _hasHit;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        SetParticlesToWorldSpace();
    }

    public void Launch(Vector2 direction, float speed, float damage, GameObject source)
    {
        if (direction.sqrMagnitude < 0.01f) direction = Vector2.right;
        _source = source;
        _damage = damage;
        _homingTarget = null;
        _speed = speed;
        _turnSpeed = 0f;
        _health = 1f;
        _requestsCloneDebuff = false;
        _hasHit = false;
        _rigidbody.linearVelocity = direction.normalized * speed;
        transform.up = direction;
        Destroy(gameObject, lifetime);
    }

    public void LaunchHoming(
        Transform target,
        Vector2 initialDirection,
        float speed,
        float turnSpeed,
        float damage,
        float health,
        Color color,
        bool requestsCloneDebuff,
        GameObject source)
    {
        Launch(initialDirection, speed, damage, source);
        _homingTarget = target;
        _turnSpeed = Mathf.Max(0f, turnSpeed);
        _health = Mathf.Max(0.1f, health);
        _requestsCloneDebuff = requestsCloneDebuff;
        ApplyColor(color);
    }

    private void FixedUpdate()
    {
        if (_homingTarget == null || _turnSpeed <= 0f || _hasHit) return;

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
            if (_requestsCloneDebuff)
            {
                _hasHit = true;
                CloneDebuffRequested?.Invoke(root.gameObject, _source);
                Destroy(gameObject);
                return;
            }

            IDamageable target = other.GetComponentInParent<IDamageable>();
            if (target == null) return;

            _hasHit = true;
            target.TakeDamage(_damage, _source);
            Destroy(gameObject);
            return;
        }

        //if (other.CompareTag("Ground") || other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        //{
        //    _hasHit = true;
        //    Destroy(gameObject);
        //}
    }

    public void TakeDamage(float damage)
    {
        TakeDamage(damage, null);
    }

    public void TakeDamage(float damage, GameObject source)
    {
        if (_hasHit || damage <= 0f) return;
        _health -= damage;
        if (_health > 0f) return;

        _hasHit = true;
        Destroy(gameObject);
    }

    private void ApplyColor(Color color)
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) spriteRenderer.color = color;

        TrailRenderer trail = GetComponentInChildren<TrailRenderer>();
        if (trail == null) return;
        trail.startColor = color;
        trail.endColor = new Color(color.r, color.g, color.b, 0f);
    }

    private void SetParticlesToWorldSpace()
    {
        ParticleSystem[] particleSystems = GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem.MainModule main = particleSystems[i].main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
        }
    }

    private void OnValidate()
    {
        lifetime = Mathf.Max(0.1f, lifetime);
    }
}
