using System.Collections.Generic;
using UnityEngine;

public class ChainPhysics2D : MonoBehaviour
{
    public Transform startPoint; // 캐릭터의 손
    public Transform endPoint;   // 회중시계
    public LineRenderer lineRenderer;

    public int segmentCount = 10; // 줄을 몇 개로 나눌 것인가
    public float ropeLength = 2f;
    public float gravity = -9.81f;
    public int constraintIterations = 5;

    [Range(0f, 1f)]
    public float tightness = 0.3f;

    private List<RopeSegment> segments = new List<RopeSegment>();

    void Start()
    {
        Vector3 spawnPos = startPoint.position;
        for (int i = 0; i < segmentCount; i++)
        {
            segments.Add(new RopeSegment(spawnPos));
        }
    }

    void OnEnable()
    {
        if (startPoint != null)
            ResetSegments(startPoint.position);
    }

    public void ResetSegments(Vector2 pos)
    {
        for (int i = 0; i < segments.Count; i++)
        {
            segments[i] = new RopeSegment(pos);
        }
    }

    void FixedUpdate()
    {
        Simulate();
    }

    void LateUpdate()
    {
        if (segments.Count > 0)
        {
            // 항상 endpoints는 현재 위치에 고정
            RopeSegment first = segments[0];
            first.posNow = startPoint.position;
            segments[0] = first;

            RopeSegment last = segments[segmentCount - 1];
            last.posNow = endPoint.position;
            segments[segmentCount - 1] = last;

            // tightness에 따라 constraint 반복 횟수 조절
            // 낮으면 관성/처짐 유지, 높으면 팽팽
            int lateIterations = Mathf.Max(1, Mathf.RoundToInt(constraintIterations * tightness));
            for (int i = 0; i < lateIterations; i++)
            {
                ApplyConstraints();
            }
        }
        DrawRope();
    }

    private void Simulate()
    {
        Vector2 forceGravity = new Vector2(0f, gravity);

        for (int i = 1; i < segmentCount; i++)
        {
            RopeSegment segment = segments[i];
            Vector2 velocity = segment.posNow - segment.posOld;
            segment.posOld = segment.posNow;
            segment.posNow += velocity;
            segment.posNow += forceGravity * Time.fixedDeltaTime;
            segments[i] = segment;
        }

        // 제약 조건 계산 (줄 유지)
        for (int i = 0; i < constraintIterations; i++)
        {
            ApplyConstraints();
        }
    }

    private void ApplyConstraints()
    {
        // 시작점 고정 (손)
        RopeSegment firstSegment = segments[0];
        firstSegment.posNow = startPoint.position;
        segments[0] = firstSegment;

        // 끝점 고정 (시계)
        RopeSegment lastSegment = segments[segmentCount - 1];
        lastSegment.posNow = endPoint.position;
        segments[segmentCount - 1] = lastSegment;

        for (int i = 0; i < segmentCount - 1; i++)
        {
            RopeSegment seg1 = segments[i];
            RopeSegment seg2 = segments[i + 1];

            float dist = (seg1.posNow - seg2.posNow).magnitude;
            float error = Mathf.Abs(dist - (ropeLength / segmentCount));
            Vector2 changeDir = Vector2.zero;

            if (dist > (ropeLength / segmentCount)) changeDir = (seg1.posNow - seg2.posNow).normalized;
            else if (dist < (ropeLength / segmentCount)) changeDir = (seg2.posNow - seg1.posNow).normalized;

            Vector2 changeAmount = changeDir * error;
            if (i != 0)
            {
                seg1.posNow -= changeAmount * 0.5f;
                segments[i] = seg1;
                seg2.posNow += changeAmount * 0.5f;
                segments[i + 1] = seg2;
            }
            else
            {
                seg2.posNow += changeAmount;
                segments[i + 1] = seg2;
            }
        }
    }

    private void DrawRope()
    {
        if (lineRenderer == null || segmentCount == 0) return;

        lineRenderer.positionCount = segmentCount;
        for (int i = 0; i < segmentCount; i++)
        {
            lineRenderer.SetPosition(i, segments[i].posNow);
        }

        // 시작/끝점을 transform에 강제 고정하여 로프가 절대 떨어져 보이지 않도록 보장
        if (startPoint != null)
            lineRenderer.SetPosition(0, startPoint.position);
        if (endPoint != null)
            lineRenderer.SetPosition(segmentCount - 1, endPoint.position);
    }

    public struct RopeSegment
    {
        public Vector2 posNow;
        public Vector2 posOld;
        public RopeSegment(Vector2 pos) { posNow = pos; posOld = pos; }
    }
}