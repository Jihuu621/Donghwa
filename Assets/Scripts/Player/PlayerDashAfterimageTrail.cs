using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class PlayerDashAfterimageTrail : MonoBehaviour
{
    private const string StencilMaterialResource = "PlayerDashStencil";

    private SpriteRenderer _sourceRenderer;
    private Material _stencilMaterial;
    private Material _runtimeStencilMaterial;
    private DashAfterimageGhost[] _ghostPool;
    private GameObject _poolRoot;
    private float _interval;
    private float _lifetime;
    private float _minimumDistance;
    private float _timer;
    private Color _color;
    private Vector2 _lastPosition;
    private int _nextGhostIndex;
    private bool _emitting;

    private void Awake()
    {
        _sourceRenderer = GetComponent<SpriteRenderer>();
        _stencilMaterial = Resources.Load<Material>(StencilMaterialResource);
        if (_stencilMaterial == null)
        {
            Shader stencilShader = Shader.Find("Donghwa/Player Dash Stencil");
            if (stencilShader != null)
            {
                _runtimeStencilMaterial = new Material(stencilShader)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                _stencilMaterial = _runtimeStencilMaterial;
            }
        }
    }

    public void Configure(float interval, float lifetime, float minimumDistance, Color color)
    {
        _interval = Mathf.Max(0.01f, interval);
        _lifetime = Mathf.Max(0.05f, lifetime);
        _minimumDistance = Mathf.Max(0.01f, minimumDistance);
        _color = color;
        EnsurePoolCapacity(Mathf.Clamp(Mathf.CeilToInt(_lifetime / _interval) + 3, 4, 24));
    }

    public void SetEmitting(bool enabled)
    {
        _emitting = enabled;
        _timer = 0f;
        _lastPosition = transform.position;

        if (enabled) SpawnAfterimage();
    }

    private void LateUpdate()
    {
        if (!_emitting || _sourceRenderer == null || _sourceRenderer.sprite == null) return;

        _timer -= Time.deltaTime;
        if (_timer > 0f) return;

        Vector2 position = transform.position;
        if (Vector2.Distance(position, _lastPosition) < _minimumDistance) return;

        SpawnAfterimage();
        _lastPosition = position;
        _timer = _interval;
    }

    private void SpawnAfterimage()
    {
        if (_sourceRenderer == null || _sourceRenderer.sprite == null ||
            _ghostPool == null || _ghostPool.Length == 0)
        {
            return;
        }

        DashAfterimageGhost ghost = _ghostPool[_nextGhostIndex];
        _nextGhostIndex = (_nextGhostIndex + 1) % _ghostPool.Length;
        ghost.Show(_sourceRenderer, _stencilMaterial, _color, _lifetime);
    }

    private void EnsurePoolCapacity(int requiredCapacity)
    {
        if (_ghostPool != null && _ghostPool.Length >= requiredCapacity) return;

        if (_poolRoot == null)
        {
            _poolRoot = new GameObject($"{name}_DashAfterimagePool");
            _poolRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        int previousCapacity = _ghostPool != null ? _ghostPool.Length : 0;
        DashAfterimageGhost[] expandedPool = new DashAfterimageGhost[requiredCapacity];
        for (int i = 0; i < previousCapacity; i++) expandedPool[i] = _ghostPool[i];

        for (int i = previousCapacity; i < requiredCapacity; i++)
        {
            GameObject ghostObject = new GameObject("PlayerDashAfterimage");
            ghostObject.transform.SetParent(_poolRoot.transform, false);
            SpriteRenderer ghostRenderer = ghostObject.AddComponent<SpriteRenderer>();
            DashAfterimageGhost ghost = ghostObject.AddComponent<DashAfterimageGhost>();
            ghost.Initialize(ghostRenderer);
            ghostObject.SetActive(false);
            expandedPool[i] = ghost;
        }

        _ghostPool = expandedPool;
        _nextGhostIndex = 0;
    }

    private void OnDisable()
    {
        _emitting = false;
    }

    private void OnDestroy()
    {
        if (_poolRoot != null) Destroy(_poolRoot);
        if (_runtimeStencilMaterial != null) Destroy(_runtimeStencilMaterial);
    }
}

public sealed class DashAfterimageGhost : MonoBehaviour
{
    private SpriteRenderer _renderer;
    private Color _startColor;
    private float _lifetime;
    private float _elapsed;

    public void Initialize(SpriteRenderer spriteRenderer)
    {
        _renderer = spriteRenderer;
    }

    public void Show(SpriteRenderer source, Material stencilMaterial, Color color, float lifetime)
    {
        gameObject.SetActive(false);
        gameObject.layer = source.gameObject.layer;
        transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
        transform.localScale = source.transform.lossyScale;

        _renderer.sprite = source.sprite;
        _renderer.flipX = source.flipX;
        _renderer.flipY = source.flipY;
        _renderer.drawMode = source.drawMode;
        _renderer.size = source.size;
        _renderer.maskInteraction = source.maskInteraction;
        _renderer.sharedMaterial = stencilMaterial != null ? stencilMaterial : source.sharedMaterial;
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
            Color fadedColor = _startColor;
            fadedColor.a *= 1f - progress;
            _renderer.color = fadedColor;
        }

        if (progress >= 1f) gameObject.SetActive(false);
    }
}
