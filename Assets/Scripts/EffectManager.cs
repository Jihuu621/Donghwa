using System;
using System.Collections.Generic;
using UnityEngine;

public enum StatusKeyword
{
    None,
    Stun,
    Poison,
    SpeedDown,    // 둔화 (Value: 0.3f면 30% 감소)
    DamageAmp,    // 받피증 (Value: 0.5f면 50% 증가)
    Invincible,
}

[DisallowMultipleComponent]
public class EffectManager : MonoBehaviour
{
    public event Action<StatusKeyword, float> OnStatusAdded;
    public event Action<StatusKeyword, float> OnStatusUpdated;
    public event Action<StatusKeyword, float> OnStatusRemoved;

    public float MovementSpeedMultiplier =>
        1f - Mathf.Clamp01(GetStatusValue(StatusKeyword.SpeedDown));
    public float IncomingDamageMultiplier =>
        1f + Mathf.Max(0f, GetStatusValue(StatusKeyword.DamageAmp));
    public bool BlocksMovement => HasStatus(StatusKeyword.Stun);
    public bool IsInvincible => HasStatus(StatusKeyword.Invincible);

    private class ActiveEffect
    {
        public StatusKeyword Keyword;
        public float TimeRemaining;
        public float Value; // <-- 여기에 퍼센트 저장
    }

    private readonly List<ActiveEffect> activeEffects = new List<ActiveEffect>();

    // 밸류 추가
    public void ApplyStatus(StatusKeyword keyword, float duration, float value = 0)
    {
        if (keyword == StatusKeyword.None || duration <= 0f) return;

        ActiveEffect existingEffect = activeEffects.Find(e => e.Keyword == keyword);

        if (existingEffect != null)
        {
            existingEffect.TimeRemaining = Mathf.Max(existingEffect.TimeRemaining, duration);
            // 수치가 더 높은 쪽으로 갱신
            existingEffect.Value = Mathf.Max(existingEffect.Value, value);
            OnStatusUpdated?.Invoke(keyword, existingEffect.Value);
        }
        else
        {
            activeEffects.Add(new ActiveEffect { Keyword = keyword, TimeRemaining = duration, Value = value });
            OnStatusAdded?.Invoke(keyword, value);
        }
    }

    // 사장님 이 키워드 수치 몇이에요?
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

    private void Update()
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            activeEffects[i].TimeRemaining -= Time.deltaTime;

            if (activeEffects[i].TimeRemaining <= 0)
            {
                StatusKeyword expiredKeyword = activeEffects[i].Keyword;
                float expiredValue = activeEffects[i].Value; // 삭제될 때 수치 기억
                activeEffects.RemoveAt(i);

                OnStatusRemoved?.Invoke(expiredKeyword, expiredValue);
            }
        }
    }
}
