using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class YarnHealthDisplay : MonoBehaviour
{
    public const int CurrentViewVersion = 6;

    [Header("Health Source")]
    [SerializeField] private Health targetHealth;
    [SerializeField] private bool findPlayerAutomatically = true;

    [Header("Yarn Capacity")]
    [SerializeField, Min(1)] private int yarnCount = 3;

    [Header("View")]
    [SerializeField] private RectTransform slotContainer;
    [SerializeField] private Sprite fullYarnSprite;
    [SerializeField] private Sprite halfYarnSprite;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private Image damageFlash;
    [SerializeField] private Vector2 slotSize = new Vector2(112f, 112f);

    [Header("Animation")]
    [SerializeField] private bool animateChanges = true;
    [SerializeField, Min(0.01f)] private float halfStepDuration = 0.16f;
    [SerializeField, Min(0.05f)] private float maximumSequenceDuration = 0.65f;
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField, HideInInspector] private int viewVersion;

    private readonly List<YarnHealthSlotView> slots = new List<YarnHealthSlotView>();
    private Health subscribedHealth;
    private Coroutine transitionRoutine;
    private Coroutine feedbackRoutine;
    private int displayedHalfUnits;
    private int targetHalfUnits;
    private float lastHealth;
    private bool initialized;
    private bool started;
    private Vector3 restingScale = Vector3.one;

    public Health TargetHealth => targetHealth;
    public int YarnCount => Mathf.Max(1, yarnCount);
    public int TotalHalfUnits => YarnCount * 2;
    public int DisplayedHalfUnits => displayedHalfUnits;
    public int ViewVersion => viewVersion;

    private void Awake()
    {
        restingScale = transform.localScale;
        EnsureSlots();
    }

    private void OnEnable()
    {
        ResolveTargetHealth();
        Subscribe();

        if (started && targetHealth != null)
        {
            SetHealthImmediate(targetHealth.CurrentHP, targetHealth.MaxHP);
        }
    }

    private void Start()
    {
        started = true;
        ResolveTargetHealth();
        EnsureSlots();
        Subscribe();

        if (targetHealth != null)
        {
            SetHealthImmediate(targetHealth.CurrentHP, targetHealth.MaxHP);
        }
        else
        {
            SetHealthImmediate(1f, 1f);
        }
    }

    private void OnDisable()
    {
        Unsubscribe();

        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        if (feedbackRoutine != null)
        {
            StopCoroutine(feedbackRoutine);
            feedbackRoutine = null;
        }

        transform.localScale = restingScale;
        SetFlashAlpha(0f);
        ResetAllSlotPoses();
        ApplyAllStates(displayedHalfUnits);
    }

    private void OnValidate()
    {
        yarnCount = Mathf.Max(1, yarnCount);
        halfStepDuration = Mathf.Max(0.01f, halfStepDuration);
        maximumSequenceDuration = Mathf.Max(0.05f, maximumSequenceDuration);
        slotSize.x = Mathf.Max(1f, slotSize.x);
        slotSize.y = Mathf.Max(1f, slotSize.y);
    }

    public void Configure(
        Health health,
        RectTransform container,
        Sprite fullSprite,
        Sprite halfSprite,
        TMP_Text statusLabel,
        Image flashImage,
        int numberOfYarns)
    {
        slotContainer = container;
        fullYarnSprite = fullSprite;
        halfYarnSprite = halfSprite;
        healthText = statusLabel;
        damageFlash = flashImage;
        yarnCount = Mathf.Max(1, numberOfYarns);
        viewVersion = CurrentViewVersion;

        RebuildSlots();

        if (Application.isPlaying)
        {
            Bind(health);
        }
        else
        {
            targetHealth = health;
        }
    }

    public void Bind(Health health)
    {
        if (targetHealth == health && subscribedHealth == health)
        {
            if (Application.isPlaying && health != null)
            {
                SetHealthImmediate(health.CurrentHP, health.MaxHP);
            }

            return;
        }

        Unsubscribe();
        targetHealth = health;
        Subscribe();

        if (Application.isPlaying && targetHealth != null)
        {
            SetHealthImmediate(targetHealth.CurrentHP, targetHealth.MaxHP);
        }
    }

    [ContextMenu("Rebuild Yarn Slots")]
    public void RebuildSlots()
    {
        if (Application.isPlaying)
        {
            CancelTransition();
        }

        if (slotContainer == null)
        {
            slotContainer = transform as RectTransform;
        }

        YarnHealthSlotView[] oldSlots = slotContainer.GetComponentsInChildren<YarnHealthSlotView>(true);
        for (int i = 0; i < oldSlots.Length; i++)
        {
            GameObject oldObject = oldSlots[i].gameObject;
            if (Application.isPlaying)
            {
                oldObject.SetActive(false);
                Destroy(oldObject);
            }
            else
            {
                DestroyImmediate(oldObject);
            }
        }

        slots.Clear();

        for (int i = 0; i < YarnCount; i++)
        {
            GameObject slotObject = new GameObject(
                $"YarnSlot_{i + 1}",
                typeof(RectTransform),
                typeof(LayoutElement),
                typeof(YarnHealthSlotView));

            slotObject.layer = slotContainer.gameObject.layer;
            RectTransform slotRect = slotObject.GetComponent<RectTransform>();
            slotRect.SetParent(slotContainer, false);
            slotRect.sizeDelta = slotSize;

            LayoutElement layoutElement = slotObject.GetComponent<LayoutElement>();
            layoutElement.preferredWidth = slotSize.x;
            layoutElement.preferredHeight = slotSize.y;
            layoutElement.minWidth = slotSize.x;
            layoutElement.minHeight = slotSize.y;
            layoutElement.flexibleWidth = 0f;
            layoutElement.flexibleHeight = 0f;

            GameObject mainObject = new GameObject(
                "YarnMain",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));

            mainObject.layer = slotObject.layer;
            RectTransform mainRect = mainObject.GetComponent<RectTransform>();
            mainRect.SetParent(slotRect, false);
            mainRect.anchorMin = Vector2.zero;
            mainRect.anchorMax = Vector2.one;
            mainRect.offsetMin = Vector2.zero;
            mainRect.offsetMax = Vector2.zero;

            Image mainImage = mainObject.GetComponent<Image>();
            mainImage.raycastTarget = false;
            mainImage.preserveAspect = true;

            GameObject ghostObject = new GameObject(
                "UnravelGhost",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));

            ghostObject.layer = slotObject.layer;
            RectTransform ghostRect = ghostObject.GetComponent<RectTransform>();
            ghostRect.SetParent(slotRect, false);
            ghostRect.anchorMin = Vector2.zero;
            ghostRect.anchorMax = Vector2.one;
            ghostRect.offsetMin = Vector2.zero;
            ghostRect.offsetMax = Vector2.zero;

            Image ghostImage = ghostObject.GetComponent<Image>();
            ghostImage.raycastTarget = false;
            ghostImage.preserveAspect = true;
            ghostImage.color = Color.clear;

            YarnHealthSlotView slot = slotObject.GetComponent<YarnHealthSlotView>();
            slot.Configure(i, mainImage, ghostImage);
            slots.Add(slot);
        }

        int previewUnits = Application.isPlaying && initialized ? displayedHalfUnits : TotalHalfUnits;
        ApplyAllStates(previewUnits);
    }

    public void SetHealthImmediate(float currentHealth, float maximumHealth)
    {
        CancelTransition();
        EnsureSlots();

        displayedHalfUnits = CalculateVisibleHalfUnits(currentHealth, maximumHealth, YarnCount);
        targetHalfUnits = displayedHalfUnits;
        lastHealth = Mathf.Clamp(currentHealth, 0f, Mathf.Max(0f, maximumHealth));
        initialized = true;

        ApplyAllStates(displayedHalfUnits);
        UpdateHealthText(currentHealth, maximumHealth);
    }

    public static int CalculateVisibleHalfUnits(float currentHealth, float maximumHealth, int numberOfYarns)
    {
        int safeYarnCount = Mathf.Max(1, numberOfYarns);
        int totalUnits = safeYarnCount * 2;

        if (maximumHealth <= 0f || currentHealth <= 0f)
        {
            return 0;
        }

        float normalizedHealth = Mathf.Clamp01(currentHealth / maximumHealth);
        float scaledUnits = normalizedHealth * totalUnits;

        // The small epsilon keeps exact boundaries stable (for example, 4.0 remains 4).
        return Mathf.Clamp(Mathf.CeilToInt(scaledUnits - 0.0001f), 1, totalUnits);
    }

    private void ResolveTargetHealth()
    {
        if (targetHealth != null || !findPlayerAutomatically)
        {
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            targetHealth = player.GetComponent<Health>();
            if (targetHealth == null)
            {
                targetHealth = player.GetComponentInChildren<Health>(true);
            }
        }
    }

    private void Subscribe()
    {
        if (!Application.isPlaying || targetHealth == null || subscribedHealth == targetHealth)
        {
            return;
        }

        Unsubscribe();
        subscribedHealth = targetHealth;
        subscribedHealth.OnHealthChanged += HandleHealthChanged;
    }

    private void Unsubscribe()
    {
        if (subscribedHealth == null)
        {
            return;
        }

        subscribedHealth.OnHealthChanged -= HandleHealthChanged;
        subscribedHealth = null;
    }

    private void HandleHealthChanged(float currentHealth, float maximumHealth)
    {
        EnsureSlots();
        UpdateHealthText(currentHealth, maximumHealth);

        int nextTarget = CalculateVisibleHalfUnits(currentHealth, maximumHealth, YarnCount);
        float clampedHealth = Mathf.Clamp(currentHealth, 0f, Mathf.Max(0f, maximumHealth));
        bool tookDamage = initialized && clampedHealth < lastHealth - 0.0001f;
        float damageFraction = maximumHealth > 0f ? Mathf.Max(0f, lastHealth - clampedHealth) / maximumHealth : 0f;

        lastHealth = clampedHealth;
        targetHalfUnits = nextTarget;

        if (!initialized || !animateChanges || !Application.isPlaying)
        {
            displayedHalfUnits = targetHalfUnits;
            initialized = true;
            ApplyAllStates(displayedHalfUnits);
            return;
        }

        if (tookDamage)
        {
            StartDamageFeedback(damageFraction);
        }

        if (displayedHalfUnits != targetHalfUnits && transitionRoutine == null)
        {
            transitionRoutine = StartCoroutine(AnimateTowardLatestTarget());
        }
    }

    private IEnumerator AnimateTowardLatestTarget()
    {
        float remainingSequenceBudget = maximumSequenceDuration;

        while (displayedHalfUnits != targetHalfUnits)
        {
            int direction = targetHalfUnits > displayedHalfUnits ? 1 : -1;
            int fromUnits = displayedHalfUnits;
            int toUnits = displayedHalfUnits + direction;
            int remainingSteps = Mathf.Max(1, Mathf.Abs(targetHalfUnits - displayedHalfUnits));
            float duration = Mathf.Min(
                halfStepDuration,
                Mathf.Max(0f, remainingSequenceBudget / remainingSteps));

            if (direction < 0)
            {
                yield return AnimateDamageStep(fromUnits, toUnits, duration);
            }
            else
            {
                yield return AnimateHealStep(fromUnits, toUnits, duration);
            }

            displayedHalfUnits = toUnits;
            ApplyAllStates(displayedHalfUnits);
            remainingSequenceBudget = Mathf.Max(0f, remainingSequenceBudget - duration);
        }

        transitionRoutine = null;
    }

    private IEnumerator AnimateDamageStep(int fromUnits, int toUnits, float duration)
    {
        int slotIndex = Mathf.Clamp((fromUnits - 1) / 2, 0, slots.Count - 1);
        YarnHealthSlotView slot = slots[slotIndex];
        int fromState = GetSlotState(fromUnits, slotIndex);
        int toState = GetSlotState(toUnits, slotIndex);
        Sprite fromSprite = GetStateSprite(fromState);
        Sprite toSprite = GetStateSprite(toState);

        slot.PrepareTransition(fromSprite, toSprite != null ? toSprite : fromSprite);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += GetDeltaTime();
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = EaseOutCubic(t);

            slot.Root.localRotation = Quaternion.Euler(0f, 0f, -10f * Mathf.Sin(t * Mathf.PI));
            slot.Root.localScale = Vector3.one * Mathf.Lerp(1f, 0.92f, Mathf.Sin(t * Mathf.PI));

            slot.GhostRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, -115f, eased));
            slot.GhostRect.localScale = Vector3.one * Mathf.Lerp(1f, toState == 0 ? 0.18f : 1.28f, eased);
            slot.GhostRect.anchoredPosition = new Vector2(Mathf.Lerp(0f, 14f, eased), 0f);
            slot.SetGhostAlpha(1f - eased);

            slot.MainRect.localScale = Vector3.one * Mathf.Lerp(0.76f, 1f, EaseOutBack(t));
            slot.SetMainAlpha(toState == 0 ? 0f : Mathf.Lerp(0.25f, 1f, eased));

            yield return null;
        }

        slot.SetState(toState, fullYarnSprite, halfYarnSprite);
    }

    private IEnumerator AnimateHealStep(int fromUnits, int toUnits, float duration)
    {
        int slotIndex = Mathf.Clamp((toUnits - 1) / 2, 0, slots.Count - 1);
        YarnHealthSlotView slot = slots[slotIndex];
        int fromState = GetSlotState(fromUnits, slotIndex);
        int toState = GetSlotState(toUnits, slotIndex);
        Sprite fromSprite = GetStateSprite(fromState);
        Sprite toSprite = GetStateSprite(toState);

        slot.PrepareTransition(fromSprite, toSprite);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += GetDeltaTime();
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = EaseOutCubic(t);

            slot.Root.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(18f, 0f, eased));
            slot.Root.localScale = Vector3.one * Mathf.Lerp(0.72f, 1f, EaseOutBack(t));
            slot.MainRect.localScale = Vector3.one;
            slot.SetMainAlpha(eased);

            slot.GhostRect.localScale = Vector3.one * Mathf.Lerp(0.85f, 1.15f, eased);
            slot.SetGhostAlpha(fromSprite == null ? 0f : 1f - eased);

            yield return null;
        }

        slot.SetState(toState, fullYarnSprite, halfYarnSprite);
    }

    private void StartDamageFeedback(float damageFraction)
    {
        if (feedbackRoutine != null)
        {
            StopCoroutine(feedbackRoutine);
        }

        feedbackRoutine = StartCoroutine(PlayDamageFeedback(damageFraction));
    }

    private IEnumerator PlayDamageFeedback(float damageFraction)
    {
        float strength = Mathf.Lerp(0.025f, 0.07f, Mathf.Clamp01(damageFraction * 4f));
        const float duration = 0.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += GetDeltaTime();
            float t = Mathf.Clamp01(elapsed / duration);
            float pulse = Mathf.Sin(t * Mathf.PI);
            transform.localScale = restingScale * (1f + pulse * strength);
            SetFlashAlpha((1f - t) * Mathf.Lerp(0.16f, 0.34f, Mathf.Clamp01(damageFraction * 4f)));
            yield return null;
        }

        transform.localScale = restingScale;
        SetFlashAlpha(0f);
        feedbackRoutine = null;
    }

    private void EnsureSlots()
    {
        if (slotContainer == null)
        {
            slotContainer = transform as RectTransform;
        }

        slots.Clear();
        if (slotContainer != null)
        {
            slots.AddRange(slotContainer.GetComponentsInChildren<YarnHealthSlotView>(false));
            slots.Sort((a, b) => a.SlotIndex.CompareTo(b.SlotIndex));
        }

        if (slots.Count != YarnCount)
        {
            RebuildSlots();
        }
    }

    private void ApplyAllStates(int halfUnits)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].SetState(GetSlotState(halfUnits, i), fullYarnSprite, halfYarnSprite);
        }
    }

    private static int GetSlotState(int halfUnits, int slotIndex)
    {
        return Mathf.Clamp(halfUnits - slotIndex * 2, 0, 2);
    }

    private Sprite GetStateSprite(int state)
    {
        if (state >= 2)
        {
            return fullYarnSprite;
        }

        return state == 1 ? halfYarnSprite : null;
    }

    private void UpdateHealthText(float currentHealth, float maximumHealth)
    {
        if (healthText == null)
        {
            return;
        }

        float safeMaximum = Mathf.Max(0f, maximumHealth);
        float safeCurrent = Mathf.Clamp(currentHealth, 0f, safeMaximum);
        healthText.text = $"{safeCurrent:0.#} / {safeMaximum:0.#}";
    }

    private void ResetAllSlotPoses()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].ResetPose();
        }
    }

    private void CancelTransition()
    {
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        if (feedbackRoutine != null)
        {
            StopCoroutine(feedbackRoutine);
            feedbackRoutine = null;
        }

        transform.localScale = restingScale;
        SetFlashAlpha(0f);
        ResetAllSlotPoses();
    }

    private void SetFlashAlpha(float alpha)
    {
        if (damageFlash == null)
        {
            return;
        }

        Color color = damageFlash.color;
        color.a = Mathf.Clamp01(alpha);
        damageFlash.color = color;
    }

    private float GetDeltaTime()
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }

    private static float EaseOutCubic(float value)
    {
        float inverse = 1f - Mathf.Clamp01(value);
        return 1f - inverse * inverse * inverse;
    }

    private static float EaseOutBack(float value)
    {
        float t = Mathf.Clamp01(value) - 1f;
        const float overshoot = 1.70158f;
        return 1f + (overshoot + 1f) * t * t * t + overshoot * t * t;
    }
}
