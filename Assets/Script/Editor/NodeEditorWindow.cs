using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using ND_BehaviorTrees;
public class NodeEditorWindow : EditorWindow
{
    [MenuItem("Window/Node Editor")]
    public static void ShowWindow()
    {
        GetWindow<NodeEditorWindow>("Node Editor");
    }

    private void CreateGUI()
    {
        var node = ScriptableObject.CreateInstance<Selector>();
        node.name = "TestNode";
        var nodeElement = new NodeElement(node);
        rootVisualElement.Add(nodeElement);
    }
}