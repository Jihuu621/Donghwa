using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Serialization;

public sealed class BossTilemapPhaseController : MonoBehaviour
{
    private const int MaxPhaseCount = 8;

    [Header("References")]
    [SerializeField] private CheshireCatHealth bossHealth;
    [SerializeField] private Transform tilemapRoot;

    [Header("Phase Maps")]
    [Tooltip("Leave empty to use top-level Tilemaps under Tilemap Root in hierarchy order. Each Tilemap and all of its children form one phase.")]
    [SerializeField] private List<Tilemap> phaseTilemaps = new List<Tilemap>();

    [Header("Phase Cycle")]
    [Tooltip("1-based map numbers. The sequence wraps back to its first entry.")]
    [SerializeField] private List<int> phaseMapOrder = new List<int> { 1, 3, 6, 2, 4, 7, 5 };
    [SerializeField, Min(0.1f)] private float normalMapDamageToAdvance = 100f;
    [SerializeField, Min(0.1f)] private float mapOneAndTwoDamageToAdvance = 40f;
    [SerializeField, Min(0.1f)] private float automaticAdvanceSeconds = 20f;

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
    private readonly List<int> _phaseSequence = new List<int>(MaxPhaseCount);
    private bool _subscribed;
    private int _activePhaseIndex = -1;
    private int _cyclePosition;
    private float _damageTakenThisMap;
    private float _mapElapsed;
    private float _lastObservedHealth;

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
        {
            _lastObservedHealth = bossHealth.CurrentHP;
            if (_activePhaseIndex < 0) InitializeCycle();
            else if (_transition == null && _requestedPhase != _activePhaseIndex)
                _transition = StartCoroutine(Transition());
        }
    }

    private void Start()
    {
        _started = true;
        if (bossHealth == null)
        {
            Debug.LogError("[BossTilemapPhaseController] CheshireCatHealth를 찾을 수 없습니다.", this);
            return;
        }

        InitializeCycle();
    }

    private void Update()
    {
        if (!_started || bossHealth == null || bossHealth.CurrentHP <= 0f ||
            _phaseSequence.Count == 0 || _transition != null)
            return;

        _mapElapsed += Time.deltaTime;
        if (_mapElapsed >= automaticAdvanceSeconds) AdvanceToNextMap();
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

        if (_resolvedTilemaps.Count == 0)
        {
            Tilemap[] discovered = tilemapRoot.GetComponentsInChildren<Tilemap>(true);
            for (int i = 0; i < discovered.Length && _resolvedTilemaps.Count < MaxPhaseCount; i++)
            {
                if (HasTilemapAncestorWithinRoot(discovered[i])) continue;
                AddIfValid(discovered[i]);
            }
        }

        ResolvePhaseSequence();
    }

    private void ResolvePhaseSequence()
    {
        _phaseSequence.Clear();
        for (int i = 0; phaseMapOrder != null && i < phaseMapOrder.Count; i++)
        {
            int index = phaseMapOrder[i] - 1;
            if (index < 0 || index >= _resolvedTilemaps.Count || _phaseSequence.Contains(index)) continue;
            _phaseSequence.Add(index);
        }

        if (_phaseSequence.Count > 0) return;
        for (int i = 0; i < _resolvedTilemaps.Count; i++) _phaseSequence.Add(i);
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
        float damage = Mathf.Max(0f, _lastObservedHealth - currentHP);
        _lastObservedHealth = currentHP;
        if (!_started || currentHP <= 0f || damage <= 0f) return;

        _damageTakenThisMap += damage;
        if (_damageTakenThisMap >= GetCurrentDamageThreshold()) AdvanceToNextMap();
    }

    private void InitializeCycle()
    {
        if (_phaseSequence.Count == 0) return;

        if (_transition != null) StopCoroutine(_transition);
        _transition = null;
        _cyclePosition = 0;
        _requestedPhase = _phaseSequence[_cyclePosition];
        _damageTakenThisMap = 0f;
        _mapElapsed = 0f;
        _lastObservedHealth = bossHealth.CurrentHP;
        Settle(_requestedPhase);
    }

    private void AdvanceToNextMap()
    {
        if (_phaseSequence.Count == 0) return;

        _cyclePosition = (_cyclePosition + 1) % _phaseSequence.Count;
        _requestedPhase = _phaseSequence[_cyclePosition];
        _damageTakenThisMap = 0f;
        _mapElapsed = 0f;

        if (_transition == null && _requestedPhase != _activePhaseIndex)
            _transition = StartCoroutine(Transition());
    }

    private float GetCurrentDamageThreshold()
    {
        int mapNumber = _requestedPhase + 1;
        return mapNumber == 1 || mapNumber == 2
            ? mapOneAndTwoDamageToAdvance
            : normalMapDamageToAdvance;
    }

    private IEnumerator Transition()
    {
        // Finish the current visual beat before responding to another damage burst.
        while (_requestedPhase != _activePhaseIndex)
        {
            int outgoing = _activePhaseIndex;
            int incoming = _requestedPhase;
            ClearRopeBridgesForPhase(outgoing);
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
            if (i != phase) ClearRopeBridgesForPhase(i);
            Paint(i, 0f, 1f);
            _resolvedTilemaps[i].gameObject.SetActive(i == phase);
            SetCollision(i, true);
        }
        _activePhaseIndex = phase;
    }

    private void ClearRopeBridgesForPhase(int phase)
    {
        if (phase < 0 || phase >= _resolvedTilemaps.Count) return;

        Transform phaseRoot = _resolvedTilemaps[phase].transform;
        RopeBridge[] bridges = FindObjectsByType<RopeBridge>(FindObjectsInactive.Include);
        for (int i = 0; i < bridges.Length; i++)
        {
            RopeBridge bridge = bridges[i];
            if (bridge == null) continue;

            GameObject start = bridge.StartObj;
            GameObject end = bridge.EndObj;
            if ((start != null && start.transform.IsChildOf(phaseRoot)) ||
                (end != null && end.transform.IsChildOf(phaseRoot)))
            {
                Destroy(bridge.gameObject);
            }
        }
    }

    private void OnValidate()
    {
        normalMapDamageToAdvance = Mathf.Max(0.1f, normalMapDamageToAdvance);
        mapOneAndTwoDamageToAdvance = Mathf.Max(0.1f, mapOneAndTwoDamageToAdvance);
        automaticAdvanceSeconds = Mathf.Max(0.1f, automaticAdvanceSeconds);
    }
}
