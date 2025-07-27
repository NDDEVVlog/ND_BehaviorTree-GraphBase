using UnityEngine;
using UnityEngine.UIElements;

public class RingElement : VisualElement
{
    public float startAngle = 0f;
    public float endAngle = 360f;
    public float outerRadius = 60f;
    public float innerRadius = 30f;
    public int segments = 64;
    public Color color = Color.cyan;

    public RingElement()
    {
        generateVisualContent += OnGenerateVisualContent;
        style.width = 200;
        style.height = 200;
    }

    void OnGenerateVisualContent(MeshGenerationContext ctx)
    {
        var meshWriteData = ctx.Allocate(segments * 2, (segments - 1) * 6);

        Vector2 center = new Vector2(layout.width / 2, layout.height / 2);
        float angleStep = (endAngle - startAngle) / (segments - 1);

        for (int i = 0; i < segments; i++)
        {
            float angleRad = Mathf.Deg2Rad * (startAngle + i * angleStep);
            float cos = Mathf.Cos(angleRad);
            float sin = Mathf.Sin(angleRad);

            Vector2 outer = center + new Vector2(cos, sin) * outerRadius;
            Vector2 inner = center + new Vector2(cos, sin) * innerRadius;

            meshWriteData.SetNextVertex(new Vertex() { position = outer, tint = color });
            meshWriteData.SetNextVertex(new Vertex() { position = inner, tint = color });
        }

        for (int i = 0; i < segments - 1; i++)
        {
            int i0 = i * 2;
            int i1 = i * 2 + 1;
            int i2 = i * 2 + 2;
            int i3 = i * 2 + 3;

            meshWriteData.SetNextIndex((ushort)i0);
            meshWriteData.SetNextIndex((ushort)i2);
            meshWriteData.SetNextIndex((ushort)i1);

            meshWriteData.SetNextIndex((ushort)i2);
            meshWriteData.SetNextIndex((ushort)i3);
            meshWriteData.SetNextIndex((ushort)i1);
        }
    }
}
