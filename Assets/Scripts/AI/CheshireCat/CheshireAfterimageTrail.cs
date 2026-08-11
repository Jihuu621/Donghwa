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
    private GameObject _poolRoot;
    private CheshireAfterimageGhost[] _ghostPool;
    private int _nextGhostIndex;

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
        EnsurePoolCapacity(Mathf.CeilToInt(_lifetime / _interval) + 2);
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
        if (_ghostPool == null || _ghostPool.Length == 0) return;

        CheshireAfterimageGhost ghost = _ghostPool[_nextGhostIndex];
        _nextGhostIndex = (_nextGhostIndex + 1) % _ghostPool.Length;
        ghost.Show(
            _sourceRenderer,
            transform.position,
            transform.rotation,
            transform.lossyScale,
            gameObject.layer,
            _color,
            _lifetime);
    }

    private void EnsurePoolCapacity(int requiredCapacity)
    {
        if (_ghostPool != null && _ghostPool.Length >= requiredCapacity) return;

        if (_poolRoot == null)
        {
            _poolRoot = new GameObject($"{name}_AfterimagePool");
            _poolRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        int previousCapacity = _ghostPool != null ? _ghostPool.Length : 0;
        CheshireAfterimageGhost[] expandedPool = new CheshireAfterimageGhost[requiredCapacity];
        for (int i = 0; i < previousCapacity; i++) expandedPool[i] = _ghostPool[i];

        for (int i = previousCapacity; i < requiredCapacity; i++)
        {
            GameObject ghostObject = new GameObject("CheshireAfterimage");
            ghostObject.transform.SetParent(_poolRoot.transform, false);
            SpriteRenderer ghostRenderer = ghostObject.AddComponent<SpriteRenderer>();
            CheshireAfterimageGhost ghost = ghostObject.AddComponent<CheshireAfterimageGhost>();
            ghost.Initialize(ghostRenderer);
            ghostObject.SetActive(false);
            expandedPool[i] = ghost;
        }

        _ghostPool = expandedPool;
        _nextGhostIndex = 0;
    }

    private void OnDestroy()
    {
        if (_poolRoot != null) Destroy(_poolRoot);
    }
}

public sealed class CheshireAfterimageGhost : MonoBehaviour
{
    private SpriteRenderer _renderer;
    private Color _startColor;
    private float _lifetime;
    private float _elapsed;

    public void Initialize(SpriteRenderer spriteRenderer)
    {
        _renderer = spriteRenderer;
    }

    public void Show(
        SpriteRenderer source,
        Vector3 position,
        Quaternion rotation,
        Vector3 scale,
        int layer,
        Color color,
        float lifetime)
    {
        gameObject.SetActive(false);
        gameObject.layer = layer;
        transform.SetPositionAndRotation(position, rotation);
        transform.localScale = scale;

        _renderer.sprite = source.sprite;
        _renderer.flipX = source.flipX;
        _renderer.flipY = source.flipY;
        _renderer.sharedMaterial = source.sharedMaterial;
        _renderer.sortingLayerID = source.sortingLayerID;
        _renderer.sortingOrder = source.sortingOrder - 1;
        _startColor = new Color(color.r, color.g, color.b, color.a * source.color.a);
        _renderer.color = _startColor;
        _lifetime = Mathf.Max(0.05f, lifetime);
        _elapsed = 0f;
        gameObject.SetActive(true);
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

        if (progress >= 1f) gameObject.SetActive(false);
    }
}
