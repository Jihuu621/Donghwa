using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(EdgeCollider2D))]
public class RopeBridge : MonoBehaviour
{
    public GameObject segmentPrefab;
    public float segmentLength = 0.5f;
    public int minSegmentCount = 4;
    public int maxSegmentCount = 200;

    [Header("플레이어 하중 물리 설정")]
    public float playerWeightForce = 25f; // 서 있을 때 줄을 누르는 힘
    public float detectRadius = 0.6f;     // 플레이어 감지 거리

    private LayerMask playerLayer;        // "Player" 레이어 자동 할당
    private int segmentCount;
    private LineRenderer line;
    private EdgeCollider2D edgeCollider;
    private List<Transform> segments = new List<Transform>();
    private List<Rigidbody2D> segmentRbs = new List<Rigidbody2D>();
    private List<Collider2D> segmentColliders = new List<Collider2D>();

    public GameObject StartObj { get; private set; }
    public GameObject EndObj { get; private set; }

    private void Awake()
    {
        // 코드에서 자동으로 "Player" 레이어를 찾아 설정
        playerLayer = LayerMask.GetMask("Player");
    }

    public void SetPassThrough(Collider2D playerCol, bool ignore)
    {
        // 개별 세그먼트 대신 단일 EdgeCollider2D와 충돌 여부 제어
        if (edgeCollider != null)
        {
            Physics2D.IgnoreCollision(playerCol, edgeCollider, ignore);
        }
    }

    public void Setup(Transform start, Transform end)
    {
        StartObj = start.gameObject;
        EndObj = end.gameObject;

        edgeCollider = GetComponent<EdgeCollider2D>();

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
        segmentRbs.Add(startRB);
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
            segmentRbs.Add(segRB);

            HingeJoint2D joint = seg.GetComponent<HingeJoint2D>();
            joint.connectedBody = prevRB;
            joint.autoConfigureConnectedAnchor = false;
            joint.anchor = new Vector2(-0.5f, 0);
            joint.connectedAnchor = (i == 0) ? Vector2.zero : new Vector2(0.5f, 0);

            // 세그먼트 콜라이더 캐싱 및 EdgeCollider와의 자가 충돌 방지 (심장박동 진동 방지)
            Collider2D segCol = seg.GetComponent<Collider2D>();
            if (segCol != null)
            {
                segmentColliders.Add(segCol);
                if (edgeCollider != null)
                {
                    Physics2D.IgnoreCollision(edgeCollider, segCol, true);
                }
            }

            prevRB = segRB;
            segments.Add(seg.transform);
        }

        HingeJoint2D endJoint = end.gameObject.GetComponent<HingeJoint2D>();
        if (endJoint == null) endJoint = end.gameObject.AddComponent<HingeJoint2D>();

        endJoint.connectedBody = prevRB;
        endJoint.autoConfigureConnectedAnchor = false;

        // 오브젝트 회전 각도/피벗과 무관하게 실제 콜라이더 바운드 중심에 줄 연결
        Collider2D endCol = end.GetComponent<Collider2D>();
        Vector2 localCenter = endCol != null ? (Vector2)end.InverseTransformPoint(endCol.bounds.center) : Vector2.zero;

        endJoint.anchor = localCenter;
        endJoint.connectedAnchor = new Vector2(0.5f, 0);

        segments.Add(end);
        segmentRbs.Add(endRB);
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
            line.SetPosition(i, segments[i].position);
        }
    }

    void LateUpdate()
    {
        UpdateEdgeCollider();
    }

    void FixedUpdate()
    {
        ApplyPlayerWeight();
    }

    // 세그먼트 위치를 기반으로 EdgeCollider2D 실시간 정점 갱신
    private void UpdateEdgeCollider()
    {
        if (edgeCollider == null || segments.Count == 0) return;

        List<Vector2> localPoints = new List<Vector2>(segments.Count);
        for (int i = 0; i < segments.Count; i++)
        {
            if (segments[i] != null)
            {
                localPoints.Add(transform.InverseTransformPoint(segments[i].position));
            }
        }
        edgeCollider.SetPoints(localPoints);
    }

    // 플레이어가 EdgeCollider 위에 있을 때 해당 세그먼트에 하향 힘 전달
    private void ApplyPlayerWeight()
    {
        for (int i = 1; i < segments.Count - 1; i++)
        {
            if (segments[i] == null) continue;

            Collider2D player = Physics2D.OverlapCircle(segments[i].position, detectRadius, playerLayer);
            if (player != null)
            {
                segmentRbs[i].AddForce(Vector2.down * playerWeightForce, ForceMode2D.Force);
            }
        }
    }
}