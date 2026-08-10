using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class CheshireAfterimageTrail : MonoBehaviour
{
    private SpriteRenderer _sourceRenderer;
    private Rigidbody2D _rigidbody;
    private float _interval;
    private float _lifetime;
    private float _minimumDistance;
    private float _timer;
    private Color _color;
    private Vector2 _lastPosition;
    private bool _emitting;

    private void Awake()
    {
        _sourceRenderer = GetComponent<SpriteRenderer>();
        _rigidbody = GetComponent<Rigidbody2D>();
    }

    public void Configure(float interval, float lifetime, float minimumDistance, Color color)
    {
        _interval = Mathf.Max(0.05f, interval);
        _lifetime = Mathf.Max(0.05f, lifetime);
        _minimumDistance = Mathf.Max(0.01f, minimumDistance);
        _color = color;
    }

    public void SetEmitting(bool enabled)
    {
        _emitting = enabled;
        _timer = 0f;
        _lastPosition = transform.position;
    }

    private void Update()
    {
        if (!_emitting || _sourceRenderer == null || _sourceRenderer.sprite == null) return;

        _timer -= Time.deltaTime;
        if (_timer > 0f) return;

        Vector2 position = transform.position;
        bool isMoving = _rigidbody != null
            ? _rigidbody.linearVelocity.sqrMagnitude > 0.04f
            : Vector2.Distance(position, _lastPosition) >= _minimumDistance;
        if (!isMoving || Vector2.Distance(position, _lastPosition) < _minimumDistance) return;

        SpawnAfterimage();
        _lastPosition = position;
        _timer = _interval;
    }

    private void SpawnAfterimage()
    {
        GameObject ghostObject = new GameObject("CheshireAfterimage");
        ghostObject.layer = gameObject.layer;
        ghostObject.transform.SetPositionAndRotation(transform.position, transform.rotation);
        ghostObject.transform.localScale = transform.lossyScale;

        SpriteRenderer ghostRenderer = ghostObject.AddComponent<SpriteRenderer>();
        ghostRenderer.sprite = _sourceRenderer.sprite;
        ghostRenderer.flipX = _sourceRenderer.flipX;
        ghostRenderer.flipY = _sourceRenderer.flipY;
        ghostRenderer.sharedMaterial = _sourceRenderer.sharedMaterial;
        ghostRenderer.sortingLayerID = _sourceRenderer.sortingLayerID;
        ghostRenderer.sortingOrder = _sourceRenderer.sortingOrder - 1;
        ghostRenderer.color = new Color(_color.r, _color.g, _color.b, _color.a * _sourceRenderer.color.a);

        CheshireAfterimageGhost ghost = ghostObject.AddComponent<CheshireAfterimageGhost>();
        ghost.Initialize(ghostRenderer, _lifetime);
    }
}

public sealed class CheshireAfterimageGhost : MonoBehaviour
{
    private SpriteRenderer _renderer;
    private Color _startColor;
    private float _lifetime;
    private float _elapsed;

    public void Initialize(SpriteRenderer spriteRenderer, float lifetime)
    {
        _renderer = spriteRenderer;
        _startColor = spriteRenderer.color;
        _lifetime = Mathf.Max(0.05f, lifetime);
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(_elapsed / _lifetime);
        if (_renderer != null)
        {
            Color color = _startColor;
            color.a *= 1f - progress;
            _renderer.color = color;
        }

        if (progress >= 1f) Destroy(gameObject);
    }
}
