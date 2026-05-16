using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
[RequireComponent(typeof(CanvasRenderer))]
public class TelemetryLineGraph :  MaskableGraphic
{

    public float lineThickness = 2f;

    public Color lineColor = Color.cyan;

    public Color fillColor = new Color(0f, 1f, 1f, 0.08f);

    public bool autoRange = true;

    public float manualMin = 0f;
    public float manualMax = 100f;

    public bool  showGrid    = true;
    public int   gridLines   = 3;
    public Color gridColor   = new Color(1f, 1f, 1f, 0.06f);
    public float gridThickness = 0.5f;
 
    private TelemetryRingBuffer _buffer;
 
    public void SetBuffer(TelemetryRingBuffer buffer)
    {
        _buffer = buffer;
        SetVerticesDirty();
    }
 
    public void Refresh() => SetVerticesDirty();
 
    public override Color color
    {
        get => lineColor;
        set { lineColor = value; SetVerticesDirty(); }
    }
 
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
 
        Rect rect = rectTransform.rect;
        float w = rect.width;
        float h = rect.height;
        float x0 = rect.xMin;
        float y0 = rect.yMin;
 
        if (showGrid && gridLines > 0)
        {
            for (int g = 0; g <= gridLines; g++)
            {
                float yNorm = (float)g / gridLines;
                float yPx   = y0 + yNorm * h;
                DrawRect(vh, x0, yPx - gridThickness * 0.5f,
                             w,  gridThickness, gridColor);
            }
        }
 
        if (_buffer == null || _buffer.Count < 2) return;
 
        float yMin, yMax;
        if (autoRange)
        {
            yMin = _buffer.Min;
            yMax = _buffer.Max;
        }
        else
        {
            yMin = manualMin;
            yMax = manualMax;
        }
 
        float yRange = yMax - yMin;
        if (yRange < 0.001f) yRange = 1f; 
 
        int n = _buffer.Count;
 

        if (fillColor.a > 0.001f)
        {
            for (int i = 0; i < n - 1; i++)
            {
                float xA = x0 + (float)i       / (n - 1) * w;
                float xB = x0 + (float)(i + 1) / (n - 1) * w;
                float yA = y0 + Mathf.Clamp01((_buffer.Get(i)     - yMin) / yRange) * h;
                float yB = y0 + Mathf.Clamp01((_buffer.Get(i + 1) - yMin) / yRange) * h;
 
                int idx = vh.currentVertCount;
                vh.AddVert(new Vector3(xA, y0),  fillColor, Vector2.zero);
                vh.AddVert(new Vector3(xA, yA),  fillColor, Vector2.zero);
                vh.AddVert(new Vector3(xB, yB),  fillColor, Vector2.zero);
                vh.AddVert(new Vector3(xB, y0),  fillColor, Vector2.zero);
                vh.AddTriangle(idx, idx + 1, idx + 2);
                vh.AddTriangle(idx, idx + 2, idx + 3);
            }
        }
 

        for (int i = 0; i < n - 1; i++)
        {
            float xA = x0 + (float)i       / (n - 1) * w;
            float xB = x0 + (float)(i + 1) / (n - 1) * w;
            float yA = y0 + Mathf.Clamp01((_buffer.Get(i)     - yMin) / yRange) * h;
            float yB = y0 + Mathf.Clamp01((_buffer.Get(i + 1) - yMin) / yRange) * h;
 
            DrawLine(vh, new Vector2(xA, yA), new Vector2(xB, yB), lineThickness, lineColor);
        }
    }

    static void DrawRect(VertexHelper vh, float x, float y, float w, float h, Color c)
    {
        int i = vh.currentVertCount;
        vh.AddVert(new Vector3(x,     y),     c, Vector2.zero);
        vh.AddVert(new Vector3(x,     y + h), c, Vector2.zero);
        vh.AddVert(new Vector3(x + w, y + h), c, Vector2.zero);
        vh.AddVert(new Vector3(x + w, y),     c, Vector2.zero);
        vh.AddTriangle(i, i + 1, i + 2);
        vh.AddTriangle(i, i + 2, i + 3);
    }
 
    static void DrawLine(VertexHelper vh, Vector2 a, Vector2 b, float thickness, Color c)
    {
        Vector2 dir  = (b - a).normalized;
        Vector2 perp = new Vector2(-dir.y, dir.x) * (thickness * 0.5f);
 
        int i = vh.currentVertCount;
        vh.AddVert(new Vector3(a.x - perp.x, a.y - perp.y), c, Vector2.zero);
        vh.AddVert(new Vector3(a.x + perp.x, a.y + perp.y), c, Vector2.zero);
        vh.AddVert(new Vector3(b.x + perp.x, b.y + perp.y), c, Vector2.zero);
        vh.AddVert(new Vector3(b.x - perp.x, b.y - perp.y), c, Vector2.zero);
        vh.AddTriangle(i, i + 1, i + 2);
        vh.AddTriangle(i, i + 2, i + 3);
    }
}
