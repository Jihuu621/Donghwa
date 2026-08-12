using System.Collections;
using UnityEngine;
using System.Collections.Generic;

public interface IScissorCutTarget
{
    bool TryScissorCut(Vector2 start, Vector2 end);
}

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

    [Header("잘린 줄 소멸")]
    [SerializeField, Min(0f)] private float ropeCutPieceSeparation = 0.14f;
    [SerializeField, Min(0f)] private float ropeCutPieceSpeed = 2.4f;
    [SerializeField, Min(0f)] private float ropeCutPieceLift = 1.1f;
    [SerializeField, Min(0f)] private float ropeCutRevealDuration = 0.28f;
    [SerializeField, Min(0f)] private float ropeFadeDelay = 0.15f;
    [SerializeField, Min(0.05f)] private float ropeFadeDuration = 1f;

    private LineRenderer lineRenderer;
    private bool isScissorMode = false;
    private bool isDragging = false;
    private Vector2 dragStart;
    private Vector2 dragEnd;
    private readonly RaycastHit2D[] snipHits = new RaycastHit2D[64];
    private readonly HashSet<IScissorCutTarget> snippedTargets = new HashSet<IScissorCutTarget>();
    private ContactFilter2D snipContactFilter;
    private readonly HashSet<GameObject> fadingRopeObjects = new HashSet<GameObject>();

    void Awake()
    {
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
        snipContactFilter = ContactFilter2D.noFilter;
    }

    void Update()
    {
        // 가위 모드
        if (Input.GetKeyDown(KeyCode.R))
        {
            ToggleScissorMode();
        }

        if (!isScissorMode) return;

        // 마우스 오른쪽 버튼 드래그시 시작
        if (Input.GetMouseButtonDown(1))
        {
            isDragging = true;
            dragStart = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, new Vector3(dragStart.x, dragStart.y, 0));
        }

        //선 시각화
        if (isDragging && Input.GetMouseButton(1))
        {
            dragEnd = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            lineRenderer.SetPosition(1, new Vector3(dragEnd.x, dragEnd.y, 0));
            Debug.DrawLine(dragStart, dragEnd, Color.red);
        }

        // 마우스 버튼을 떼면 자르기
        if (isDragging && Input.GetMouseButtonUp(1))
        {
            dragEnd = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            lineRenderer.positionCount = 0; // 선 제거

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

    void Snip(Vector2 start, Vector2 end) // 선에 닿아있는 물체중 Sliceable컴포넌트 있는거 조각으로 나누기 실행코드
    {
        if (Vector2.Distance(start, end) < 0.1f) return;

        snippedTargets.Clear();
        int hitCount = Physics2D.Linecast(start, end, snipContactFilter, snipHits);
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = snipHits[i];
            IScissorCutTarget cutTarget = hit.collider.GetComponentInParent<IScissorCutTarget>();
            if (cutTarget != null)
            {
                if (snippedTargets.Add(cutTarget)) cutTarget.TryScissorCut(start, end);
                continue;
            }

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
                        GameObject original = poly.gameObject;
                        bool isRope = original.CompareTag("Rope");
                        List<GameObject> connectedRope = isRope
                            ? CollectConnectedRopeObjects(original)
                            : null;

                        // 왼쪽 조각과 오른쪽 조각 생성
                        GameObject leftPiece = CreateVisualPiece(original, leftPoints, tex, texRect, spriteBounds, Vector2.left * 2f);
                        GameObject rightPiece = CreateVisualPiece(original, rightPoints, tex, texRect, spriteBounds, Vector2.right * 2f);

                        if (isRope)
                        {
                            leftPiece.layer = original.layer;
                            rightPiece.layer = original.layer;
                            leftPiece.tag = original.tag;
                            rightPiece.tag = original.tag;
                            BeginConnectedRopeFade(connectedRope, original, leftPiece, rightPiece);
                        }
                        Destroy(poly.gameObject);
                    }
                }
            }
        }
    }


    // 위에서 자른거 새 오브젝트로 생성
    GameObject CreateVisualPiece(GameObject original, List<Vector2> points, Texture2D tex, Rect rect, Bounds bounds, Vector2 pushForce)
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
        rb.AddTorque(Random.Range(-2f, 2f), ForceMode2D.Impulse);

        return piece;
    }

    private List<GameObject> CollectConnectedRopeObjects(GameObject cutSegment)
    {
        List<GameObject> result = new List<GameObject>();
        Rigidbody2D cutBody = cutSegment != null ? cutSegment.GetComponent<Rigidbody2D>() : null;
        if (cutBody == null) return result;

        Rigidbody2D[] allBodies = FindObjectsByType<Rigidbody2D>(FindObjectsInactive.Include);
        HashSet<Rigidbody2D> ropeBodies = new HashSet<Rigidbody2D>();
        Dictionary<Rigidbody2D, List<Rigidbody2D>> links = new Dictionary<Rigidbody2D, List<Rigidbody2D>>();

        foreach (Rigidbody2D body in allBodies)
        {
            if (body == null || !body.gameObject.CompareTag("Rope")) continue;
            ropeBodies.Add(body);
            links[body] = new List<Rigidbody2D>();
        }

        foreach (Rigidbody2D body in ropeBodies)
        {
            Joint2D[] joints = body.GetComponents<Joint2D>();
            foreach (Joint2D joint in joints)
            {
                Rigidbody2D connected = joint != null ? joint.connectedBody : null;
                if (connected == null || !ropeBodies.Contains(connected)) continue;

                links[body].Add(connected);
                links[connected].Add(body);
            }
        }

        Queue<Rigidbody2D> pending = new Queue<Rigidbody2D>();
        HashSet<Rigidbody2D> visited = new HashSet<Rigidbody2D>();
        pending.Enqueue(cutBody);
        visited.Add(cutBody);

        while (pending.Count > 0)
        {
            Rigidbody2D body = pending.Dequeue();
            if (body != null) result.Add(body.gameObject);

            if (!links.TryGetValue(body, out List<Rigidbody2D> neighbours)) continue;
            foreach (Rigidbody2D neighbour in neighbours)
            {
                if (neighbour != null && visited.Add(neighbour)) pending.Enqueue(neighbour);
            }
        }

        return result;
    }

    private void BeginConnectedRopeFade(
        List<GameObject> connectedRope,
        GameObject cutSegment,
        GameObject leftPiece,
        GameObject rightPiece)
    {
        HashSet<GameObject> ropeSet = new HashSet<GameObject>();
        HashSet<Rigidbody2D> ropeBodies = new HashSet<Rigidbody2D>();

        if (connectedRope != null)
        {
            foreach (GameObject ropeObject in connectedRope)
            {
                if (ropeObject == null) continue;
                ropeSet.Add(ropeObject);
                Rigidbody2D body = ropeObject.GetComponent<Rigidbody2D>();
                if (body != null) ropeBodies.Add(body);
            }
        }

        // A block or another non-rope endpoint can still own a joint connected to this chain.
        // Release it immediately so gameplay is cut before the visual fade finishes.
        Joint2D[] allJoints = FindObjectsByType<Joint2D>(FindObjectsInactive.Include);
        foreach (Joint2D joint in allJoints)
        {
            if (joint == null || joint.connectedBody == null) continue;
            if (!ropeBodies.Contains(joint.connectedBody) || ropeSet.Contains(joint.gameObject)) continue;

            joint.enabled = false;
            Destroy(joint);
        }

        if (connectedRope != null)
        {
            foreach (GameObject ropeObject in connectedRope)
            {
                if (ropeObject == null || ropeObject == cutSegment) continue;
                PrepareRopeObjectForFade(ropeObject, ropeCutRevealDuration);
            }
        }

        SeparateCutPieces(leftPiece, rightPiece);
        StartCoroutine(FadeCutPieceAfterReveal(leftPiece));
        StartCoroutine(FadeCutPieceAfterReveal(rightPiece));
    }

    private void SeparateCutPieces(GameObject leftPiece, GameObject rightPiece)
    {
        if (ropeCutPieceSeparation <= 0f) return;

        if (leftPiece != null)
        {
            leftPiece.transform.position += Vector3.left * ropeCutPieceSeparation;
            SetCutPieceVelocity(leftPiece, Vector2.left);
        }

        if (rightPiece != null)
        {
            rightPiece.transform.position += Vector3.right * ropeCutPieceSeparation;
            SetCutPieceVelocity(rightPiece, Vector2.right);
        }
    }

    private void SetCutPieceVelocity(GameObject piece, Vector2 horizontalDirection)
    {
        Rigidbody2D body = piece != null ? piece.GetComponent<Rigidbody2D>() : null;
        if (body == null) return;

        body.bodyType = RigidbodyType2D.Dynamic;
        body.simulated = true;
        body.gravityScale = 1f;
        body.linearVelocity = horizontalDirection.normalized * ropeCutPieceSpeed + Vector2.up * ropeCutPieceLift;
    }

    private void PrepareRopeObjectForFade(GameObject ropeObject, float additionalDelay = 0f)
    {
        if (ropeObject == null || !fadingRopeObjects.Add(ropeObject)) return;

        Collider2D[] colliders = ropeObject.GetComponentsInChildren<Collider2D>(true);
        foreach (Collider2D collider in colliders)
        {
            if (collider != null) collider.enabled = false;
        }

        Joint2D[] joints = ropeObject.GetComponents<Joint2D>();
        foreach (Joint2D joint in joints)
        {
            if (joint != null) joint.enabled = false;
        }

        Rigidbody2D body = ropeObject.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.simulated = false;
        }

        StartCoroutine(FadeAndDestroyRopeObject(ropeObject, additionalDelay));
    }

    private IEnumerator FadeCutPieceAfterReveal(GameObject piece)
    {
        if (piece == null || !fadingRopeObjects.Add(piece)) yield break;

        // Keep the two fresh pieces dynamic briefly so the cut visibly opens up
        // before either the chain or the pieces begin to fade.
        if (ropeCutRevealDuration > 0f)
        {
            yield return new WaitForSeconds(ropeCutRevealDuration);
        }

        if (piece == null) yield break;

        Collider2D[] colliders = piece.GetComponentsInChildren<Collider2D>(true);
        foreach (Collider2D collider in colliders)
        {
            if (collider != null) collider.enabled = false;
        }

        // Keep the Rigidbody2D simulated and preserve its velocity while fading.
        // Only collisions are disabled so the visual debris continues flying without
        // interfering with the player, platforms, or the newly freed block.
        yield return FadeAndDestroyRopeObject(piece, 0f);
    }

    private IEnumerator FadeAndDestroyRopeObject(GameObject ropeObject, float additionalDelay = 0f)
    {
        float initialDelay = Mathf.Max(0f, ropeFadeDelay + additionalDelay);
        if (initialDelay > 0f)
        {
            yield return new WaitForSeconds(initialDelay);
        }

        SpriteRenderer[] spriteRenderers = ropeObject != null
            ? ropeObject.GetComponentsInChildren<SpriteRenderer>(true)
            : new SpriteRenderer[0];
        Color[] spriteColors = new Color[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            spriteColors[i] = spriteRenderers[i].color;
        }

        MeshRenderer[] meshRenderers = ropeObject != null
            ? ropeObject.GetComponentsInChildren<MeshRenderer>(true)
            : new MeshRenderer[0];
        Material[] materials = new Material[meshRenderers.Length];
        Color[] materialColors = new Color[meshRenderers.Length];
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            materials[i] = meshRenderers[i].material;
            materialColors[i] = materials[i] != null && materials[i].HasProperty("_Color")
                ? materials[i].color
                : Color.white;
        }

        float elapsed = 0f;
        while (ropeObject != null && elapsed < ropeFadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - Mathf.Clamp01(elapsed / ropeFadeDuration);

            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] == null) continue;
                Color color = spriteColors[i];
                color.a *= alpha;
                spriteRenderers[i].color = color;
            }

            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] == null || !materials[i].HasProperty("_Color")) continue;
                Color color = materialColors[i];
                color.a *= alpha;
                materials[i].color = color;
            }

            yield return null;
        }

        fadingRopeObjects.Remove(ropeObject);
        if (ropeObject != null) Destroy(ropeObject);
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
