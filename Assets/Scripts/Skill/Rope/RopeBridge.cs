using UnityEngine;
using System.Collections.Generic;

public class RopeBridge : MonoBehaviour
{
    public GameObject segmentPrefab; // HingeJoint2D가 달린 나무조각용
    public float segmentLength = 0.5f;   // 세그먼트 1개의 기준 길이
    public int minSegmentCount = 4;      // 최소 개수(너무 짧을 때 보정)
    public int maxSegmentCount = 200;    // 안전을 위한 최대 개수

    private int segmentCount;            // 런타임에 계산됨
    private LineRenderer line;
    private List<Transform> segments = new List<Transform>();
    public GameObject StartObj { get; private set; }
    public GameObject EndObj { get; private set; }

    public void Setup(Transform start, Transform end)
    {
        StartObj = start.gameObject;
        EndObj = end.gameObject;

        float dist = Vector2.Distance(start.position, end.position);

        //거리 기반 동적 세그먼트 개수 계산
        segmentCount = Mathf.Clamp(
            Mathf.CeilToInt(dist / segmentLength),
            minSegmentCount,
            maxSegmentCount
        );

        line = GetComponent<LineRenderer>();
        line.positionCount = segmentCount + 2;

        // 1. 시작점 고정
        Rigidbody2D startRB = start.GetComponent<Rigidbody2D>();
        if (startRB == null) startRB = start.gameObject.AddComponent<Rigidbody2D>();
        startRB.bodyType = RigidbodyType2D.Static;

        // 2. 끝점 세팅
        Rigidbody2D endRB = end.GetComponent<Rigidbody2D>();
        if (endRB == null) endRB = end.gameObject.AddComponent<Rigidbody2D>();
        endRB.bodyType = RigidbodyType2D.Static;

        Vector2 dir = (end.position - start.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        segments.Add(start);
        Rigidbody2D prevRB = startRB;

        float step = dist / (segmentCount + 1);

        // 3. 중간 세그먼트 생성
        for (int i = 0; i < segmentCount; i++)
        {
            GameObject seg = Instantiate(segmentPrefab, transform);
            seg.transform.position = (Vector2)start.position + dir * step * (i + 1);
            seg.transform.rotation = Quaternion.Euler(0, 0, angle);

            // 프리팹의 기본 길이(1)에 맞춰 스케일 조정 → 거리에 맞게 늘어나도록
            seg.transform.localScale = new Vector3(step, seg.transform.localScale.y, seg.transform.localScale.z);

            Rigidbody2D segRB = seg.GetComponent<Rigidbody2D>();
            segRB.angularDamping = 20f;

            HingeJoint2D joint = seg.GetComponent<HingeJoint2D>();
            joint.connectedBody = prevRB;

            joint.autoConfigureConnectedAnchor = false;
            joint.anchor = new Vector2(-0.5f, 0);
            joint.connectedAnchor = (i == 0) ? Vector2.zero : new Vector2(0.5f, 0);

            prevRB = segRB;
            segments.Add(seg.transform);
        }

        // 4. 마지막 조인트(Object B)
        HingeJoint2D endJoint = end.gameObject.GetComponent<HingeJoint2D>();
        if (endJoint == null) endJoint = end.gameObject.AddComponent<HingeJoint2D>();

        endJoint.connectedBody = prevRB;
        endJoint.autoConfigureConnectedAnchor = false;
        endJoint.anchor = Vector2.zero;
        endJoint.connectedAnchor = new Vector2(0.5f, 0);

        segments.Add(end);
    }

    void Update()
    {
        for (int i = 0; i < segments.Count; i++)
        {
            if (segments[i] == null)
            {
                Destroy(gameObject);
                return;
            }
        }

        for (int i = 0; i < segments.Count; i++)
        {
            line.SetPosition(i, segments[i].position);
        }
    }
}
