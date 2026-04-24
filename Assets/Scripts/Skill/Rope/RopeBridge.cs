using UnityEngine;
using System.Collections.Generic;

public class RopeBridge : MonoBehaviour
{
    public GameObject segmentPrefab; // HingeJoint2D가 달린 작은프리팹
    public int segmentCount = 10;
    private LineRenderer line;
    private List<Transform> segments = new List<Transform>();

    public void Setup(Transform start, Transform end)
    {
        line = GetComponent<LineRenderer>();
        line.positionCount = segmentCount + 2;

        // 1. 시작점 설정
        Rigidbody2D startRB = start.GetComponent<Rigidbody2D>();
        if (startRB == null) startRB = start.gameObject.AddComponent<Rigidbody2D>();
        startRB.bodyType = RigidbodyType2D.Static; // 시작점 고정

        // 2. 끝점 설정 (미리 Rigidbody와 Joint를 준비)
        Rigidbody2D endRB = end.GetComponent<Rigidbody2D>();
        if (endRB == null) endRB = end.gameObject.AddComponent<Rigidbody2D>();
        endRB.bodyType = RigidbodyType2D.Static; // 끝점 고정

        float dist = Vector2.Distance(start.position, end.position);
        Vector2 dir = (end.position - start.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        segments.Add(start);
        Rigidbody2D prevRB = startRB;

        // 3. 마디 생성 및 연결
        for (int i = 0; i < segmentCount; i++)
        {
            GameObject seg = Instantiate(segmentPrefab, transform);
            seg.transform.position = (Vector2)start.position + (dir * (dist / (segmentCount + 1)) * (i + 1));
            seg.transform.rotation = Quaternion.Euler(0, 0, angle);

            Rigidbody2D segRB = seg.GetComponent<Rigidbody2D>();
            segRB.angularDamping = 20f;

            HingeJoint2D joint = seg.GetComponent<HingeJoint2D>();
            joint.connectedBody = prevRB; // 이전 마디에 연결

            // 앵커 수동 고정 (회전 방지용)
            joint.autoConfigureConnectedAnchor = false;
            joint.anchor = new Vector2(-0.5f, 0);
            joint.connectedAnchor = (i == 0) ? Vector2.zero : new Vector2(0.5f, 0);

            prevRB = segRB;
            segments.Add(seg.transform);
        }

        // 4. 마지막 마디와 끝점(Object B) 연결 (핵심!)
        HingeJoint2D endJoint = end.gameObject.GetComponent<HingeJoint2D>();
        if (endJoint == null) endJoint = end.gameObject.AddComponent<HingeJoint2D>();

        endJoint.connectedBody = prevRB; // 마지막 세그먼트의 RB를 연결
        endJoint.autoConfigureConnectedAnchor = false;

        // 끝점의 위치에 맞게 앵커 설정
        endJoint.anchor = Vector2.zero;
        endJoint.connectedAnchor = new Vector2(0.5f, 0); // 마지막 세그먼트의 오른쪽 끝에 붙음

        segments.Add(end);
    }

    void Update()
    {
        // 1. 연결된 모든 마디(시작점, 세그먼트들, 끝점)가 여전히 존재하는지 검사
        for (int i = 0; i < segments.Count; i++)
        {
            // 만약 오브젝트가 Destroy되어 사라졌다면
            if (segments[i] == null)
            {
                // 실 전체를 파괴하고 함수 종료
                Destroy(gameObject);
                return;
            }
        }

        // 2. 모두 살아있다면 라인 렌더러 위치 갱신
        for (int i = 0; i < segments.Count; i++)
        {
            line.SetPosition(i, segments[i].position);
        }
    }
}
