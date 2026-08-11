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
    [SerializeField] private List<Image> halfUnitFills = new List<Image>();
    [SerializeField] private TMP_Text chargeText;
    [SerializeField] private RectTransform feedbackRoot;
    [SerializeField] private Color filledColor = new Color(0.96f, 0.08f, 0.18f, 1f);
    [SerializeField] private Color emptyColor = new Color(0.12f, 0.035f, 0.055f, 0.9f);

    [Header("Animation")]
    [SerializeField, Min(0f)] private float halfStepDuration = 0.13f;
    [SerializeField] private bool useUnscaledTime = true;

    private NeedleSkillManager subscribedSkill;
    private Coroutine transitionRoutine;
    private Coroutine feedbackRoutine;
    private int displayedHalfUnits;
    private int targetHalfUnits;
    private Vector2 restingPosition;
    private Vector3 restingScale = Vector3.one;
    private bool initialized;

    public int DisplayedHalfUnits => displayedHalfUnits;
    public int TotalHalfUnits => halfUnitFills.Count;
    public NeedleSkillManager TargetSkill => targetSkill;

    private void Awake()
    {
        CacheRestingPose();
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
        ApplyAllStates(displayedHalfUnits);
    }

    private void OnValidate()
    {
        halfStepDuration = Mathf.Max(0f, halfStepDuration);
    }

    public void Configure(
        NeedleSkillManager skill,
        IList<Image> fills,
        TMP_Text statusText,
        RectTransform animatedRoot)
    {
        targetSkill = skill;
        chargeText = statusText;
        feedbackRoot = animatedRoot != null ? animatedRoot : transform as RectTransform;
        halfUnitFills.Clear();

        if (fills != null)
        {
            for (int i = 0; i < fills.Count; i++)
            {
                if (fills[i] != null)
                {
                    halfUnitFills.Add(fills[i]);
                }
            }
        }

        CacheRestingPose();
        ApplyImmediate(skill != null ? skill.ParryHalfUnits : 0);
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
        if (targetSkill != null || !findPlayerAutomatically)
        {
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            targetSkill = player.GetComponent<NeedleSkillManager>();
            if (targetSkill == null)
            {
                targetSkill = player.GetComponentInChildren<NeedleSkillManager>(true);
            }
        }
    }

    private void Subscribe()
    {
        if (!Application.isPlaying || targetSkill == null || subscribedSkill == targetSkill)
        {
            return;
        }

        Unsubscribe();
        subscribedSkill = targetSkill;
        subscribedSkill.OnParryChargeChanged += HandleChargeChanged;
        subscribedSkill.OnNeedleThrowDenied += HandleThrowDenied;
    }

    private void Unsubscribe()
    {
        if (subscribedSkill == null)
        {
            return;
        }

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
        displayedHalfUnits = Mathf.Clamp(halfUnits, 0, TotalHalfUnits);
        targetHalfUnits = displayedHalfUnits;
        initialized = true;
        ApplyAllStates(displayedHalfUnits);
        UpdateChargeText();
    }

    private void HandleChargeChanged(int currentHalfUnits, int maximumHalfUnits)
    {
        targetHalfUnits = Mathf.Clamp(currentHalfUnits, 0, TotalHalfUnits);
        UpdateChargeText();

        if (!initialized || !Application.isPlaying || halfStepDuration <= 0f)
        {
            ApplyImmediate(targetHalfUnits);
            return;
        }

        if (transitionRoutine == null && displayedHalfUnits != targetHalfUnits)
        {
            transitionRoutine = StartCoroutine(AnimateTowardTarget());
        }
    }

    private IEnumerator AnimateTowardTarget()
    {
        while (displayedHalfUnits != targetHalfUnits)
        {
            int direction = targetHalfUnits > displayedHalfUnits ? 1 : -1;
            int imageIndex = direction > 0 ? displayedHalfUnits : displayedHalfUnits - 1;

            if (imageIndex < 0 || imageIndex >= halfUnitFills.Count)
            {
                displayedHalfUnits = targetHalfUnits;
                break;
            }

            Image fill = halfUnitFills[imageIndex];
            yield return AnimateHalf(fill, direction > 0);

            displayedHalfUnits += direction;
            ApplyAllStates(displayedHalfUnits);
        }

        transitionRoutine = null;
    }

    private IEnumerator AnimateHalf(Image fill, bool filling)
    {
        if (fill == null)
        {
            yield break;
        }

        RectTransform rect = fill.rectTransform;
        float elapsed = 0f;

        while (elapsed < halfStepDuration)
        {
            elapsed += GetDeltaTime();
            float t = Mathf.Clamp01(elapsed / halfStepDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            float amount = filling ? eased : 1f - eased;

            fill.color = Color.Lerp(emptyColor, filledColor, amount);
            rect.localScale = new Vector3(Mathf.Lerp(0.2f, 1f, amount), Mathf.Lerp(0.78f, 1f, amount), 1f);
            yield return null;
        }

        fill.color = filling ? filledColor : emptyColor;
        rect.localScale = Vector3.one;

        if (filling && feedbackRoot != null)
        {
            feedbackRoot.localScale = restingScale * 1.025f;
            yield return null;
            feedbackRoot.localScale = restingScale;
        }
    }

    private void HandleThrowDenied()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        if (feedbackRoutine != null)
        {
            StopCoroutine(feedbackRoutine);
        }

        feedbackRoutine = StartCoroutine(PlayDeniedFeedback());
    }

    private IEnumerator PlayDeniedFeedback()
    {
        if (feedbackRoot == null)
        {
            yield break;
        }

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

    private void ApplyAllStates(int halfUnits)
    {
        for (int i = 0; i < halfUnitFills.Count; i++)
        {
            Image fill = halfUnitFills[i];
            if (fill == null)
            {
                continue;
            }

            fill.color = i < halfUnits ? filledColor : emptyColor;
            fill.rectTransform.localScale = Vector3.one;
        }
    }

    private void UpdateChargeText()
    {
        if (chargeText == null)
        {
            return;
        }

        int charges = targetHalfUnits / 2;
        int maximumCharges = targetSkill != null ? targetSkill.MaximumNeedleCharges : Mathf.Max(1, TotalHalfUnits / 2);
        chargeText.text = $"NEEDLE  {charges} / {maximumCharges}";
    }

    private void CacheRestingPose()
    {
        if (feedbackRoot == null)
        {
            feedbackRoot = transform as RectTransform;
        }

        if (feedbackRoot != null)
        {
            restingPosition = feedbackRoot.anchoredPosition;
            restingScale = feedbackRoot.localScale;
        }
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
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }
    }

    private float GetDeltaTime()
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }
}
