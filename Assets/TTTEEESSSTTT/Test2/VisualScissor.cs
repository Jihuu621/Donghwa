using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class VisualScissor : MonoBehaviour
{
    [Header("가위 절단선 설정")]
    public Material defaultSpriteMaterial;
    public Color lineColor = Color.red;
    public float lineWidth = 0.05f;

    [Header("가위 커서 설정")]
    public Texture2D scissorCursorTexture;
    public Vector2 cursorHotspot = Vector2.zero;

    private LineRenderer lineRenderer;
    private bool isScissorMode = false;
    private bool isDragging = false;
    private Vector2 dragStart;
    private Vector2 dragEnd;

    void Awake()
    {
        // LineRenderer 컴포넌트 초기화 및 설정
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = 0;

        // 간단한 스프라이트용 머티리얼 적용
        if (lineRenderer.material == null)
        {
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        }
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
    }

    void Update()
    {
        // '/' 키로 가위 모드 토글
        if (Input.GetKeyDown(KeyCode.Slash))
        {
            ToggleScissorMode();
        }

        if (!isScissorMode) return;

        // 마우스 오른쪽 버튼 드래그 시작
        if (Input.GetMouseButtonDown(1))
        {
            isDragging = true;
            dragStart = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, new Vector3(dragStart.x, dragStart.y, 0));
        }

        // 드래그 중: 선 실시간 시각화
        if (isDragging && Input.GetMouseButton(1))
        {
            dragEnd = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            lineRenderer.SetPosition(1, new Vector3(dragEnd.x, dragEnd.y, 0));
            Debug.DrawLine(dragStart, dragEnd, Color.red);
        }

        // 마우스 버튼을 떼면 절단 실행
        if (isDragging && Input.GetMouseButtonUp(1))
        {
            dragEnd = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            lineRenderer.positionCount = 0; // 게임 화면에서 선 제거

            Snip(dragStart, dragEnd);
            isDragging = false;
            DeactivateScissorMode();
        }
    }

    void ToggleScissorMode()
    {
        if (isScissorMode) DeactivateScissorMode();
        else ActivateScissorMode();
    }

    void ActivateScissorMode()
    {
        isScissorMode = true;
        if (scissorCursorTexture != null)
        {
            Cursor.SetCursor(scissorCursorTexture, cursorHotspot, CursorMode.Auto);
        }
    }

    void DeactivateScissorMode()
    {
        isScissorMode = false;
        isDragging = false;
        lineRenderer.positionCount = 0;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    void Snip(Vector2 start, Vector2 end)
    {
        if (Vector2.Distance(start, end) < 0.1f) return;

        // 절단선 상에 있는 모든 콜라이더 체크
        RaycastHit2D[] hits = Physics2D.LinecastAll(start, end);

        foreach (var hit in hits)
        {
            Sliceable target = hit.collider.GetComponent<Sliceable>();
            PolygonCollider2D poly = hit.collider as PolygonCollider2D;

            if (target != null && poly != null)
            {
                if (SlicePolygon(poly, start, end, out var leftPoints, out var rightPoints))
                {
                    Texture2D tex = null;
                    Rect texRect = new Rect();
                    Bounds spriteBounds = new Bounds();

                    SpriteRenderer sr = poly.GetComponent<SpriteRenderer>();
                    if (sr != null && sr.sprite != null)
                    {
                        tex = sr.sprite.texture;
                        texRect = sr.sprite.textureRect;
                        spriteBounds = sr.sprite.bounds;
                    }
                    else
                    {
                        tex = (Texture2D)poly.GetComponent<MeshRenderer>().material.mainTexture;
                        texRect = target.originalRect;
                        spriteBounds = target.originalBounds;
                    }

                    if (tex != null)
                    {
                        // 왼쪽 조각과 오른쪽 조각 생성 (약간의 물리적 반동 추가)
                        CreateVisualPiece(poly.gameObject, leftPoints, tex, texRect, spriteBounds, Vector2.left * 2f);
                        CreateVisualPiece(poly.gameObject, rightPoints, tex, texRect, spriteBounds, Vector2.right * 2f);
                        Destroy(poly.gameObject);
                    }
                }
            }
        }
    }

    void CreateVisualPiece(GameObject original, List<Vector2> points, Texture2D tex, Rect rect, Bounds bounds, Vector2 pushForce)
    {
        GameObject piece = new GameObject(original.name + "_Piece");
        piece.transform.position = original.transform.position;
        piece.transform.rotation = original.transform.rotation;
        piece.transform.localScale = original.transform.localScale;

        Sliceable s = piece.AddComponent<Sliceable>();
        s.originalRect = rect;
        s.originalBounds = bounds;

        PolygonCollider2D pc = piece.AddComponent<PolygonCollider2D>();
        pc.SetPath(0, points.ToArray());

        MeshFilter mf = piece.AddComponent<MeshFilter>();
        MeshRenderer mr = piece.AddComponent<MeshRenderer>();

        mr.material = new Material(defaultSpriteMaterial != null ? defaultSpriteMaterial : new Material(Shader.Find("Sprites/Default")));
        mr.material.mainTexture = tex;

        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[points.Count];
        Vector2[] uvs = new Vector2[points.Count];

        float texW = tex.width;
        float texH = tex.height;

        for (int i = 0; i < points.Count; i++)
        {
            vertices[i] = new Vector3(points[i].x, points[i].y, 0);

            // UV 좌표 계산 (원본 텍스처 좌표 유지)
            float normX = (points[i].x - bounds.min.x) / bounds.size.x;
            float normY = (points[i].y - bounds.min.y) / bounds.size.y;
            uvs[i] = new Vector2((rect.x + (normX * rect.width)) / texW, (rect.y + (normY * rect.height)) / texH);
        }

        Triangulator tr = new Triangulator(points.ToArray());
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = tr.Triangulate();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mf.mesh = mesh;

        Rigidbody2D rb = piece.AddComponent<Rigidbody2D>();
        rb.AddForce(pushForce, ForceMode2D.Impulse);
        // 조각이 자연스럽게 회전하며 떨어지도록 토크 추가
        rb.AddTorque(Random.Range(-2f, 2f), ForceMode2D.Impulse);
    }

    bool SlicePolygon(PolygonCollider2D collider, Vector2 start, Vector2 end, out List<Vector2> left, out List<Vector2> right)
    {
        left = new List<Vector2>();
        right = new List<Vector2>();
        Vector2[] points = collider.points;

        for (int i = 0; i < points.Length; i++)
        {
            Vector2 p1 = collider.transform.TransformPoint(points[i]);
            Vector2 p2 = collider.transform.TransformPoint(points[(i + 1) % points.Length]);

            if (IsLeft(start, end, p1)) left.Add(collider.transform.InverseTransformPoint(p1));
            else right.Add(collider.transform.InverseTransformPoint(p1));

            if (GetIntersection(start, end, p1, p2, out Vector2 intersect))
            {
                Vector2 localInt = collider.transform.InverseTransformPoint(intersect);
                left.Add(localInt);
                right.Add(localInt);
            }
        }
        return left.Count > 2 && right.Count > 2;
    }

    bool IsLeft(Vector2 a, Vector2 b, Vector2 p) => ((b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x)) > 0;

    bool GetIntersection(Vector2 a, Vector2 b, Vector2 c, Vector2 d, out Vector2 intersect)
    {
        intersect = Vector2.zero;
        float denom = (b.x - a.x) * (d.y - c.y) - (b.y - a.y) * (d.x - c.x);
        if (Mathf.Abs(denom) < 0.001f) return false;

        float t = ((c.x - a.x) * (d.y - c.y) - (c.y - a.y) * (d.x - c.x)) / denom;
        float u = ((c.x - a.x) * (b.y - a.y) - (c.y - a.y) * (b.x - a.x)) / denom;

        if (t >= 0 && t <= 1 && u >= 0 && u <= 1)
        {
            intersect = a + t * (b - a);
            return true;
        }
        return false;
    }
}