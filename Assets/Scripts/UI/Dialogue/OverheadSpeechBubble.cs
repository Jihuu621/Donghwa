using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class OverheadSpeechBubble : MonoBehaviour
{
    [Serializable]
    public sealed class SoundRequestEvent : UnityEvent<string> { }

    [Header("Follow")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 worldOffset;
    [SerializeField] private bool faceCamera;

    [Header("Appearance")]
    [SerializeField] private TMP_FontAsset fontAsset;
    [SerializeField] private Font dynamicSourceFont;
    [SerializeField, Min(1f)] private float fontSize = 28f;
    [SerializeField] private Color textColor = new Color(0.97f, 0.95f, 0.87f, 1f);
    [SerializeField] private Color backgroundColor = new Color(0.075f, 0.075f, 0.09f, 0.96f);
    [SerializeField] private Color outlineColor = new Color(0.95f, 0.56f, 0.68f, 0.95f);
    [SerializeField, Min(0.001f)] private float canvasScale = 0.015f;
    private float _scaleMultiplier = 1f;
    [SerializeField] private int sortingOrder = 120;

    [Header("Layout")]
    [SerializeField, Min(20f)] private float minimumWidth = 110f;
    [SerializeField, Min(0f)] private float horizontalPadding = 36f;
    [SerializeField, Min(0f)] private float verticalPadding = 26f;
    [SerializeField, Min(0f)] private float tailHeight = 18f;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float characterDelay = 0.045f;
    [SerializeField, Min(0f)] private float defaultHoldDuration = 1.6f;
    [SerializeField, Min(0f)] private float appearDuration = 0.12f;
    [SerializeField, Min(0f)] private float disappearDuration = 0.14f;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Text Effects")]
    [SerializeField, Min(0f)] private float defaultShakeStrength = 2.2f;
    [SerializeField, Min(0f)] private float defaultWaveStrength = 3f;
    [SerializeField, Min(0f)] private float waveSpeed = 8f;
    [SerializeField, Min(0f)] private float waveSpacing = 0.65f;
    [SerializeField] private SoundRequestEvent soundRequested = new SoundRequestEvent();

    private static TMP_FontAsset _runtimeKoreanFont;
    private static Font _runtimeKoreanSourceFont;
    private static TMP_FontAsset _runtimeProjectFontFallback;

    [Header("Runtime UI References")]
    [SerializeField] private RectTransform _canvasRect;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private SpeechBubbleGraphic _background;
    [SerializeField] private TextMeshProUGUI _text;
    private Coroutine _playRoutine;
    private ParsedDialogue _dialogue;
    private TMP_MeshInfo[] _baseMeshInfo;
    private int _visibleCharacters;
    private int _cueIndex;
    private bool _geometryDirty;

    public bool IsVisible => _canvasRect != null && _canvasRect.gameObject.activeSelf;
    public TMP_Text TextComponent => _text;

    public void SetScaleMultiplier(float multiplier)
    {
        _scaleMultiplier = Mathf.Max(0.1f, multiplier);
    }

    private float EffectiveCanvasScale => canvasScale * _scaleMultiplier;

    private float DeltaTime => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    private float CurrentTime => useUnscaledTime ? Time.unscaledTime : Time.time;

    private void Awake()
    {
        BuildVisuals();
        HideImmediately();
    }

    public void SetTarget(Transform followTarget, Vector3 offset)
    {
        target = followTarget;
        worldOffset = offset;
        UpdateFollowPosition();
    }

    public void Show(string rawDialogue)
    {
        Show(rawDialogue, defaultHoldDuration);
    }

    public void Show(string rawDialogue, float holdDuration)
    {
        BuildVisuals();

        if (_playRoutine != null)
        {
            StopCoroutine(_playRoutine);
            _playRoutine = null;
        }

        _canvasGroup.alpha = 0f;
        ClearRenderedText();
        _dialogue = Parse(rawDialogue ?? string.Empty);
        _visibleCharacters = 0;
        _cueIndex = 0;
        UpdateLayout();

        _canvasRect.localScale = Vector3.one * (EffectiveCanvasScale * 0.82f);
        _canvasRect.gameObject.SetActive(true);
        _text.maxVisibleCharacters = int.MaxValue;
        _geometryDirty = true;
        UpdateFollowPosition();
        _playRoutine = StartCoroutine(PlayRoutine(Mathf.Max(0f, holdDuration)));
    }

    public void Hide(bool immediate = false)
    {
        if (_canvasRect == null || !_canvasRect.gameObject.activeSelf)
        {
            return;
        }

        if (_playRoutine != null)
        {
            StopCoroutine(_playRoutine);
            _playRoutine = null;
        }

        if (immediate)
        {
            HideImmediately();
        }
        else
        {
            _playRoutine = StartCoroutine(HideRoutine());
        }
    }

    private IEnumerator PlayRoutine(float holdDuration)
    {
        yield return AnimateVisibility(0f, 1f, 0.82f, 1f, appearDuration);

        int characterCount = _dialogue.CharacterCount;
        while (_visibleCharacters < characterCount)
        {
            yield return RunCuesAtPosition(_visibleCharacters);

            _visibleCharacters++;
            _text.text = _dialogue.GetRenderedPrefix(_visibleCharacters);
            UpdateLayout();
            _geometryDirty = true;

            float delay = characterDelay;
            if (_visibleCharacters - 1 < _dialogue.Effects.Count)
            {
                delay = _dialogue.Effects[_visibleCharacters - 1].Delay;
            }

            if (delay > 0f)
            {
                yield return WaitForDuration(delay);
            }
        }

        yield return RunCuesAtPosition(characterCount);

        if (holdDuration > 0f)
        {
            yield return WaitForDuration(holdDuration);
            yield return HideRoutine();
        }
        else
        {
            _playRoutine = null;
        }
    }

    private IEnumerator RunCuesAtPosition(int position)
    {
        while (_cueIndex < _dialogue.Cues.Count && _dialogue.Cues[_cueIndex].CharacterIndex <= position)
        {
            DialogueCue cue = _dialogue.Cues[_cueIndex++];
            if (cue.Type == CueType.Sound)
            {
                soundRequested.Invoke(cue.Value);
            }
            else if (cue.Duration > 0f)
            {
                yield return WaitForDuration(cue.Duration);
            }
        }
    }

    private IEnumerator HideRoutine()
    {
        yield return AnimateVisibility(_canvasGroup.alpha, 0f,
            _canvasRect.localScale.x / EffectiveCanvasScale, 0.88f, disappearDuration);
        SetHiddenVisual();
    }

    private IEnumerator AnimateVisibility(float fromAlpha, float toAlpha, float fromScale,
        float toScale, float duration)
    {
        if (duration <= 0f)
        {
            _canvasGroup.alpha = toAlpha;
            _canvasRect.localScale = Vector3.one * (EffectiveCanvasScale * toScale);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += DeltaTime;
            float normalized = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - normalized, 3f);
            _canvasGroup.alpha = Mathf.LerpUnclamped(fromAlpha, toAlpha, eased);
            float scale = Mathf.LerpUnclamped(fromScale, toScale, eased);
            _canvasRect.localScale = Vector3.one * (EffectiveCanvasScale * scale);
            yield return null;
        }

        _canvasGroup.alpha = toAlpha;
        _canvasRect.localScale = Vector3.one * (EffectiveCanvasScale * toScale);
    }

    private IEnumerator WaitForDuration(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += DeltaTime;
            yield return null;
        }
    }

    private void LateUpdate()
    {
        UpdateFollowPosition();

        if (!IsVisible || _dialogue == null)
        {
            return;
        }

        if (!_dialogue.HasAnimatedEffects)
        {
            return;
        }

        if (_geometryDirty)
        {
            RefreshBaseGeometry();
        }

        ApplyCharacterEffects();
    }

    private void UpdateFollowPosition()
    {
        if (target != null)
        {
            transform.position = target.position + worldOffset;
        }

        if (faceCamera && Camera.main != null)
        {
            transform.rotation = Camera.main.transform.rotation;
        }
        else
        {
            transform.rotation = Quaternion.identity;
        }
    }

    private void RefreshBaseGeometry()
    {
        _text.ForceMeshUpdate(true);
        _baseMeshInfo = _text.textInfo.CopyMeshInfoVertexData();
        _geometryDirty = false;
    }

    private void ApplyCharacterEffects()
    {
        if (_baseMeshInfo == null)
        {
            return;
        }

        TMP_TextInfo textInfo = _text.textInfo;
        for (int materialIndex = 0; materialIndex < textInfo.meshInfo.Length; materialIndex++)
        {
            Vector3[] source = _baseMeshInfo[materialIndex].vertices;
            Vector3[] destination = textInfo.meshInfo[materialIndex].vertices;
            Array.Copy(source, destination, Mathf.Min(source.Length, destination.Length));
        }

        int count = Mathf.Min(Mathf.Min(textInfo.characterCount, _dialogue.Effects.Count), _visibleCharacters);
        float now = CurrentTime;
        for (int i = 0; i < count; i++)
        {
            TMP_CharacterInfo character = textInfo.characterInfo[i];
            if (!character.isVisible)
            {
                continue;
            }

            CharacterEffect effect = _dialogue.Effects[i];
            Vector3 offset = Vector3.zero;
            if (effect.Shake > 0f)
            {
                float x = Mathf.PerlinNoise(i * 1.37f, now * 24f) * 2f - 1f;
                float y = Mathf.PerlinNoise(i * 2.11f + 17f, now * 27f) * 2f - 1f;
                offset += new Vector3(x, y, 0f) * effect.Shake;
            }

            if (effect.Wave > 0f)
            {
                offset.y += Mathf.Sin(now * waveSpeed + i * waveSpacing) * effect.Wave;
            }

            if (offset == Vector3.zero)
            {
                continue;
            }

            int materialIndex = character.materialReferenceIndex;
            int vertexIndex = character.vertexIndex;
            Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;
            for (int vertex = 0; vertex < 4; vertex++)
            {
                vertices[vertexIndex + vertex] += offset;
            }
        }

        _text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
    }

    private void BuildVisuals()
    {
        if (_canvasRect != null && _canvasGroup != null && _background != null && _text != null)
        {
            ApplyVisualSettings();
            return;
        }

        GameObject canvasObject = new GameObject("World Speech Bubble", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasGroup));
        canvasObject.layer = gameObject.layer;
        canvasObject.transform.SetParent(transform, false);
        _canvasRect = canvasObject.GetComponent<RectTransform>();
        _canvasRect.pivot = new Vector2(0.5f, 0f);
        _canvasRect.localScale = Vector3.one * EffectiveCanvasScale;

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;
        _canvasGroup = canvasObject.GetComponent<CanvasGroup>();
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;

        GameObject backgroundObject = new GameObject("Bubble And Tail", typeof(RectTransform),
            typeof(SpeechBubbleGraphic), typeof(Outline));
        backgroundObject.layer = gameObject.layer;
        backgroundObject.transform.SetParent(canvasObject.transform, false);
        RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;
        _background = backgroundObject.GetComponent<SpeechBubbleGraphic>();
        _background.color = backgroundColor;
        _background.TailHeight = tailHeight;

        Outline outline = backgroundObject.GetComponent<Outline>();
        outline.effectColor = outlineColor;
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        outline.useGraphicAlpha = true;

        GameObject textObject = new GameObject("Dialogue Text", typeof(RectTransform),
            typeof(TextMeshProUGUI));
        textObject.layer = gameObject.layer;
        textObject.transform.SetParent(canvasObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = new Vector2(0f, tailHeight * 0.5f);
        textRect.sizeDelta = new Vector2(1f, fontSize);

        _text = textObject.GetComponent<TextMeshProUGUI>();
        _text.raycastTarget = false;
        _text.richText = true;
        _text.enableAutoSizing = false;
        _text.fontSize = fontSize;
        _text.color = textColor;
        _text.alignment = TextAlignmentOptions.TopLeft;
        _text.textWrappingMode = TextWrappingModes.NoWrap;
        _text.overflowMode = TextOverflowModes.Overflow;
        _text.font = ResolveFontAsset();

        ApplyVisualSettings();
    }

    private void ApplyVisualSettings()
    {
        _canvasRect.localScale = Vector3.one * EffectiveCanvasScale;

        Canvas canvas = _canvasRect.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;
        }

        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
        _background.color = backgroundColor;
        _background.TailHeight = tailHeight;

        Outline outline = _background.GetComponent<Outline>();
        if (outline != null)
        {
            outline.effectColor = outlineColor;
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            outline.useGraphicAlpha = true;
        }

        _text.raycastTarget = false;
        _text.richText = true;
        _text.enableAutoSizing = false;
        _text.fontSize = fontSize;
        _text.color = textColor;
        _text.alignment = TextAlignmentOptions.TopLeft;
        _text.textWrappingMode = TextWrappingModes.NoWrap;
        _text.overflowMode = TextOverflowModes.Overflow;
        _text.font = ResolveFontAsset();

        RectTransform textRect = _text.rectTransform;
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = new Vector2(0f, tailHeight * 0.5f);
    }

    private TMP_FontAsset ResolveFontAsset()
    {
        if (fontAsset != null)
        {
            AddDynamicSourceFallback(fontAsset);
            return fontAsset;
        }

        if (_runtimeKoreanFont != null)
        {
            return _runtimeKoreanFont;
        }

        string[,] systemFontFaces =
        {
            { "Noto Sans KR", "Regular" },
            { "맑은 고딕", "Regular" },
            { "Malgun Gothic", "Regular" },
            { "Apple SD Gothic Neo", "Regular" },
            { "NanumGothic", "Regular" }
        };

        for (int i = 0; i < systemFontFaces.GetLength(0); i++)
        {
            string family = systemFontFaces[i, 0];
            string style = systemFontFaces[i, 1];
            TMP_FontAsset dynamicFont = TMP_FontAsset.CreateFontAsset(family, style, 64);
            if (dynamicFont != null && dynamicFont.HasCharacter('가', false, true))
            {
                _runtimeKoreanFont = dynamicFont;
                _runtimeKoreanFont.name = "Runtime Korean Dialogue Font";
                _runtimeKoreanFont.hideFlags = HideFlags.HideAndDontSave;
                return _runtimeKoreanFont;
            }
        }

        string[] installedFonts = Font.GetOSInstalledFontNames();
        string[] preferredFonts =
        {
            "Malgun Gothic", "맑은 고딕", "Noto Sans CJK KR", "Noto Sans KR",
            "Apple SD Gothic Neo", "NanumGothic", "Arial Unicode MS"
        };

        for (int preferredIndex = 0; preferredIndex < preferredFonts.Length; preferredIndex++)
        {
            for (int installedIndex = 0; installedIndex < installedFonts.Length; installedIndex++)
            {
                if (!installedFonts[installedIndex].Equals(preferredFonts[preferredIndex],
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                _runtimeKoreanSourceFont = Font.CreateDynamicFontFromOSFont(installedFonts[installedIndex], 64);
                if (_runtimeKoreanSourceFont == null)
                {
                    continue;
                }

                _runtimeKoreanSourceFont.hideFlags = HideFlags.HideAndDontSave;
                _runtimeKoreanFont = TMP_FontAsset.CreateFontAsset(_runtimeKoreanSourceFont);
                if (_runtimeKoreanFont != null)
                {
                    _runtimeKoreanFont.name = "Runtime Korean Dialogue Font";
                    _runtimeKoreanFont.hideFlags = HideFlags.HideAndDontSave;
                    return _runtimeKoreanFont;
                }
            }
        }

        return TMP_Settings.defaultFontAsset;
    }

    private void AddDynamicSourceFallback(TMP_FontAsset primaryFont)
    {
        if (dynamicSourceFont == null)
        {
            return;
        }

        if (_runtimeProjectFontFallback == null)
        {
            _runtimeProjectFontFallback = TMP_FontAsset.CreateFontAsset(dynamicSourceFont);
            if (_runtimeProjectFontFallback == null)
            {
                return;
            }

            _runtimeProjectFontFallback.name = $"{dynamicSourceFont.name} Runtime Fallback";
            _runtimeProjectFontFallback.hideFlags = HideFlags.HideAndDontSave;
            _runtimeProjectFontFallback.HasCharacter('가', false, true);
        }

        if (primaryFont.fallbackFontAssetTable == null)
        {
            primaryFont.fallbackFontAssetTable = new List<TMP_FontAsset>();
        }

        if (!primaryFont.fallbackFontAssetTable.Contains(_runtimeProjectFontFallback))
        {
            primaryFont.fallbackFontAssetTable.Add(_runtimeProjectFontFallback);
        }
    }

    private void UpdateLayout()
    {
        _text.enableAutoSizing = false;
        _text.fontSize = fontSize;

        Vector2 currentSize = _canvasRect.sizeDelta;
        if (_visibleCharacters <= 0)
        {
            float minimumHeight = fontSize + verticalPadding + tailHeight;
            _canvasRect.sizeDelta = new Vector2(minimumWidth, minimumHeight);
            _text.rectTransform.sizeDelta = new Vector2(1f, fontSize);
            _text.ForceMeshUpdate(true);
            return;
        }

        // No automatic wrapping: each revealed character extends the bubble horizontally.
        _canvasRect.sizeDelta = new Vector2(Mathf.Max(currentSize.x, minimumWidth),
            Mathf.Max(currentSize.y, fontSize + verticalPadding + tailHeight));
        _text.ForceMeshUpdate(true);
        Vector2 visibleSize = GetVisibleTextSize(_text.textInfo.characterCount);
        Vector2 preferredSize = _text.GetPreferredValues(_text.text, 100000f, 100000f);
        float textWidth = Mathf.Max(visibleSize.x, preferredSize.x);
        float textHeight = Mathf.Max(fontSize, visibleSize.y, preferredSize.y);
        float width = Mathf.Max(minimumWidth, textWidth + horizontalPadding);
        float height = textHeight + verticalPadding + tailHeight;
        _canvasRect.sizeDelta = new Vector2(width, height);
        _text.rectTransform.sizeDelta = new Vector2(
            Mathf.Max(1f, textWidth + 1f),
            textHeight);
        _text.rectTransform.anchoredPosition = new Vector2(0f, tailHeight * 0.5f);
        _text.ForceMeshUpdate(true);
        _geometryDirty = true;
    }

    private Vector2 GetVisibleTextSize(int visibleCount)
    {
        TMP_TextInfo textInfo = _text.textInfo;
        int count = Mathf.Min(visibleCount, textInfo.characterCount);
        float minX = float.PositiveInfinity;
        float minY = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float maxY = float.NegativeInfinity;

        for (int i = 0; i < count; i++)
        {
            TMP_CharacterInfo character = textInfo.characterInfo[i];
            if (!character.isVisible)
            {
                continue;
            }

            minX = Mathf.Min(minX, character.origin);
            minY = Mathf.Min(minY, character.descender);
            maxX = Mathf.Max(maxX, character.xAdvance);
            maxY = Mathf.Max(maxY, character.ascender);
        }

        if (float.IsPositiveInfinity(minX))
        {
            return Vector2.zero;
        }

        return new Vector2(maxX - minX, maxY - minY);
    }

    private void HideImmediately()
    {
        if (_playRoutine != null)
        {
            StopCoroutine(_playRoutine);
            _playRoutine = null;
        }

        SetHiddenVisual();
    }

    private void SetHiddenVisual()
    {
        _playRoutine = null;
        if (_canvasRect != null)
        {
            _canvasGroup.alpha = 0f;
            ClearRenderedText();
            _dialogue = null;
            _visibleCharacters = 0;
            _cueIndex = 0;
            _baseMeshInfo = null;
            _geometryDirty = false;
            _canvasRect.localScale = Vector3.one * EffectiveCanvasScale;
            _canvasRect.gameObject.SetActive(false);
        }
    }

    private void ClearRenderedText()
    {
        if (_text == null) return;

        _text.text = string.Empty;
        _text.maxVisibleCharacters = 0;
        _text.ForceMeshUpdate(true, true);
        _text.canvasRenderer.Clear();
    }

    private ParsedDialogue Parse(string rawDialogue)
    {
        StringBuilder rendered = new StringBuilder(rawDialogue.Length);
        List<CharacterEffect> effects = new List<CharacterEffect>(rawDialogue.Length);
        List<int> renderedEndIndices = new List<int>(rawDialogue.Length);
        List<DialogueCue> cues = new List<DialogueCue>();
        Stack<float> speedStack = new Stack<float>();
        Stack<float> shakeStack = new Stack<float>();
        Stack<float> waveStack = new Stack<float>();
        float activeDelay = characterDelay;
        float activeShake = 0f;
        float activeWave = 0f;

        int index = 0;
        while (index < rawDialogue.Length)
        {
            char opening = rawDialogue[index];
            char closing = opening == '<' ? '>' : opening == '[' ? ']' : '\0';
            if (closing != '\0')
            {
                int end = rawDialogue.IndexOf(closing, index + 1);
                if (end >= 0)
                {
                    string tag = rawDialogue.Substring(index + 1, end - index - 1).Trim();
                    if (TryHandleCustomTag(tag, effects.Count, cues,
                            speedStack, shakeStack, waveStack,
                            ref activeDelay, ref activeShake, ref activeWave))
                    {
                        index = end + 1;
                        continue;
                    }

                    if (opening == '<')
                    {
                        if (tag.Equals("br", StringComparison.OrdinalIgnoreCase) ||
                            tag.Equals("br/", StringComparison.OrdinalIgnoreCase))
                        {
                            rendered.Append('\n');
                            effects.Add(new CharacterEffect(activeDelay, activeShake, activeWave));
                            renderedEndIndices.Add(rendered.Length);
                        }
                        else
                        {
                            rendered.Append(rawDialogue, index, end - index + 1);
                        }

                        index = end + 1;
                        continue;
                    }
                }
            }

            rendered.Append(rawDialogue[index]);
            effects.Add(new CharacterEffect(activeDelay, activeShake, activeWave));
            renderedEndIndices.Add(rendered.Length);
            index++;
        }

        return new ParsedDialogue(rendered.ToString(), renderedEndIndices, effects, cues);
    }

    private bool TryHandleCustomTag(string rawTag, int characterIndex, List<DialogueCue> cues,
        Stack<float> speedStack, Stack<float> shakeStack, Stack<float> waveStack,
        ref float activeDelay, ref float activeShake, ref float activeWave)
    {
        bool closingTag = rawTag.StartsWith("/", StringComparison.Ordinal);
        string tag = closingTag ? rawTag.Substring(1).Trim() : rawTag;
        int separator = tag.IndexOf('=');
        string name = (separator >= 0 ? tag.Substring(0, separator) : tag).Trim().ToLowerInvariant();
        string value = separator >= 0 ? tag.Substring(separator + 1).Trim().Trim('"', '\'') : string.Empty;

        switch (name)
        {
            case "speed":
                if (closingTag)
                {
                    activeDelay = speedStack.Count > 0 ? speedStack.Pop() : characterDelay;
                }
                else
                {
                    speedStack.Push(activeDelay);
                    activeDelay = ParseFloat(value, characterDelay);
                }
                return true;

            case "shake":
                if (closingTag)
                {
                    activeShake = shakeStack.Count > 0 ? shakeStack.Pop() : 0f;
                }
                else
                {
                    shakeStack.Push(activeShake);
                    activeShake = ParseFloat(value, defaultShakeStrength);
                }
                return true;

            case "wave":
                if (closingTag)
                {
                    activeWave = waveStack.Count > 0 ? waveStack.Pop() : 0f;
                }
                else
                {
                    waveStack.Push(activeWave);
                    activeWave = ParseFloat(value, defaultWaveStrength);
                }
                return true;

            case "wait":
            case "pause":
                if (!closingTag)
                {
                    cues.Add(DialogueCue.Wait(characterIndex, ParseFloat(value, 0.25f)));
                }
                return true;

            case "sound":
            case "sfx":
                if (!closingTag && !string.IsNullOrWhiteSpace(value))
                {
                    cues.Add(DialogueCue.Sound(characterIndex, value));
                }
                return true;

            default:
                return false;
        }
    }

    private static float ParseFloat(string value, float fallback)
    {
        return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed)
            ? Mathf.Max(0f, parsed)
            : fallback;
    }

    private sealed class ParsedDialogue
    {
        public readonly string RenderedText;
        public readonly List<int> RenderedEndIndices;
        public readonly List<CharacterEffect> Effects;
        public readonly List<DialogueCue> Cues;
        public readonly bool HasAnimatedEffects;
        public int CharacterCount => Effects.Count;

        public ParsedDialogue(string renderedText, List<int> renderedEndIndices,
            List<CharacterEffect> effects, List<DialogueCue> cues)
        {
            RenderedText = renderedText;
            RenderedEndIndices = renderedEndIndices;
            Effects = effects;
            Cues = cues;
            for (int i = 0; i < effects.Count; i++)
            {
                if (effects[i].Shake > 0f || effects[i].Wave > 0f)
                {
                    HasAnimatedEffects = true;
                    break;
                }
            }
        }

        public string GetRenderedPrefix(int visibleCharacterCount)
        {
            if (visibleCharacterCount <= 0 || RenderedEndIndices.Count == 0)
            {
                return string.Empty;
            }

            if (visibleCharacterCount >= RenderedEndIndices.Count)
            {
                return RenderedText;
            }

            return RenderedText.Substring(0, RenderedEndIndices[visibleCharacterCount - 1]);
        }
    }

    private readonly struct CharacterEffect
    {
        public readonly float Delay;
        public readonly float Shake;
        public readonly float Wave;

        public CharacterEffect(float delay, float shake, float wave)
        {
            Delay = delay;
            Shake = shake;
            Wave = wave;
        }
    }

    private enum CueType
    {
        Wait,
        Sound
    }

    private readonly struct DialogueCue
    {
        public readonly CueType Type;
        public readonly int CharacterIndex;
        public readonly float Duration;
        public readonly string Value;

        private DialogueCue(CueType type, int characterIndex, float duration, string value)
        {
            Type = type;
            CharacterIndex = characterIndex;
            Duration = duration;
            Value = value;
        }

        public static DialogueCue Wait(int characterIndex, float duration)
        {
            return new DialogueCue(CueType.Wait, characterIndex, duration, string.Empty);
        }

        public static DialogueCue Sound(int characterIndex, string value)
        {
            return new DialogueCue(CueType.Sound, characterIndex, 0f, value);
        }
    }
}
