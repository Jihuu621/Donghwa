using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class CheshireCatBossHealthBar : MonoBehaviour
{
    [Header("Health Source")]
    [SerializeField] private CheshireCatHealth targetHealth;
    [SerializeField] private bool findCheshireCatAutomatically = true;

    [Header("View")]
    [SerializeField] private Image currentFill;
    [SerializeField] private Image delayedFill;
    [SerializeField] private TMP_Text bossNameText;
    [SerializeField] private TMP_Text healthText;
    [SerializeField, Min(0f)] private float delayedFillLag = 0.35f;

    private float displayedHealth = 1f;
    private Coroutine delayedFillRoutine;
    private Graphic[] graphics;
    private bool isSubscribed;
    private float currentFillWidth;
    private float delayedFillWidth;

    private void OnEnable()
    {
        graphics = GetComponentsInChildren<Graphic>(true);
        currentFillWidth = GetFillWidth(currentFill);
        delayedFillWidth = GetFillWidth(delayedFill);
        SetViewVisible(false);
        ResolveHealth();
        Subscribe();
    }

    private void Start()
    {
        ResolveHealth();
        if (targetHealth != null)
        {
            Subscribe();
            Refresh(targetHealth.CurrentHP, targetHealth.MaxHP, true);
        }
    }

    private void Update()
    {
        if (targetHealth != null) return;

        isSubscribed = false;
        ResolveHealth();
        if (targetHealth == null) return;

        Subscribe();
        Refresh(targetHealth.CurrentHP, targetHealth.MaxHP, true);
    }

    private void OnDisable()
    {
        Unsubscribe();
        if (delayedFillRoutine != null) StopCoroutine(delayedFillRoutine);
        delayedFillRoutine = null;
    }

    private void ResolveHealth()
    {
        if (targetHealth == null && findCheshireCatAutomatically)
        {
            targetHealth = FindAnyObjectByType<CheshireCatHealth>();
        }
    }

    private void Subscribe()
    {
        if (targetHealth == null || isSubscribed) return;
        targetHealth.OnHealthChanged -= HandleHealthChanged;
        targetHealth.OnHealthChanged += HandleHealthChanged;
        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (targetHealth != null && isSubscribed) targetHealth.OnHealthChanged -= HandleHealthChanged;
        isSubscribed = false;
    }

    private void HandleHealthChanged(float current, float maximum)
    {
        Refresh(current, maximum, false);
    }

    private void Refresh(float current, float maximum, bool immediate)
    {
        SetViewVisible(true);
        float normalized = maximum <= 0f ? 0f : Mathf.Clamp01(current / maximum);
        SetFillWidth(currentFill, currentFillWidth, normalized);
        if (healthText != null) healthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(maximum)}";

        if (delayedFill == null) return;
        if (delayedFillRoutine != null) StopCoroutine(delayedFillRoutine);

        if (immediate)
        {
            displayedHealth = normalized;
            SetFillWidth(delayedFill, delayedFillWidth, normalized);
            return;
        }

        delayedFillRoutine = StartCoroutine(AnimateDelayedFill(normalized));
    }

    private void SetViewVisible(bool visible)
    {
        if (graphics == null) return;
        foreach (Graphic graphic in graphics)
        {
            if (graphic != null) graphic.enabled = visible;
        }
    }

    private IEnumerator AnimateDelayedFill(float target)
    {
        float start = displayedHealth;
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, delayedFillLag);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            displayedHealth = Mathf.Lerp(start, target, elapsed / duration);
            SetFillWidth(delayedFill, delayedFillWidth, displayedHealth);
            yield return null;
        }

        displayedHealth = target;
        SetFillWidth(delayedFill, delayedFillWidth, target);
        delayedFillRoutine = null;
    }

    private static float GetFillWidth(Image image)
    {
        return image != null ? image.rectTransform.rect.width : 0f;
    }

    private static void SetFillWidth(Image image, float fullWidth, float normalized)
    {
        if (image == null) return;
        image.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, fullWidth * Mathf.Clamp01(normalized));
    }
}
