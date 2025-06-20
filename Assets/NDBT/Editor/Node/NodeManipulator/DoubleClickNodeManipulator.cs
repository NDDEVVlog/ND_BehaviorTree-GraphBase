using UnityEditor;
using UnityEditor.Experimental.GraphView; // Or your alias 'NodeElements'
using UnityEngine;
using UnityEngine.UIElements; // For MouseDownEvent, etc.
namespace ND_DrawTrello.Editor
{
     public class DoubleClickNodeManipulator : Manipulator // Directly implementing Manipulator
    {
        private ND_NodeEditor _nodeEditorVisual;
        private double _lastClickTime = 0;
        private const double DoubleClickSpeed = 0.3; // Seconds for double click detection

        public DoubleClickNodeManipulator(ND_NodeEditor nodeEditorVisual)
        {
            _nodeEditorVisual = nodeEditorVisual;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            // We want to capture pointer down, as click might be consumed by selection
            target.RegisterCallback<PointerDownEvent>(OnPointerDown);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            // Only react to left clicks (button 0)
            if (evt.button != 0 || !target.ContainsPoint(evt.localPosition)) // Ensure click is within the target bounds
            {
                return;
            }

            double currentTime = EditorApplication.timeSinceStartup;
            if (currentTime - _lastClickTime < DoubleClickSpeed)
            {
                // Double click detected
                _lastClickTime = 0;    // Reset time to prevent triple click acting as double again immediately
                evt.StopPropagation(); // Consume the event so it doesn't cause other actions (like graph selection)
                evt.PreventDefault();  // Further prevent default actions
                HandleDoubleClick();
            }
            else
            {
                // First click of a potential double click
                _lastClickTime = currentTime;
                // Do not stop propagation here, allow single click to select the node
            }
        }

        private void HandleDoubleClick()
        {
            if (_nodeEditorVisual == null || _nodeEditorVisual.node == null)
            {
                Debug.LogWarning("Double-clicked node or its underlying data is null.");
                return;
            }

            // Get the ScriptableObject Node instance that this visual editor represents
            Node nodeAsset = _nodeEditorVisual.node;

            Debug.Log($"Node '{_nodeEditorVisual.title}' (Asset: {nodeAsset.name}, ID: {nodeAsset.id}) double-clicked. Selecting asset.");

           
            // Debug.Log($"Node '{_nodeEditorVisual.title}' (Asset: {nodeAssetToEdit.name}) double-clicked. Opening property window.");
            NodePropertyEditorWindow.Open(nodeAsset);
        }
    }
}
