using UnityEngine;
using UnityEngine.UIElements;

public class ArcElement : VisualElement
{
    private ArcSettings settings;

    public ArcElement(ArcSettings settings)
    {
        this.settings = settings;
        generateVisualContent += OnGenerateVisualContent;

        style.width = 150;
        style.height = 150;
    }

    public void SetSettings(ArcSettings newSettings)
    {
        settings = newSettings;
        MarkDirtyRepaint(); // Forces visual refresh
    }

    void OnGenerateVisualContent(MeshGenerationContext ctx)
    {
        if (settings == null) return;

        var painter = ctx.painter2D;

        float angleStep = (settings.endAngle - settings.startAngle) / settings.segments;
        Vector2 center = new Vector2(layout.width / 2, layout.height / 2);

        float radStart = Mathf.Deg2Rad * settings.startAngle;

        painter.BeginPath();
        painter.MoveTo(center);

        for (int i = 0; i <= settings.segments; i++)
        {
            float angle = radStart + Mathf.Deg2Rad * angleStep * i;
            float x = center.x + Mathf.Cos(angle) * settings.radius;
            float y = center.y + Mathf.Sin(angle) * settings.radius;
            painter.LineTo(new Vector2(x, y));
        }

        painter.ClosePath();
        painter.fillColor = settings.color;
        painter.Fill();
    }
}
