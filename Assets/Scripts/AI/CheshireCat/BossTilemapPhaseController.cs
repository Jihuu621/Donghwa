using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Serialization;

public sealed class BossTilemapPhaseController : MonoBehaviour
{
    private const int MaxPhaseCount = 5;

    [Header("References")]
    [SerializeField] private CheshireCatHealth bossHealth;
    [SerializeField] private Transform tilemapRoot;

    [Header("Phase Order (100%, 80%, 60%, 40%, 20%)")]
    [Tooltip("Leave empty to use top-level Tilemaps under Tilemap Root in hierarchy order. Each Tilemap and all of its children form one phase.")]
    [SerializeField] private List<Tilemap> phaseTilemaps = new List<Tilemap>();

    [Header("Spectral Transition")]
    [FormerlySerializedAs("previewDuration")]
    [SerializeField, Min(0.1f)] private float disappearDuration = 0.65f;
    [SerializeField, Min(0f)] private float emptyDuration = 0.12f;
    [FormerlySerializedAs("dissolveDuration")]
    [SerializeField, Min(0.1f)] private float appearDuration = 0.45f;
    [SerializeField] private Color spectralColor = new Color(0.65f, 0.4f, 1f, 1f);

    private readonly List<Tilemap[]> _phaseTilemapVisuals = new List<Tilemap[]>();
    private readonly List<Color[]> _tilemapColors = new List<Color[]>();
    private readonly List<SpriteRenderer[]> _phaseSpriteVisuals = new List<SpriteRenderer[]>();
    private readonly List<Color[]> _spriteColors = new List<Color[]>();
    private readonly List<Collider2D[]> _colliders = new List<Collider2D[]>();
    private readonly List<bool[]> _colliderStates = new List<bool[]>();
    private Coroutine _transition;
    private int _requestedPhase = -1;
    private bool _started;

    private readonly List<Tilemap> _resolvedTilemaps = new List<Tilemap>(MaxPhaseCount);
    private bool _subscribed;
    private int _activePhaseIndex = -1;

    private void Awake()
    {
        if (tilemapRoot == null) tilemapRoot = transform;
        if (bossHealth == null) bossHealth = FindAnyObjectByType<CheshireCatHealth>();

        ResolveTilemaps();
    }

    private void OnEnable()
    {
        Subscribe();
        if (_started && bossHealth != null)
            ApplyHealth(bossHealth.CurrentHP, bossHealth.MaxHP, true);
    }

    private void Start()
    {
        _started = true;
        if (bossHealth == null)
        {
            Debug.LogError("[BossTilemapPhaseController] CheshireCatHealth를 찾을 수 없습니다.", this);
            return;
        }

        ApplyHealth(bossHealth.CurrentHP, bossHealth.MaxHP, true);
    }

    private void OnDisable()
    {
        Unsubscribe();
        if (_transition != null) StopCoroutine(_transition);
        _transition = null;
        if (_activePhaseIndex >= 0) Settle(_activePhaseIndex);
    }

    private void ResolveTilemaps()
    {
        _resolvedTilemaps.Clear();
        _phaseTilemapVisuals.Clear();
        _tilemapColors.Clear();
        _phaseSpriteVisuals.Clear();
        _spriteColors.Clear();
        _colliders.Clear();
        _colliderStates.Clear();

        for (int i = 0; i < phaseTilemaps.Count && _resolvedTilemaps.Count < MaxPhaseCount; i++)
        {
            AddIfValid(phaseTilemaps[i]);
        }

        if (_resolvedTilemaps.Count > 0) return;

        Tilemap[] discovered = tilemapRoot.GetComponentsInChildren<Tilemap>(true);
        for (int i = 0; i < discovered.Length && _resolvedTilemaps.Count < MaxPhaseCount; i++)
        {
            if (HasTilemapAncestorWithinRoot(discovered[i])) continue;
            AddIfValid(discovered[i]);
        }
    }

    private bool HasTilemapAncestorWithinRoot(Tilemap candidate)
    {
        Transform current = candidate.transform.parent;
        while (current != null && current != tilemapRoot)
        {
            if (current.GetComponent<Tilemap>() != null) return true;
            current = current.parent;
        }
        return false;
    }

    private void AddIfValid(Tilemap tilemap)
    {
        if (tilemap != null && !_resolvedTilemaps.Contains(tilemap))
        {
            _resolvedTilemaps.Add(tilemap);

            Tilemap[] tilemapVisuals = tilemap.GetComponentsInChildren<Tilemap>(true);
            _phaseTilemapVisuals.Add(tilemapVisuals);
            Color[] tilemapColors = new Color[tilemapVisuals.Length];
            for (int i = 0; i < tilemapVisuals.Length; i++)
                tilemapColors[i] = tilemapVisuals[i].color;
            _tilemapColors.Add(tilemapColors);

            SpriteRenderer[] spriteVisuals = tilemap.GetComponentsInChildren<SpriteRenderer>(true);
            _phaseSpriteVisuals.Add(spriteVisuals);
            Color[] spriteColors = new Color[spriteVisuals.Length];
            for (int i = 0; i < spriteVisuals.Length; i++)
                spriteColors[i] = spriteVisuals[i].color;
            _spriteColors.Add(spriteColors);

            Collider2D[] colliders = tilemap.GetComponentsInChildren<Collider2D>(true);
            _colliders.Add(colliders);
            bool[] states = new bool[colliders.Length];
            for (int i = 0; i < colliders.Length; i++) states[i] = colliders[i].enabled;
            _colliderStates.Add(states);
        }
    }

    private void Subscribe()
    {
        if (_subscribed || bossHealth == null) return;
        bossHealth.OnHealthChanged += HandleHealthChanged;
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed || bossHealth == null) return;
        bossHealth.OnHealthChanged -= HandleHealthChanged;
        _subscribed = false;
    }

    private void HandleHealthChanged(float currentHP, float maxHP)
    {
        ApplyHealth(currentHP, maxHP, false);
    }

    private void ApplyHealth(float currentHP, float maxHP, bool force)
    {
        if (_resolvedTilemaps.Count == 0 || maxHP <= 0f) return;

        float healthRatio = Mathf.Clamp01(currentHP / maxHP);
        int requestedPhase = GetPhaseIndex(healthRatio);
        int availablePhase = Mathf.Min(requestedPhase, _resolvedTilemaps.Count - 1);

        _requestedPhase = availablePhase;
        if (force || _activePhaseIndex < 0)
        {
            if (_transition != null) StopCoroutine(_transition);
            _transition = null;
            Settle(availablePhase);
        }
        else if (_transition == null && availablePhase != _activePhaseIndex)
            _transition = StartCoroutine(Transition());
    }

    private IEnumerator Transition()
    {
        // Finish the current visual beat before responding to another damage burst.
        while (_requestedPhase != _activePhaseIndex)
        {
            int outgoing = _activePhaseIndex;
            int incoming = _requestedPhase;
            SetCollision(incoming, false);
            Paint(incoming, 1f, 0f);
            _resolvedTilemaps[incoming].gameObject.SetActive(false);

            // Finish dissolving the old layout before revealing any of the next one.
            float elapsed = 0f;
            float duration = Mathf.Max(0.1f, disappearDuration);
            while (elapsed < duration)
            {
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                Paint(outgoing, t, 1f - t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            Paint(outgoing, 1f, 0f);
            SetCollision(outgoing, false);
            _resolvedTilemaps[outgoing].gameObject.SetActive(false);
            // A brief empty beat makes the two stages visually distinct.
            yield return null;
            if (emptyDuration > 0f) yield return new WaitForSeconds(emptyDuration);

            _resolvedTilemaps[incoming].gameObject.SetActive(true);
            elapsed = 0f;
            duration = Mathf.Max(0.1f, appearDuration);
            while (elapsed < duration)
            {
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                Paint(incoming, 1f - t, t);
                elapsed += Time.deltaTime;
                yield return null;
            }
            // Only fully materialized platforms become solid.
            Settle(incoming);
        }
        _transition = null;
    }

    private void Paint(int index, float spectralBlend, float opacity)
    {
        Tilemap[] tilemaps = _phaseTilemapVisuals[index];
        Color[] tilemapColors = _tilemapColors[index];
        for (int i = 0; i < tilemaps.Length; i++)
        {
            if (tilemaps[i] == null) continue;
            Color color = Color.Lerp(tilemapColors[i], spectralColor, spectralBlend);
            color.a = tilemapColors[i].a * opacity;
            tilemaps[i].color = color;
        }

        SpriteRenderer[] sprites = _phaseSpriteVisuals[index];
        Color[] spriteColors = _spriteColors[index];
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] == null) continue;
            Color color = Color.Lerp(spriteColors[i], spectralColor, spectralBlend);
            color.a = spriteColors[i].a * opacity;
            sprites[i].color = color;
        }
    }

    private void SetCollision(int index, bool enabled)
    {
        for (int i = 0; i < _colliders[index].Length; i++)
            if (_colliders[index][i] != null)
                _colliders[index][i].enabled = enabled && _colliderStates[index][i];
    }

    private void Settle(int phase)
    {
        for (int i = 0; i < _resolvedTilemaps.Count; i++)
        {
            Paint(i, 0f, 1f);
            _resolvedTilemaps[i].gameObject.SetActive(i == phase);
            SetCollision(i, true);
        }
        _activePhaseIndex = phase;
    }

    private static int GetPhaseIndex(float healthRatio)
    {
        if (healthRatio > 0.8f) return 0;
        if (healthRatio > 0.6f) return 1;
        if (healthRatio > 0.4f) return 2;
        if (healthRatio > 0.2f) return 3;
        return 4;
    }
}
