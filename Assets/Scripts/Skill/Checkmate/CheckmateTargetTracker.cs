using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스킬 활성화 시 반경 내 적을 탐지하고 추적한다.
/// OverlapCircleNonAlloc으로 GC 할당을 최소화한다.
/// </summary>
public class CheckmateTargetTracker
{
    private readonly float detectionRadius;
    private readonly LayerMask enemyLayer;

    // NonAlloc용 재사용 배열
    private Collider2D[] overlapBuffer = new Collider2D[32];

    // 현재 추적 중인 적 Transform 목록
    private List<Transform> trackedTargets = new List<Transform>();

    // 공격 이벤트 구독 해제를 위한 매핑
    private List<IAttackNotifier> subscribedNotifiers = new List<IAttackNotifier>();
    private Action onEnemyAttackCallback;

    public List<Transform> TrackedTargets => trackedTargets;

    public CheckmateTargetTracker(float detectionRadius, LayerMask enemyLayer, Action onEnemyAttackCallback)
    {
        this.detectionRadius = detectionRadius;
        this.enemyLayer = enemyLayer;
        this.onEnemyAttackCallback = onEnemyAttackCallback;
    }

    /// <summary>
    /// 플레이어 위치를 중심으로 적을 탐지한다.
    /// </summary>
    public void DetectTargets(Vector2 center)
    {
        Clear();

        int count = Physics2D.OverlapCircleNonAlloc(center, detectionRadius, overlapBuffer, enemyLayer);

        for (int i = 0; i < count; i++)
        {
            Collider2D col = overlapBuffer[i];
            if (col == null) continue;

            Transform t = col.transform;
            if (!trackedTargets.Contains(t))
            {
                trackedTargets.Add(t);
            }

            // IAttackNotifier 구독으로 적 공격 이벤트 수신
            IAttackNotifier notifier = col.GetComponent<IAttackNotifier>();
            if (notifier != null)
            {
                notifier.OnAttackPerformed += onEnemyAttackCallback;
                subscribedNotifiers.Add(notifier);
            }
        }

        Debug.Log($"<color=cyan>[체크메이트]</color> {trackedTargets.Count}명의 적 감지됨");
    }

    /// <summary>
    /// 파괴된 적을 리스트에서 제거한다.
    /// </summary>
    public void CleanupDestroyedTargets()
    {
        for (int i = trackedTargets.Count - 1; i >= 0; i--)
        {
            if (trackedTargets[i] == null)
            {
                trackedTargets.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// 구독 해제 및 리스트 초기화.
    /// </summary>
    public void Clear()
    {
        // 이벤트 구독 해제
        for (int i = 0; i < subscribedNotifiers.Count; i++)
        {
            if (subscribedNotifiers[i] != null)
            {
                subscribedNotifiers[i].OnAttackPerformed -= onEnemyAttackCallback;
            }
        }
        subscribedNotifiers.Clear();
        trackedTargets.Clear();
    }
}