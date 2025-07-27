using UnityEngine;

[CreateAssetMenu(menuName = "UI/Arc Settings")]
public class ArcSettings : ScriptableObject
{
    public float startAngle = 0f;
    public float endAngle = 90f;
    public float radius = 50f;
    public int segments = 20;
    public Color color = Color.green;
}
