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

    [Header("줄 장력")]
    public bool keepRopeTaut = true;

    [Header("팽팽한 줄의 국소 처짐")]
    [Min(4)] public int tautPointCount = 32;
    [Min(0.01f)] public float localSagRadius = 1.2f;
    [Min(0f)] public float localSagDepth = 0.12f;
    [Min(0f)] public float tautEdgeRadius = 0.02f;
    [Min(0.01f)] public float sagEnterTime = 0.08f;
    [Min(0.01f)] public float sagReleaseTime = 0.5f;
    [Min(0.01f)] public float sagPositionTime = 0.12f;
    public int ropeSortingOrder = -1;

    private LayerMask playerLayer;        // "Player" 레이어 자동 할당
    private int segmentCount;
    private LineRenderer line;
    private EdgeCollider2D edgeCollider;
    private List<Transform> segments = new List<Transform>();
    private List<Rigidbody2D> segmentRbs = new List<Rigidbody2D>();
    private List<Collider2D> segmentColliders = new List<Collider2D>();
    private readonly List<Vector2> tautPoints = new List<Vector2>();
    private HingeJoint2D endJoint;
    private bool isReleased;
    private float sagWeight;
    private float sagWeightVelocity;
    private float displayedPlayerT = 0.5f;
    private int tautPointsFrame = -1;

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

        // 이 오브젝트의 EdgeCollider2D가 실제 발판이다. 생성 직후 Ground 레이어로
        // 맞춰 PlayerController의 점프/지면 판정과 Physics 2D 충돌 설정을 적용한다.
        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer >= 0)
        {
            gameObject.layer = groundLayer;
        }

        edgeCollider = GetComponent<EdgeCollider2D>();
        if (edgeCollider == null) edgeCollider = gameObject.AddComponent<EdgeCollider2D>();
        edgeCollider.edgeRadius = keepRopeTaut ? tautEdgeRadius : 0f;
        line = GetComponent<LineRenderer>();
        // 줄은 캐릭터 뒤에서 그려야 경사 이동 중 발/다리를 관통하는 것처럼 보이지 않는다.
        if (line != null) line.sortingOrder = ropeSortingOrder;

        Rigidbody2D startRB = start.GetComponent<Rigidbody2D>();
        if (startRB == null) startRB = start.gameObject.AddComponent<Rigidbody2D>();
        startRB.bodyType = RigidbodyType2D.Static;

        Rigidbody2D endRB = end.GetComponent<Rigidbody2D>();
        if (endRB == null) endRB = end.gameObject.AddComponent<Rigidbody2D>();
        endRB.bodyType = RigidbodyType2D.Static;

        if (keepRopeTaut)
        {
            line.positionCount = 2;
            return;
        }

        float dist = Vector2.Distance(start.position, end.position);

        segmentCount = Mathf.Clamp(
            Mathf.CeilToInt(dist / segmentLength),
            minSegmentCount,
            maxSegmentCount
        );

        line.positionCount = segmentCount + 2;

        Vector2 dir = (end.position - start.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        segments.Add(start);
        segmentRbs.Add(startRB);
        Rigidbody2D prevRB = startRB;
        Vector2 startAnchor = GetLocalColliderCenter(start);

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
            joint.connectedAnchor = (i == 0) ? startAnchor : new Vector2(0.5f, 0);

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

        // 다른 시스템의 Joint를 재사용하지 않고 이 RopeBridge가 소유할 Joint를 만든다.
        endJoint = end.gameObject.AddComponent<HingeJoint2D>();

        endJoint.connectedBody = prevRB;
        endJoint.autoConfigureConnectedAnchor = false;

        // 오브젝트 회전 각도/피벗과 무관하게 실제 콜라이더 바운드 중심에 줄 연결
        endJoint.anchor = GetLocalColliderCenter(end);
        endJoint.connectedAnchor = new Vector2(0.5f, 0);

        segments.Add(end);
        segmentRbs.Add(endRB);
    }

    /// <summary>
    /// Alt 당김을 시작하기 전에 이 다리가 만든 모든 제약을 즉시 해제한다.
    /// Destroy는 프레임 끝에 반영되므로 먼저 enabled를 꺼서 잔류 Joint가 블록을 붙잡지 않게 한다.
    /// </summary>
    public void ReleaseForPull()
    {
        if (isReleased) return;
        isReleased = true;

        enabled = false;
        if (line != null) line.enabled = false;
        if (edgeCollider != null) edgeCollider.enabled = false;

        foreach (Collider2D segmentCollider in segmentColliders)
        {
            if (segmentCollider != null) segmentCollider.enabled = false;
        }

        for (int i = 1; i < segments.Count - 1; i++)
        {
            if (segments[i] == null) continue;

            if (i < segmentRbs.Count && segmentRbs[i] != null)
            {
                segmentRbs[i].simulated = false;
            }

            HingeJoint2D[] joints = segments[i].GetComponents<HingeJoint2D>();
            foreach (HingeJoint2D joint in joints)
            {
                joint.enabled = false;
            }
        }

        ReleaseEndJoint();
    }

    private static Vector2 GetLocalColliderCenter(Transform root)
    {
        Collider2D[] colliders = root.GetComponentsInChildren<Collider2D>();
        bool hasBounds = false;
        Bounds bounds = default;

        foreach (Collider2D col in colliders)
        {
            if (col == null || !col.enabled || col.isTrigger) continue;

            if (!hasBounds)
            {
                bounds = col.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(col.bounds);
            }
        }

        return hasBounds
            ? (Vector2)root.InverseTransformPoint(bounds.center)
            : Vector2.zero;
    }

    private void ReleaseEndJoint()
    {
        if (endJoint == null) return;

        endJoint.enabled = false;
        Destroy(endJoint);
        endJoint = null;
    }

    private void OnDestroy()
    {
        ReleaseEndJoint();
    }

    void Update()
    {
        if (keepRopeTaut)
        {
            UpdateTautLine();
            return;
        }

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
        if (!keepRopeTaut) ApplyPlayerWeight();
    }

    private void UpdateTautLine()
    {
        if (line == null || !BuildTautPoints()) return;

        line.positionCount = tautPoints.Count;
        for (int i = 0; i < tautPoints.Count; i++)
        {
            line.SetPosition(i, tautPoints[i]);
        }
    }

    private void UpdateTautEdgeCollider()
    {
        if (edgeCollider == null || !TryGetTautEndpoints(out Vector2 startPoint, out Vector2 endPoint)) return;

        // 발판 충돌선은 움직이는 도중에도 하나의 직선으로 유지한다. 플레이어 발밑에서
        // 콜라이더를 계속 재생성하면 대각선에서 접점이 튀어 걸리는 현상이 발생한다.
        edgeCollider.SetPoints(new List<Vector2>
        {
            transform.InverseTransformPoint(startPoint),
            transform.InverseTransformPoint(endPoint)
        });
    }

    // 평소에는 직선으로 유지하되, 플레이어가 밟은 좁은 영역만 부드럽게 처지게 한다.
    // 같은 점 배열을 렌더러와 EdgeCollider에 적용해 보이는 줄과 발판 충돌이 일치한다.
    private bool BuildTautPoints()
    {
        // Update와 LateUpdate가 같은 프레임에 모두 호출한다. 한 프레임 안에서는
        // 같은 곡선을 재사용해야 처짐 보간 속도가 두 배가 되지 않는다.
        if (tautPointsFrame == Time.frameCount && tautPoints.Count > 0) return true;

        tautPoints.Clear();
        if (!TryGetTautEndpoints(out Vector2 startPoint, out Vector2 endPoint)) return false;

        Vector2 rope = endPoint - startPoint;
        float ropeLength = rope.magnitude;
        float playerAlong = 0f;
        bool hasPlayer = TryGetPlayerOnRope(startPoint, endPoint, ropeLength, out playerAlong);
        float playerT = ropeLength > Mathf.Epsilon ? playerAlong / ropeLength : 0.5f;
        float targetSagWeight = hasPlayer ? 1f : 0f;
        float smoothingTime = hasPlayer ? sagEnterTime : sagReleaseTime;
        sagWeight = Mathf.SmoothDamp(sagWeight, targetSagWeight, ref sagWeightVelocity, smoothingTime);

        if (hasPlayer)
        {
            float positionBlend = 1f - Mathf.Exp(-Time.deltaTime / sagPositionTime);
            displayedPlayerT = Mathf.Lerp(displayedPlayerT, playerT, positionBlend);
        }

        int pointCount = sagWeight > 0.001f ? Mathf.Max(4, tautPointCount) : 2;
        float visualPlayerT = Mathf.Clamp01(displayedPlayerT);

        for (int i = 0; i < pointCount; i++)
        {
            float t = i / (float)(pointCount - 1);
            Vector2 point = Vector2.Lerp(startPoint, endPoint, t);

            if (sagWeight > 0.001f && ropeLength > Mathf.Epsilon)
            {
                // 점 하중을 받은 팽팽한 줄처럼, 플레이어 위치가 가장 낮고 양 끝까지
                // 자연스럽게 영향을 전달한다. 발밑만 움푹 파이는 국소 처짐을 피한다.
                float tensionShape = t <= visualPlayerT
                    ? Mathf.Sin(Mathf.PI * 0.5f * t / Mathf.Max(visualPlayerT, 0.001f))
                    : Mathf.Sin(Mathf.PI * 0.5f * (1f - t) / Mathf.Max(1f - visualPlayerT, 0.001f));
                point += Vector2.down * (localSagDepth * sagWeight * tensionShape);
            }

            tautPoints.Add(point);
        }

        tautPointsFrame = Time.frameCount;
        return true;
    }

    private bool TryGetPlayerOnRope(Vector2 startPoint, Vector2 endPoint, float ropeLength, out float playerAlong)
    {
        playerAlong = 0f;
        if (playerLayer == 0 || ropeLength <= Mathf.Epsilon) return false;

        Vector2 middle = (startPoint + endPoint) * 0.5f;
        Collider2D[] candidates = Physics2D.OverlapCircleAll(middle, ropeLength * 0.5f + detectRadius, playerLayer);
        Collider2D player = null;

        // 공격 히트박스처럼 Player 레이어를 공유하는 자식 콜라이더는 무게로 취급하지
        // 않는다. 반드시 PlayerController가 붙은 본체의 콜라이더만 사용한다.
        foreach (Collider2D candidate in candidates)
        {
            PlayerController controller = candidate.GetComponentInParent<PlayerController>();
            if (controller == null) continue;

            Collider2D bodyCollider = controller.GetComponent<Collider2D>();
            if (bodyCollider == null || !bodyCollider.enabled) continue;

            player = bodyCollider;
            break;
        }

        if (player == null) return false;

        // 근처에 서 있는 것만으로 줄이 움직이지 않도록, 본체가 이 줄의 발판
        // 콜라이더를 실제로 밟고 있을 때에만 무게를 적용한다.
        if (edgeCollider == null || !player.IsTouching(edgeCollider)) return false;

        Vector2 direction = (endPoint - startPoint) / ropeLength;
        Vector2 playerCenter = player.bounds.center;
        playerAlong = Mathf.Clamp(Vector2.Dot(playerCenter - startPoint, direction), 0f, ropeLength);
        Vector2 closestPoint = startPoint + direction * playerAlong;

        // 줄의 위/근처에 있는 경우만 처지게 한다. 멀리 있는 플레이어에는 반응하지 않는다.
        float allowedDistance = detectRadius + player.bounds.extents.y;
        return Vector2.Distance(playerCenter, closestPoint) <= allowedDistance;
    }

    private bool TryGetTautEndpoints(out Vector2 startPoint, out Vector2 endPoint)
    {
        startPoint = Vector2.zero;
        endPoint = Vector2.zero;

        if (StartObj == null || EndObj == null) return false;

        TryGetObjectBounds(StartObj.transform, out Bounds startBounds);
        TryGetObjectBounds(EndObj.transform, out Bounds endBounds);

        if (startBounds.center.x <= endBounds.center.x)
        {
            startPoint = new Vector2(startBounds.max.x, startBounds.center.y);
            endPoint = new Vector2(endBounds.min.x, endBounds.center.y);
        }
        else
        {
            startPoint = new Vector2(startBounds.min.x, startBounds.center.y);
            endPoint = new Vector2(endBounds.max.x, endBounds.center.y);
        }

        return true;
    }

    private static bool TryGetObjectBounds(Transform root, out Bounds bounds)
    {
        SpriteRenderer[] sprites = root.GetComponentsInChildren<SpriteRenderer>();
        bool hasBounds = false;
        bounds = default;

        foreach (SpriteRenderer sprite in sprites)
        {
            if (sprite == null || !sprite.enabled) continue;

            if (!hasBounds)
            {
                bounds = sprite.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(sprite.bounds);
            }
        }

        if (hasBounds) return true;

        Collider2D[] colliders = root.GetComponentsInChildren<Collider2D>();
        foreach (Collider2D collider in colliders)
        {
            if (collider == null || !collider.enabled || collider.isTrigger) continue;

            if (!hasBounds)
            {
                bounds = collider.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(collider.bounds);
            }
        }

        if (!hasBounds)
        {
            bounds = new Bounds(root.position, Vector3.zero);
        }

        return true;
    }

    // 세그먼트 위치를 기반으로 EdgeCollider2D 실시간 정점 갱신
    private void UpdateEdgeCollider()
    {
        if (edgeCollider == null) return;

        if (keepRopeTaut)
        {
            UpdateTautEdgeCollider();
            return;
        }

        if (segments.Count == 0) return;

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
