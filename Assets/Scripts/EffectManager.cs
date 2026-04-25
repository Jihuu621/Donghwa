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

public class EffectManager : MonoBehaviour
{
    // Action<StatusKeyword, float>으로 하면 키워드 + 수치까지 전달된대
    public event Action<StatusKeyword, float> OnStatusAdded;
    public event Action<StatusKeyword, float> OnStatusRemoved;

    private class ActiveEffect
    {
        public StatusKeyword Keyword;
        public float TimeRemaining;
        public float Value; // <-- 여기에 퍼센트 저장
    }

    private List<ActiveEffect> activeEffects = new List<ActiveEffect>();

    // 밸류 추가
    public void ApplyStatus(StatusKeyword keyword, float duration, float value = 0)
    {
        ActiveEffect existingEffect = activeEffects.Find(e => e.Keyword == keyword);

        if (existingEffect != null)
        {
            existingEffect.TimeRemaining = Mathf.Max(existingEffect.TimeRemaining, duration);
            // 수치가 더 높은 쪽으로 갱신
            existingEffect.Value = Mathf.Max(existingEffect.Value, value);
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

    void Update()
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