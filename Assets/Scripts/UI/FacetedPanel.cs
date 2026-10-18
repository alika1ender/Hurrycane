using UnityEngine;
using UnityEngine.UI;

/// <summary>Resolution-independent, clipped corners for the low-poly title screen.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class FacetedPanel : MaskableGraphic
{
    public float corner = 16;

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect r = rectTransform.rect;
        float c = Mathf.Min(corner, Mathf.Min(r.width, r.height) * .25f);
        Vector2[] points = {
            new Vector2(r.xMin + c, r.yMin), new Vector2(r.xMax - c, r.yMin),
            new Vector2(r.xMax, r.yMin + c), new Vector2(r.xMax, r.yMax - c),
            new Vector2(r.xMax - c, r.yMax), new Vector2(r.xMin + c, r.yMax),
            new Vector2(r.xMin, r.yMax - c), new Vector2(r.xMin, r.yMin + c)
        };
        mesh.AddVert(r.center, color, Vector2.zero);
        foreach (Vector2 p in points) mesh.AddVert(p, color, Vector2.zero);
        for (int i = 0; i < points.Length; i++) mesh.AddTriangle(0, i + 1, (i + 1) % points.Length + 1);
    }
}
