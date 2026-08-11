using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Speech Bubble Graphic")]
public sealed class SpeechBubbleGraphic : MaskableGraphic
{
    [SerializeField, Min(0f)] private float cornerRadius = 18f;
    [SerializeField, Range(1, 12)] private int cornerSegments = 5;
    [SerializeField, Min(0f)] private float tailWidth = 28f;
    [SerializeField, Min(0f)] private float tailHeight = 18f;
    [SerializeField] private float tailOffset;

    private readonly List<Vector2> _boundary = new List<Vector2>(32);

    public float TailHeight
    {
        get => tailHeight;
        set
        {
            tailHeight = Mathf.Max(0f, value);
            SetVerticesDirty();
        }
    }

    protected override void Awake()
    {
        base.Awake();
        raycastTarget = false;
    }

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();

        Rect rect = GetPixelAdjustedRect();
        float bodyBottom = Mathf.Min(rect.yMax, rect.yMin + tailHeight);
        float radius = Mathf.Min(cornerRadius, Mathf.Min(rect.width, rect.yMax - bodyBottom) * 0.5f);

        _boundary.Clear();
        AddCorner(new Vector2(rect.xMax - radius, bodyBottom + radius), -90f, 0f, radius);
        AddCorner(new Vector2(rect.xMax - radius, rect.yMax - radius), 0f, 90f, radius);
        AddCorner(new Vector2(rect.xMin + radius, rect.yMax - radius), 90f, 180f, radius);
        AddCorner(new Vector2(rect.xMin + radius, bodyBottom + radius), 180f, 270f, radius);

        Vector2 center = new Vector2(rect.center.x, (bodyBottom + rect.yMax) * 0.5f);
        vertexHelper.AddVert(center, color, Vector2.zero);

        for (int i = 0; i < _boundary.Count; i++)
        {
            vertexHelper.AddVert(_boundary[i], color, Vector2.zero);
        }

        for (int i = 0; i < _boundary.Count; i++)
        {
            int next = (i + 1) % _boundary.Count;
            vertexHelper.AddTriangle(0, i + 1, next + 1);
        }

        if (tailHeight <= 0f || tailWidth <= 0f)
        {
            return;
        }

        float tailCenter = Mathf.Clamp(rect.center.x + tailOffset,
            rect.xMin + radius + tailWidth * 0.5f,
            rect.xMax - radius - tailWidth * 0.5f);
        int tailStart = vertexHelper.currentVertCount;
        vertexHelper.AddVert(new Vector2(tailCenter - tailWidth * 0.5f, bodyBottom + 0.5f), color, Vector2.zero);
        vertexHelper.AddVert(new Vector2(tailCenter + tailWidth * 0.5f, bodyBottom + 0.5f), color, Vector2.zero);
        vertexHelper.AddVert(new Vector2(tailCenter, rect.yMin), color, Vector2.zero);
        vertexHelper.AddTriangle(tailStart, tailStart + 2, tailStart + 1);
    }

    private void AddCorner(Vector2 center, float fromDegrees, float toDegrees, float radius)
    {
        int segments = Mathf.Max(1, cornerSegments);
        for (int i = 0; i <= segments; i++)
        {
            float angle = Mathf.Lerp(fromDegrees, toDegrees, i / (float)segments) * Mathf.Deg2Rad;
            _boundary.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
        }
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        cornerRadius = Mathf.Max(0f, cornerRadius);
        tailWidth = Mathf.Max(0f, tailWidth);
        tailHeight = Mathf.Max(0f, tailHeight);
        SetVerticesDirty();
    }
#endif
}
