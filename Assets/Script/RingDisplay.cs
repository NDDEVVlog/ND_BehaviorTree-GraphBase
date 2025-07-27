using UnityEngine;
using UnityEngine.UIElements;

[ExecuteAlways]
public class RingDisplay : MonoBehaviour
{
    [Range(0, 360)] public float startAngle = 0f;
    [Range(0, 360)] public float endAngle = 270f;
    [Min(0)] public float innerRadius = 20f;
    [Min(1)] public float outerRadius = 60f;
    [Range(3, 128)] public int segments = 64;
    public Color color = Color.cyan;

    private UIDocument uiDoc;
    private RingElement ring;

    void OnEnable()
    {
        uiDoc = GetComponent<UIDocument>();
        if (uiDoc == null) return;

        ring = new RingElement();
        uiDoc.rootVisualElement.Clear();
        uiDoc.rootVisualElement.Add(ring);
    }

    void Update()
    {
        if (ring == null) return;

        // Sync values from Inspector
        ring.startAngle = startAngle;
        ring.endAngle = endAngle;
        ring.innerRadius = innerRadius;
        ring.outerRadius = outerRadius;
        ring.segments = segments;
        ring.color = color;

        ring.MarkDirtyRepaint(); // Forces visual update
    }
}
