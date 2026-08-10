using System;
using System.Collections.Generic;
using UnityEngine;

public enum StatusKeyword
{
    None,
    Stun,
    Poison,
    SpeedDown,    // ?”í™” (Value: 0.3fë©?30% ê°ì†Œ)
    DamageAmp,    // ë°›í”¼ì¦?(Value: 0.5fë©?50% ì¦ê?)
    Invincible,
}

[DisallowMultipleComponent]
public class EffectManager : MonoBehaviour
{
    // Action<StatusKeyword, float>?¼ë¡œ ?˜ë©´ ?¤ì›Œ??+ ?˜ì¹˜ê¹Œì? ?„ë‹¬?œë?
    public event Action<StatusKeyword, float> OnStatusAdded;
    public event Action<StatusKeyword, float> OnStatusUpdated;
    public event Action<StatusKeyword, float> OnStatusRemoved;

    public float MovementSpeedMultiplier => 1f - Mathf.Clamp01(GetStatusValue(StatusKeyword.SpeedDown));
    public float IncomingDamageMultiplier => 1f + Mathf.Max(0f, GetStatusValue(StatusKeyword.DamageAmp));
    public bool BlocksMovement => HasStatus(StatusKeyword.Stun);
    public bool IsInvincible => HasStatus(StatusKeyword.Invincible);

    private class ActiveEffect
    {
        public StatusKeyword Keyword;
        public float TimeRemaining;
        public float Value; // <-- ?¬ê¸°???¼ì„¼???€??
    }

    private readonly List<ActiveEffect> activeEffects = new List<ActiveEffect>();

    // ë°¸ë¥˜ ì¶”ê?
    public void ApplyStatus(StatusKeyword keyword, float duration, float value = 0)
    {
        if (keyword == StatusKeyword.None || duration <= 0f) return;

        ActiveEffect existingEffect = activeEffects.Find(e => e.Keyword == keyword);

        if (existingEffect != null)
        {
            existingEffect.TimeRemaining = Mathf.Max(existingEffect.TimeRemaining, duration);
            // ?˜ì¹˜ê°€ ???’ì? ìª½ìœ¼ë¡?ê°±ì‹ 
            existingEffect.Value = Mathf.Max(existingEffect.Value, value);
            OnStatusUpdated?.Invoke(keyword, existingEffect.Value);
        }
        else
        {
            activeEffects.Add(new ActiveEffect { Keyword = keyword, TimeRemaining = duration, Value = value });
            OnStatusAdded?.Invoke(keyword, value);
        }
    }

    // ?¬ì¥?????¤ì›Œ???˜ì¹˜ ëª‡ì´?ìš”?
    public float GetStatusValue(StatusKeyword keyword)
    {
        var effect = activeEffects.Find(e => e.Keyword == keyword);
        return effect != null ? effect.Value : 0;
    }

    public bool HasStatus(StatusKeyword keyword) => activeEffects.Exists(e => e.Keyword == keyword);

    public bool TryGetStatus(StatusKeyword keyword, out float remainingTime, out float value)
    {
        ActiveEffect effect = activeEffects.Find(e => e.Keyword == keyword);
        if (effect == null)
        {
            remainingTime = 0f;
            value = 0f;
            return false;
        }

        remainingTime = effect.TimeRemaining;
        value = effect.Value;
        return true;
    }

    public bool RemoveStatus(StatusKeyword keyword)
    {
        int index = activeEffects.FindIndex(e => e.Keyword == keyword);
        if (index < 0) return false;

        float value = activeEffects[index].Value;
        activeEffects.RemoveAt(index);
        OnStatusRemoved?.Invoke(keyword, value);
        return true;
    }

    public void ClearStatuses()
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            StatusKeyword keyword = activeEffects[i].Keyword;
            float value = activeEffects[i].Value;
            activeEffects.RemoveAt(i);
            OnStatusRemoved?.Invoke(keyword, value);
        }
    }

    void Update()
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            activeEffects[i].TimeRemaining -= Time.deltaTime;

            if (activeEffects[i].TimeRemaining <= 0)
            {
                StatusKeyword expiredKeyword = activeEffects[i].Keyword;
                float expiredValue = activeEffects[i].Value; // ?? œ?????˜ì¹˜ ê¸°ì–µ
                activeEffects.RemoveAt(i);

                OnStatusRemoved?.Invoke(expiredKeyword, expiredValue);
            }
        }
    }
}
