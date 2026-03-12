using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 타겟 적들의 이동 거리와 공격 횟수를 기반으로 전역 무브 카운트를 추적한다.
/// 매 프레임 Distance 호출 대신 누적 방식으로 이동 거리를 계산한다.
/// </summary>
public class MoveCounter
{
    private float movementThreshold;
    private int moveCount;

    // 적별 누적 이동 거리
    private Dictionary<Transform, float> accumulatedDistance = new Dictionary<Transform, float>();
    // 적별 이전 위치 캐시
    private Dictionary<Transform, Vector2> previousPositions = new Dictionary<Transform, Vector2>();

    public int MoveCount => moveCount;

    public MoveCounter(float movementThreshold)
    {
        this.movementThreshold = movementThreshold;
    }

    public void Reset()
    {
        moveCount = 0;
        accumulatedDistance.Clear();
        previousPositions.Clear();
    }

    /// <summary>
    /// 추적 시작 시 적의 초기 위치를 등록한다.
    /// </summary>
    public void RegisterTarget(Transform target)
    {
        if (target == null) return;
        Vector2 pos = target.position;
        previousPositions[target] = pos;
        accumulatedDistance[target] = 0f;
    }

    /// <summary>
    /// 적의 공격이 감지되었을 때 호출. +1 무브
    /// </summary>
    public void OnEnemyAttack()
    {
        moveCount++;
    }

    /// <summary>
    /// 매 프레임 호출하여 적의 이동 거리를 누적 계산한다.
    /// sqrMagnitude 대신 맨해튼 거리로 간이 계산하여 최적화.
    /// </summary>
    public void UpdateMovement(List<Transform> targets)
    {
        for (int i = 0; i < targets.Count; i++)
        {
            Transform t = targets[i];
            if (t == null) continue;

            Vector2 currentPos = t.position;

            if (!previousPositions.ContainsKey(t))
            {
                RegisterTarget(t);
                continue;
            }

            // 프레임 간 이동 거리 누적 (Vector2.Distance 매 프레임 호출 대신 수동 계산)
            float dx = currentPos.x - previousPositions[t].x;
            float dy = currentPos.y - previousPositions[t].y;
            float frameDist = Mathf.Abs(dx) + Mathf.Abs(dy); // 맨해튼 거리 근사

            accumulatedDistance[t] += frameDist;
            previousPositions[t] = currentPos;

            // 임계값 도달 시 무브 추가
            while (accumulatedDistance[t] >= movementThreshold)
            {
                accumulatedDistance[t] -= movementThreshold;
                moveCount++;
            }
        }
    }
}