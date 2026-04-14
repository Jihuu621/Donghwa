using UnityEngine;
using System.Collections.Generic;

public class VisualScissor : MonoBehaviour
{
    [Header("가위 절단선 설정")]
    public Material defaultSpriteMaterial;

    [Header("가위 커서 설정")]
    public Texture2D scissorCursorTexture; // 가위 모양 커서 텍스처 (Inspector에서 할당)
    public Vector2 cursorHotspot = Vector2.zero;

    private bool isScissorMode = false;
    private bool isDragging = false;
    private Vector2 dragStart;
    private Vector2 dragEnd;

    void Update()
    {
        // 슬래시 키로 가위 모드 토글
        if (Input.GetKeyDown(KeyCode.Slash))
        {
            ToggleScissorMode();
        }

        if (!isScissorMode) return;

        // 오른쪽 마우스 버튼 드래그 시작
        if (Input.GetMouseButtonDown(1))
        {
            isDragging = true;
            dragStart = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }

        // 드래그 중 디버그 라인 표시
        if (isDragging && Input.GetMouseButton(1))
        {
            dragEnd = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Debug.DrawLine(dragStart, dragEnd, Color.red);
        }

        // 오른쪽 마우스 버튼 놓으면 절단 실행 후 모드 해제
        if (isDragging && Input.GetMouseButtonUp(1))
        {
            dragEnd = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Snip(dragStart, dragEnd);
            isDragging = false;
            DeactivateScissorMode();
        }
    }

    void ToggleScissorMode()
    {
        if (isScissorMode)
        {
            DeactivateScissorMode();
        }
        else
        {
            ActivateScissorMode();
        }
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
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto); // 기본 커서 복원
    }

    void Snip(Vector2 start, Vector2 end)
    {
        if (Vector2.Distance(start, end) < 0.1f) return; // 너무 짧은 드래그 무시

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
                        CreateVisualPiece(poly.gameObject, leftPoints, tex, texRect, spriteBounds, Vector2.left * 3f);
                        CreateVisualPiece(poly.gameObject, rightPoints, tex, texRect, spriteBounds, Vector2.right * 3f);
                        Destroy(poly.gameObject);
                    }
                }
            }
        }

        // 절단선을 잠시 동안 시각적으로 유지
        Debug.DrawLine(start, end, Color.yellow, 0.5f);
    }

    void CreateVisualPiece(GameObject original, List<Vector2> points, Texture2D tex, Rect rect, Bounds bounds, Vector2 pushForce)
    {
        GameObject piece = new GameObject(original.name + "_P");
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

        mr.material = new Material(defaultSpriteMaterial);
        mr.material.mainTexture = tex;

        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[points.Count];
        Vector2[] uvs = new Vector2[points.Count];

        float texW = tex.width;
        float texH = tex.height;

        for (int i = 0; i < points.Count; i++)
        {
            vertices[i] = new Vector3(points[i].x, points[i].y, 0);
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
    }

    bool SlicePolygon(PolygonCollider2D collider, Vector2 start, Vector2 end, out List<Vector2> left, out List<Vector2> right)
    {
        left = new List<Vector2>(); right = new List<Vector2>();
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
                left.Add(localInt); right.Add(localInt);
            }
        }
        return left.Count > 2 && right.Count > 2;
    }
    bool IsLeft(Vector2 a, Vector2 b, Vector2 p) => ((b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x)) > 0;
    bool GetIntersection(Vector2 a, Vector2 b, Vector2 c, Vector2 d, out Vector2 intersect)
    {
        intersect = Vector2.zero; float denom = (b.x - a.x) * (d.y - c.y) - (b.y - a.y) * (d.x - c.x);
        if (Mathf.Abs(denom) < 0.001f) return false;
        float t = ((c.x - a.x) * (d.y - c.y) - (c.y - a.y) * (d.x - c.x)) / denom;
        float u = ((c.x - a.x) * (b.y - a.y) - (c.y - a.y) * (b.x - a.x)) / denom;
        if (t >= 0 && t <= 1 && u >= 0 && u <= 1) { intersect = a + t * (b - a); return true; }
        return false;
    }
}