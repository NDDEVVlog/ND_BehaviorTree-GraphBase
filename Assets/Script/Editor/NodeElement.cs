using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using ND_BehaviorTrees;

public class NodeElement : VisualElement
{
    private Node node;
    private Label nodeNameLabel;
    private Vector2 dragStart;

    public NodeElement(Node node)
    {
        this.node = node;
        this.AddToClassList("node-container");

        // Load UXML and USS
        var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Scripts/Editor/NodeElement.uxml");
        var uss = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/Editor/NodeElement.uss");

        if (uxml == null || uss == null)
        {
            Debug.LogError("Missing UXML or USS for NodeElement.");
            return;
        }

        uxml.CloneTree(this);
        this.styleSheets.Add(uss);

        // Setup label
        nodeNameLabel = this.Q<Label>("node-name");
        if (nodeNameLabel != null)
        {
            nodeNameLabel.text = node.name;
            nodeNameLabel.RegisterCallback<MouseDownEvent>(OnDoubleClick);
        }

        // Dragging logic
        this.RegisterCallback<PointerDownEvent>(evt => dragStart = evt.position);
        this.RegisterCallback<PointerMoveEvent>(OnDrag);
    }

    private void OnDoubleClick(MouseDownEvent evt)
    {
        if (evt.clickCount == 2 && evt.button == 0)
        {
            Selection.activeObject = node;
        }
    }

    private void OnDrag(PointerMoveEvent evt)
    {
        if (evt.pressedButtons != 1) return;

        Vector2 currentPosition = (Vector2)evt.position;
        Vector2 delta = currentPosition - dragStart;

        style.left = this.resolvedStyle.left + delta.x;
        style.top = this.resolvedStyle.top + delta.y;
        dragStart = currentPosition;

    #if UNITY_EDITOR
        node.UpdateNodePosition(node, new Vector2(style.left.value.value, style.top.value.value));
    #endif
    }

}
