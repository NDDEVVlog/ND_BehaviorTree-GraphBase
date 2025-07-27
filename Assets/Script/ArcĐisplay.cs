using UnityEngine;
using UnityEngine.UIElements;

[ExecuteInEditMode]
public class ArcDisplay : MonoBehaviour
{
    public ArcSettings arcSettings;
    private UIDocument uiDoc;
    private ArcElement arcElement;

    void OnEnable()
    {
        uiDoc = GetComponent<UIDocument>();
        if (uiDoc != null && arcSettings != null)
        {
            arcElement = new ArcElement(arcSettings);
            uiDoc.rootVisualElement.Clear();
            uiDoc.rootVisualElement.Add(arcElement);
        }
    }

    void Update()
    {
        // Live update in Editor
        if (!Application.isPlaying && arcElement != null)
        {
            arcElement.MarkDirtyRepaint();
        }
    }
}
