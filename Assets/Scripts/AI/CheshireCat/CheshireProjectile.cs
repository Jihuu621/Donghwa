using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class CheshireProjectile : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float lifetime = 5f;

    private Rigidbody2D _rigidbody;
    private GameObject _source;
    private float _damage;
    private bool _hasHit;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
    }

    public void Launch(Vector2 direction, float speed, float damage, GameObject source)
    {
        if (direction.sqrMagnitude < 0.01f) direction = Vector2.right;
        _source = source;
        _damage = damage;
        _hasHit = false;
        _rigidbody.linearVelocity = direction.normalized * speed;
        transform.up = direction;
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_hasHit) return;
        if (_source != null && (other.gameObject == _source || other.transform.IsChildOf(_source.transform))) return;
        if (other.CompareTag("Enemy")) return;

        Transform root = other.transform.root;
        if (other.CompareTag("Player") || root.CompareTag("Player"))
        {
            IDamageable target = other.GetComponentInParent<IDamageable>();
            if (target != null)
            {
                _hasHit = true;
                target.TakeDamage(_damage, _source);
                Destroy(gameObject);
                return;
            }
        }

        //if (other.CompareTag("Ground") || other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        //{
        //    _hasHit = true;
        //    Destroy(gameObject);
        //}
    }

    private void OnValidate()
    {
        lifetime = Mathf.Max(0.1f, lifetime);
    }
}
