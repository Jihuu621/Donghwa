using System;
using System.Collections.Generic;
using UnityEngine;

public enum StatusKeyword
{
    None,
    Stun, 
    Poison,
    SpeedDown,

    Invincible,
}

public class EffectManager : MonoBehaviour
{
    // 다른 스크립트들이 구독할 수 있는 이벤트 <-- 이거 중요하,ㅁ
    public event Action<StatusKeyword> OnStatusAdded;
    public event Action<StatusKeyword> OnStatusRemoved;

    // 지금 있는거 관리
    private class ActiveEffect
    {
        public StatusKeyword Keyword;
        public float TimeRemaining;
    }

    // 현재 걸린 상태이상 리스트
    private List<ActiveEffect> activeEffects = new List<ActiveEffect>();


    public void ApplyStatus(StatusKeyword keyword, float duration)
    {
        // 중복체크
        ActiveEffect existingEffect = activeEffects.Find(e => e.Keyword == keyword);

        if (existingEffect != null)
        {
            // 중복이면 더 긴걸로 갱신 조지기
            existingEffect.TimeRemaining = Mathf.Max(existingEffect.TimeRemaining, duration);
        }
        else
        {
            // 새로운 상태이상 추가
            activeEffects.Add(new ActiveEffect { Keyword = keyword, TimeRemaining = duration });

            // 상태이상 추가 이벤트
            OnStatusAdded?.Invoke(keyword);
        }
    }

    // 사장님 이거 있어요?
    public bool HasStatus(StatusKeyword keyword)
    {
        return activeEffects.Exists(e => e.Keyword == keyword);
    }

    void Update()
    {
        // 역순 순회 -> 타이머감소
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            activeEffects[i].TimeRemaining -= Time.deltaTime;

            // 시간 다 되면 제거
            if (activeEffects[i].TimeRemaining <= 0)
            {
                StatusKeyword expiredKeyword = activeEffects[i].Keyword;
                activeEffects.RemoveAt(i);

                // 상태이상 끝 이벤트
                OnStatusRemoved?.Invoke(expiredKeyword);
            }
        }
    }
}