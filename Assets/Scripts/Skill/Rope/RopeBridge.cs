using System.Collections.Generic;
using UnityEngine;
public class RopeBridge : MonoBehaviour
{
    public GameObject segmentPrefab;
    public float segmentLength = 0.5f;
    public int minSegmentCount = 4;
    public int maxSegmentCount = 200;

    private int segmentCount;
    private LineRenderer line;
    private List<Transform> segments = new List<Transform>();
    private List<Collider2D> segmentColliders = new List<Collider2D>(); // 추가
    public GameObject StartObj { get; private set; }
    public GameObject EndObj { get; private set; }

    // 플레이어가 위로 점프 중이면 모든 세그먼트 충돌 무시
    public void SetPassThrough(Collider2D playerCol, bool ignore)
    {
        for (int i = 0; i < segmentColliders.Count; i++)
        {
            if (segmentColliders[i] != null)
                Physics2D.IgnoreCollision(playerCol, segmentColliders[i], ignore);
        }
    }

    public void Setup(Transform start, Transform end)
    {
        StartObj = start.gameObject;
        EndObj = end.gameObject;

        float dist = Vector2.Distance(start.position, end.position);

        segmentCount = Mathf.Clamp(
            Mathf.CeilToInt(dist / segmentLength),
            minSegmentCount,
            maxSegmentCount
        );

        line = GetComponent<LineRenderer>();
        line.positionCount = segmentCount + 2;

        Rigidbody2D startRB = start.GetComponent<Rigidbody2D>();
        if (startRB == null) startRB = start.gameObject.AddComponent<Rigidbody2D>();
        startRB.bodyType = RigidbodyType2D.Static;

        Rigidbody2D endRB = end.GetComponent<Rigidbody2D>();
        if (endRB == null) endRB = end.gameObject.AddComponent<Rigidbody2D>();
        endRB.bodyType = RigidbodyType2D.Static;

        Vector2 dir = (end.position - start.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        segments.Add(start);
        Rigidbody2D prevRB = startRB;

        float step = dist / (segmentCount + 1);

        for (int i = 0; i < segmentCount; i++)
        {
            GameObject seg = Instantiate(segmentPrefab, transform);
            seg.transform.position = (Vector2)start.position + dir * step * (i + 1);
            seg.transform.rotation = Quaternion.Euler(0, 0, angle);
            seg.transform.localScale = new Vector3(step, seg.transform.localScale.y, seg.transform.localScale.z);

            Rigidbody2D segRB = seg.GetComponent<Rigidbody2D>();
            segRB.angularDamping = 20f;

            HingeJoint2D joint = seg.GetComponent<HingeJoint2D>();
            joint.connectedBody = prevRB;
            joint.autoConfigureConnectedAnchor = false;
            joint.anchor = new Vector2(-0.5f, 0);
            joint.connectedAnchor = (i == 0) ? Vector2.zero : new Vector2(0.5f, 0);

            // 콜라이더 캐시 (PlatformEffector2D 대신 사용)
            Collider2D segCol = seg.GetComponent<Collider2D>();
            if (segCol != null) segmentColliders.Add(segCol);

            prevRB = segRB;
            segments.Add(seg.transform);
        }

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