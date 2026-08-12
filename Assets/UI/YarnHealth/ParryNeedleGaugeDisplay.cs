using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class ParryNeedleGaugeDisplay : MonoBehaviour
{
    [Header("Charge Source")]
    [SerializeField] private NeedleSkillManager targetSkill;
    [SerializeField] private bool findPlayerAutomatically = true;

    [Header("View")]
    [SerializeField] private Image mainImage;
    [SerializeField] private Image ghostImage;
    [SerializeField] private List<Sprite> chargeSprites = new List<Sprite>();
    [SerializeField] private TMP_Text chargeText;
    [SerializeField] private RectTransform feedbackRoot;

    [Header("Animation")]
    [SerializeField, Min(0f)] private float chargeStepDuration = 0.2f;
    [SerializeField] private bool useUnscaledTime = true;

    private NeedleSkillManager subscribedSkill;
    private Coroutine transitionRoutine;
    private Coroutine feedbackRoutine;
    private int displayedCharges;
    private int targetCharges;
    private Vector2 restingPosition;
    private Vector3 restingScale = Vector3.one;
    private bool initialized;

    public int DisplayedCharges => displayedCharges;
    public int DisplayedHalfUnits => displayedCharges * 2;
    public int TotalHalfUnits => MaximumVisibleCharges * 2;
    public NeedleSkillManager TargetSkill => targetSkill;

    private int MaximumVisibleCharges => Mathf.Max(0, chargeSprites.Count - 1);

    private void Awake()
    {
        CacheRestingPose();
        ApplyVisualState(displayedCharges);
    }

    private void OnEnable()
    {
        ResolveTarget();
        Subscribe();
        SyncImmediate();
    }

    private void Start()
    {
        ResolveTarget();
        Subscribe();
        SyncImmediate();
    }

    private void OnDisable()
    {
        Unsubscribe();
        StopAnimations();
        ApplyVisualState(displayedCharges);
    }

    private void OnValidate()
    {
        chargeStepDuration = Mathf.Max(0f, chargeStepDuration);
    }

    public void Configure(
        NeedleSkillManager skill,
        Image primaryImage,
        Image transitionImage,
        IReadOnlyList<Sprite> sprites,
        TMP_Text statusText,
        RectTransform animatedRoot)
    {
        Unsubscribe();
        targetSkill = skill;
        mainImage = primaryImage;
        ghostImage = transitionImage;
        chargeText = statusText;
        feedbackRoot = animatedRoot != null ? animatedRoot : transform as RectTransform;

        chargeSprites.Clear();
        if (sprites != null)
        {
            for (int i = 0; i < sprites.Count; i++)
            {
                if (sprites[i] != null) chargeSprites.Add(sprites[i]);
            }
        }

        CacheRestingPose();
        ApplyImmediate(skill != null ? skill.ParryHalfUnits : 0);
        Subscribe();
    }

    public void Bind(NeedleSkillManager skill)
    {
        Unsubscribe();
        targetSkill = skill;
        Subscribe();
        SyncImmediate();
    }

    private void ResolveTarget()
    {
        if (targetSkill != null || !findPlayerAutomatically) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        targetSkill = player.GetComponent<NeedleSkillManager>();
        if (targetSkill == null)
        {
            targetSkill = player.GetComponentInChildren<NeedleSkillManager>(true);
        }
    }

    private void Subscribe()
    {
        if (!Application.isPlaying || targetSkill == null || subscribedSkill == targetSkill) return;

        Unsubscribe();
        subscribedSkill = targetSkill;
        subscribedSkill.OnParryChargeChanged += HandleChargeChanged;
        subscribedSkill.OnNeedleThrowDenied += HandleThrowDenied;
    }

    private void Unsubscribe()
    {
        if (subscribedSkill == null) return;

        subscribedSkill.OnParryChargeChanged -= HandleChargeChanged;
        subscribedSkill.OnNeedleThrowDenied -= HandleThrowDenied;
        subscribedSkill = null;
    }

    private void SyncImmediate()
    {
        ApplyImmediate(targetSkill != null ? targetSkill.ParryHalfUnits : 0);
    }

    private void ApplyImmediate(int halfUnits)
    {
        StopTransition();
        displayedCharges = CalculateVisibleCharges(halfUnits);
        targetCharges = displayedCharges;
        initialized = true;
        ApplyVisualState(displayedCharges);
        UpdateChargeText();
    }

    private void HandleChargeChanged(int currentHalfUnits, int maximumHalfUnits)
    {
        targetCharges = CalculateVisibleCharges(currentHalfUnits);
        UpdateChargeText();

        // One perfect guard is intentionally retained as a hidden half-charge.
        // The visual only changes after every second successful perfect guard.
        if (targetCharges == displayedCharges) return;

        if (!initialized || !Application.isPlaying || chargeStepDuration <= 0f)
        {
            ApplyImmediate(currentHalfUnits);
            return;
        }

        if (transitionRoutine == null)
        {
            transitionRoutine = StartCoroutine(AnimateTowardTarget());
        }
    }

    private IEnumerator AnimateTowardTarget()
    {
        while (displayedCharges != targetCharges)
        {
            int direction = targetCharges > displayedCharges ? 1 : -1;
            int nextCharges = displayedCharges + direction;
            yield return AnimateChargeStep(displayedCharges, nextCharges, direction > 0);
            displayedCharges = nextCharges;
            ApplyVisualState(displayedCharges);
        }

        transitionRoutine = null;
    }

    private IEnumerator AnimateChargeStep(int fromCharges, int toCharges, bool gaining)
    {
        if (mainImage == null || ghostImage == null)
        {
            yield break;
        }

        Sprite fromSprite = GetChargeSprite(fromCharges);
        Sprite toSprite = GetChargeSprite(toCharges);
        mainImage.sprite = toSprite;
        mainImage.enabled = toSprite != null;
        mainImage.color = toSprite != null ? Color.white : Color.clear;
        ghostImage.sprite = fromSprite;
        ghostImage.enabled = fromSprite != null;
        ghostImage.color = fromSprite != null ? Color.white : Color.clear;

        RectTransform mainRect = mainImage.rectTransform;
        RectTransform ghostRect = ghostImage.rectTransform;
        float elapsed = 0f;

        while (elapsed < chargeStepDuration)
        {
            elapsed += GetDeltaTime();
            float t = Mathf.Clamp01(elapsed / chargeStepDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            float pop = 1f + Mathf.Sin(t * Mathf.PI) * (gaining ? 0.12f : 0.05f);

            SetImageAlpha(mainImage, eased);
            SetImageAlpha(ghostImage, 1f - eased);
            mainRect.localScale = Vector3.one * Mathf.Lerp(0.78f, pop, eased);
            mainRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(gaining ? -8f : 8f, 0f, eased));
            ghostRect.localScale = Vector3.one * Mathf.Lerp(1f, gaining ? 1.08f : 0.84f, eased);
            yield return null;
        }
    }

    private int CalculateVisibleCharges(int halfUnits)
    {
        return Mathf.Clamp(Mathf.Max(0, halfUnits) / 2, 0, MaximumVisibleCharges);
    }

    private void ApplyVisualState(int charges)
    {
        if (mainImage != null)
        {
            mainImage.sprite = GetChargeSprite(charges);
            mainImage.enabled = mainImage.sprite != null;
            mainImage.color = mainImage.sprite != null ? Color.white : Color.clear;
            mainImage.rectTransform.localScale = Vector3.one;
            mainImage.rectTransform.localRotation = Quaternion.identity;
        }

        if (ghostImage != null)
        {
            ghostImage.enabled = false;
            ghostImage.sprite = null;
            ghostImage.color = Color.clear;
            ghostImage.rectTransform.localScale = Vector3.one;
            ghostImage.rectTransform.localRotation = Quaternion.identity;
        }
    }

    private Sprite GetChargeSprite(int charges)
    {
        if (chargeSprites.Count == 0) return null;
        return chargeSprites[Mathf.Clamp(charges, 0, chargeSprites.Count - 1)];
    }

    private void HandleThrowDenied()
    {
        if (!isActiveAndEnabled) return;

        if (feedbackRoutine != null) StopCoroutine(feedbackRoutine);
        feedbackRoutine = StartCoroutine(PlayDeniedFeedback());
    }

    private IEnumerator PlayDeniedFeedback()
    {
        if (feedbackRoot == null) yield break;

        const float duration = 0.22f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += GetDeltaTime();
            float t = Mathf.Clamp01(elapsed / duration);
            float offset = Mathf.Sin(t * Mathf.PI * 6f) * (1f - t) * 7f;
            feedbackRoot.anchoredPosition = restingPosition + Vector2.right * offset;
            yield return null;
        }

        feedbackRoot.anchoredPosition = restingPosition;
        feedbackRoutine = null;
    }

    private void UpdateChargeText()
    {
        if (chargeText == null) return;

        int maximumCharges = targetSkill != null
            ? targetSkill.MaximumNeedleCharges
            : MaximumVisibleCharges;
        chargeText.text = $"NEEDLE  {targetCharges} / {maximumCharges}";
    }

    private void CacheRestingPose()
    {
        if (feedbackRoot == null) feedbackRoot = transform as RectTransform;
        if (feedbackRoot == null) return;

        restingPosition = feedbackRoot.anchoredPosition;
        restingScale = feedbackRoot.localScale;
    }

    private void StopAnimations()
    {
        StopTransition();
        if (feedbackRoutine != null)
        {
            StopCoroutine(feedbackRoutine);
            feedbackRoutine = null;
        }

        if (feedbackRoot != null)
        {
            feedbackRoot.anchoredPosition = restingPosition;
            feedbackRoot.localScale = restingScale;
        }
    }

    private void StopTransition()
    {
        if (transitionRoutine == null) return;
        StopCoroutine(transitionRoutine);
        transitionRoutine = null;
    }

    private static void SetImageAlpha(Graphic graphic, float alpha)
    {
        Color color = graphic.color;
        color.a = Mathf.Clamp01(alpha);
        graphic.color = color;
    }

    private float GetDeltaTime()
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }
}
