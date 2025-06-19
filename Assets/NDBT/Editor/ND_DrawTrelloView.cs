// File: ND_DrawTrelloView.cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading.Tasks; // For async/await
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ND_DrawTrello.Editor
{
    public class ND_DrawTrelloView : GraphView
    {
        private DrawTrello m_BTree;
        private SerializedObject m_serialLizeObject;

        private ND_DrawTrelloEditorWindow m_window;
        public ND_DrawTrelloEditorWindow window => m_window;

        public List<ND_NodeEditor> m_treeNodes;
        public Dictionary<Edge, ND_BTConnection> m_connectionDictionary;
        public Dictionary<string, ND_NodeEditor> m_nodeDictionary;

        private ND_DrawTrelloSearchProvider m_searchProvider;


        public bool hasUnsavedChanges = false;

        public ND_DrawTrelloView(SerializedObject serializedObject, ND_DrawTrelloEditorWindow window)
        {
            m_window = window;
            m_serialLizeObject = serializedObject;
            m_BTree = (DrawTrello)serializedObject.targetObject;

            // Initialize collections
            m_treeNodes = new List<ND_NodeEditor>();
            m_nodeDictionary = new Dictionary<string, ND_NodeEditor>();
            m_connectionDictionary = new Dictionary<Edge, ND_BTConnection>();

            // Initialize search provider
            m_searchProvider = ScriptableObject.CreateInstance<ND_DrawTrelloSearchProvider>();
            if (m_searchProvider == null) Debug.LogError("[ND_DrawTrelloView.ctor] SearchProvider is NULL after CreateInstance!");
            else Debug.Log("[ND_DrawTrelloView.ctor] SearchProvider created successfully.");
            m_searchProvider.view = this;

            // Assign node creation request
            this.nodeCreationRequest = ShowSearchWindow;
            if (this.nodeCreationRequest == null) Debug.LogError("[ND_DrawTrelloView.ctor] nodeCreationRequest is NULL after assignment!");
            else Debug.Log("[ND_DrawTrelloView.ctor] nodeCreationRequest assigned successfully.");

            // Styling
            StyleSheet styleSheetAsset = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/NDBT/Editor/Resources/Styles/BehaviourTreeView/BehaviourTreeView.uss");
            if (styleSheetAsset != null) styleSheets.Add(styleSheetAsset);
            else Debug.LogWarning("[ND_DrawTrelloView.ctor] BehaviourTreeView.uss not found or loaded.");

            style.flexGrow = 1;
            style.width = Length.Percent(100);
            style.height = Length.Percent(100);

            // Background
            GridBackground background = new GridBackground();
            background.name = "Grid";
            Add(background);
            background.SendToBack();

            // Manipulators for interaction
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new ClickSelector());

            // Initial drawing of existing nodes/connections
            DrawNodes();
            DrawConnections(); // Corrected from DrawConnection

            // Setup zoom
            this.SetupZoom(0.1f, 3.0f);

            // Handle graph changes
            graphViewChanged += OnGraphViewChangeEvent;

            // Handle custom deletion for animation
            this.deleteSelection = (operationName, askUser) => OnDeleteSelectionOperation(operationName, askUser);
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            List<Port> allPorts = new List<Port>();
            List<Port> compatiblePorts = new List<Port>();

            // Ensure m_treeNodes is initialized
            if (m_treeNodes == null) return compatiblePorts;

            foreach (var nodeEditor in m_treeNodes)
            {
                if (nodeEditor == null || nodeEditor.Ports == null) continue;
                allPorts.AddRange(nodeEditor.Ports);
            }

            foreach (Port p in allPorts)
            {
                if (p == startPort) continue;
                if (p.node == startPort.node) continue;
                if (startPort.direction == p.direction) continue;
                if (startPort.portType == p.portType) // Or more specific type checking if needed
                {
                    compatiblePorts.Add(p);
                }
            }
            return compatiblePorts;
        }

        private GraphViewChange OnGraphViewChangeEvent(GraphViewChange graphViewChange)
        {
            // Moved elements
            if (graphViewChange.movedElements != null)
            {
                Undo.RecordObject(m_serialLizeObject.targetObject, "Moved Elements");
                foreach (ND_NodeEditor editorNode in graphViewChange.movedElements.OfType<ND_NodeEditor>())
                {
                    editorNode.SavePosition();
                }
                EditorUtility.SetDirty(m_BTree); // Mark dirty after move
            }

            // Elements to remove (should ideally be handled by our custom delete for nodes)
            // This might still catch edges removed by other means or if custom delete isn't used.
            if (graphViewChange.elementsToRemove != null)
            {
                bool dataChanged = false;
                // Edges are typically removed directly without complex animation by GraphView if not handled by custom delete
                foreach (Edge edge in graphViewChange.elementsToRemove.OfType<Edge>().ToList())
                {
                    if (m_connectionDictionary.ContainsKey(edge)) // Check if we manage this edge
                    {
                        RemoveDataForEdge(edge);
                        dataChanged = true;
                    }
                }
                // Nodes - if our custom delete wasn't called, this might be a fallback
                // but ideally nodes are handled by OnDeleteSelectionOperation
                foreach (ND_NodeEditor nodeEditor in graphViewChange.elementsToRemove.OfType<ND_NodeEditor>().ToList())
                {
                     if (m_nodeDictionary.ContainsKey(nodeEditor.node.id)) // Check if we manage this node
                     {
                        // This removal won't be animated if it hits here.
                        RemoveDataForNode(nodeEditor);
                        dataChanged = true;
                     }
                }
                if (dataChanged)
                {
                    Undo.RecordObject(m_serialLizeObject.targetObject, "Graph Elements Removed");
                    EditorUtility.SetDirty(m_BTree);
                }
            }

            // Edges to create
            if (graphViewChange.edgesToCreate != null)
            {
                Undo.RecordObject(m_serialLizeObject.targetObject, "Connections Created");
                foreach (Edge edge in graphViewChange.edgesToCreate)
                {
                    CreateDataForEdge(edge);
                }
                EditorUtility.SetDirty(m_BTree);
            }

            if (graphViewChange.movedElements != null || 
                (graphViewChange.elementsToRemove != null && graphViewChange.elementsToRemove.Count > 0) ||
                (graphViewChange.edgesToCreate != null && graphViewChange.edgesToCreate.Count > 0))
            {
                m_window.SetUnsavedChanges(true); // Use window's unsaved changes flag
            }

            return graphViewChange;
        }

        private void CreateDataForEdge(Edge edge)
        {
            if (!(edge.input?.node is ND_NodeEditor inputNodeEditor) || !(edge.output?.node is ND_NodeEditor outputNodeEditor))
            {
                Debug.LogError("[CreateDataForEdge] Edge has invalid input or output node editor.");
                return;
            }

            int inputIndex = inputNodeEditor.Ports.IndexOf(edge.input);
            int outputIndex = outputNodeEditor.Ports.IndexOf(edge.output);

            if (inputIndex == -1 || outputIndex == -1)
            {
                Debug.LogError("[CreateDataForEdge] Could not find port index for edge.");
                return;
            }

            ND_BTConnection connection = new ND_BTConnection(inputNodeEditor.node.id, inputIndex, outputNodeEditor.node.id, outputIndex);
            m_BTree.connections.Add(connection);
            m_connectionDictionary.Add(edge, connection); // Keep track of the visual edge and its data
            Debug.Log($"[CreateDataForEdge] Created connection: {outputNodeEditor.node.id}:{outputIndex} -> {inputNodeEditor.node.id}:{inputIndex}");
        }

        private void RemoveDataForEdge(Edge e)
        {
            if (m_connectionDictionary.TryGetValue(e, out ND_BTConnection connection))
            {
                m_BTree.connections.Remove(connection);
                m_connectionDictionary.Remove(e);
                Debug.Log($"[RemoveDataForEdge] Removed connection data for edge.");
            }
        }

        private void RemoveDataForNode(ND_NodeEditor editorNode)
        {
            if (editorNode == null || editorNode.node == null) return;
            Debug.Log($"[RemoveDataForNode] Removing data for node: {editorNode.node.id} ({editorNode.title})");
            m_BTree.nodes.Remove(editorNode.node);
            m_nodeDictionary.Remove(editorNode.node.id);
            m_treeNodes.Remove(editorNode);
        }

        private void DrawNodes()
        {
            if (m_BTree.nodes == null) return;
            Debug.Log($"[DrawNodes] Attempting to draw {m_BTree.nodes.Count} nodes.");
            foreach (Node nodeData in m_BTree.nodes)
            {
                if (nodeData == null)
                {
                    Debug.LogWarning("[DrawNodes] Found a null node in the data list.");
                    continue;
                }
                AddNodeToGraphVisualsOnly(nodeData); // For initial load, don't re-add to data model
            }
            Bind(); // Bind after all elements are potentially added
        }
        
        private void AddNodeToGraphVisualsOnly(Node nodeData, bool animate = false)
        {
            if (nodeData == null) {
                Debug.LogError("[AddNodeToGraphVisualsOnly] nodeData is null.");
                return;
            }
            if (string.IsNullOrEmpty(nodeData.typeName)) {
                 nodeData.typeName = nodeData.GetType().AssemblyQualifiedName;
            }

            // Check if already exists visually (e.g. during a complex undo/redo or error)
            if (m_nodeDictionary.ContainsKey(nodeData.id)) {
                Debug.LogWarning($"[AddNodeToGraphVisualsOnly] Node editor for {nodeData.id} already exists. Skipping visual add.");
                return;
            }

            ND_NodeEditor nodeEditor = null;
            try
            {
                nodeEditor = new ND_NodeEditor(nodeData, m_serialLizeObject);
                // The ND_NodeEditor constructor should add the "nd-node" class.
            }
            catch (Exception e)
            {
                Debug.LogError($"[AddNodeToGraphVisualsOnly] EXCEPTION during 'new ND_NodeEditor()' for node type {nodeData.typeName}: {e.Message}\nStackTrace: {e.StackTrace}");
                return;
            }
             if (nodeEditor == null)
            {
                Debug.LogError($"[AddNodeToGraphVisualsOnly] CRITICAL: ND_NodeEditor was NULL after 'new ND_NodeEditor()'. This means constructor failed, likely due to UXML issues. Check logs from ND_DrawTrelloSetting for UXML path issues.");
                return;
            }

            nodeEditor.SetPosition(nodeData.position);
            m_treeNodes.Add(nodeEditor); // Add to local list of editor nodes
            m_nodeDictionary.Add(nodeData.id, nodeEditor); // Add to lookup dictionary

            AddElement(nodeEditor); // Add to GraphView's visual tree

            if (animate) {
                nodeEditor.schedule.Execute(() => {
                    if (nodeEditor != null && nodeEditor.parent != null)
                    {
                        nodeEditor.AddToClassList("appeared");
                    }
                }).StartingIn(10); // Small delay for animation
            } else {
                // If not animating (e.g., initial load), make it appear instantly
                nodeEditor.AddToClassList("appeared");
            }
        }


        private void DrawConnections() // Corrected name
        {
            if (m_BTree.connections == null) return;
            Debug.Log($"[DrawConnections] Attempting to draw {m_BTree.connections.Count} connections.");
            foreach (ND_BTConnection connectionData in m_BTree.connections)
            {
                MakeConnectionVisualsOnly(connectionData);
            }
        }

        private void MakeConnectionVisualsOnly(ND_BTConnection connectionData)
        {
            ND_NodeEditor inputEditorNode = GetEditorNode(connectionData.inputPort.nodeID);
            ND_NodeEditor outputEditorNode = GetEditorNode(connectionData.outputPort.nodeID);

            if (inputEditorNode == null || outputEditorNode == null)
            {
                Debug.LogWarning($"[MakeConnectionVisualsOnly] Could not find input or output node editor for connection. Input: {connectionData.inputPort.nodeID}, Output: {connectionData.outputPort.nodeID}");
                return;
            }

            if (connectionData.inputPort.portIndex >= inputEditorNode.Ports.Count || connectionData.outputPort.portIndex >= outputEditorNode.Ports.Count) {
                Debug.LogWarning($"[MakeConnectionVisualsOnly] Port index out of bounds for connection. Input: {connectionData.inputPort.portIndex} (max {inputEditorNode.Ports.Count-1}), Output: {connectionData.outputPort.portIndex} (max {outputEditorNode.Ports.Count-1})");
                return;
            }

            Port inPort = inputEditorNode.Ports[connectionData.inputPort.portIndex];
            Port outPort = outputEditorNode.Ports[connectionData.outputPort.portIndex];
            
            Edge edge = inPort.ConnectTo(outPort);
            AddElement(edge);
            m_connectionDictionary.Add(edge, connectionData);
        }

        private ND_NodeEditor GetEditorNode(string nodeID) // Renamed from GetNode to avoid confusion with data Node
        {
            m_nodeDictionary.TryGetValue(nodeID, out ND_NodeEditor node);
            return node;
        }

        private void ShowSearchWindow(NodeCreationContext context)
        {
            if (m_searchProvider == null) {
                Debug.LogError("[ShowSearchWindow] m_searchProvider is null!");
                return;
            }
            // The target for SearchWindow.Open should be the GraphView or an element within it.
            // Using `this` (the GraphView) is generally fine.
            SearchWindow.Open(new SearchWindowContext(context.screenMousePosition,0,0), m_searchProvider);
            Debug.Log("[ShowSearchWindow] Search window opened.");
        }

        // Called by SearchProvider to add a new node
        public void AddNewNode(Node nodeData)
        {
            Undo.RecordObject(m_serialLizeObject.targetObject, "Added Node");
            m_BTree.nodes.Add(nodeData); // Add to data model
            // m_serialLizeObject.Update(); // Update can be deferred or done after AddNodeToGraphVisualsOnly

            AddNodeToGraphVisualsOnly(nodeData, animate: true); // Add visuals and animate
            
            Bind(); // Bind after adding
            EditorUtility.SetDirty(m_BTree);
            m_window.SetUnsavedChanges(true);
        }


        private void Bind()
        {
            if (m_serialLizeObject == null || m_serialLizeObject.targetObject == null) {
                Debug.LogWarning("[Bind] SerializedObject or its target is null. Cannot bind.");
                return;
            }
            m_serialLizeObject.Update(); // Fetch latest data
            this.Bind(m_serialLizeObject); // Bind the GraphView to the SerializedObject
            Debug.Log("[Bind] GraphView bound to SerializedObject.");
        }

        // --- Animated Deletion Handling ---
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt); 
            Vector2 mousePosition = evt.mousePosition;

            if (selection.Count > 0 && (selection.Any(s => s is ND_NodeEditor) || selection.Any(s => s is Edge)))
            {
                 evt.menu.AppendAction("Delete Animated", OnDeleteSelectedAnimated, DropdownMenuAction.AlwaysEnabled);
            }
            // Can add "Create Node" here too if you want it in context menu
            // else
            // {
            //     evt.menu.AppendAction("Create Node", (action) => ShowSearchWindow(new NodeCreationContext() { screenMousePosition = GUIUtility.GUIToScreenPoint(mousePosition), target = this }), DropdownMenuAction.AlwaysEnabled);
            // }
        }

        private void OnDeleteSelectedAnimated(DropdownMenuAction action)
        {
            List<GraphElement> elementsToDelete = new List<GraphElement>();
            foreach (var selectable in selection)
            {
                if (selectable is GraphElement ge) // ND_NodeEditor and Edge are GraphElements
                {
                    elementsToDelete.Add(ge);
                }
            }
            if (elementsToDelete.Count > 0)
            {
                AnimateAndRemoveElements(elementsToDelete);
            }
        }
        
        private void OnDeleteSelectionOperation(string operationName, AskUser askUser)
        {
            List<GraphElement> elementsToDelete = new List<GraphElement>();
            foreach (var selectable in selection.OfType<GraphElement>())
            {
                elementsToDelete.Add(selectable);
            }

            if (elementsToDelete.Count > 0)
            {
                AnimateAndRemoveElements(elementsToDelete);
                ClearSelection(); 
            }
        }

        public async void AnimateAndRemoveElements(List<GraphElement> elements)
        {
            List<ND_NodeEditor> nodesToAnimate = elements.OfType<ND_NodeEditor>().ToList();
            List<Edge> edgesToRemoveDirectly = elements.OfType<Edge>().ToList();

            Undo.RecordObject(m_serialLizeObject.targetObject, "Delete Animated Elements");

            // Start animation for nodes
            foreach (var nodeEditor in nodesToAnimate)
            {
                nodeEditor.RemoveFromClassList("appeared");
                nodeEditor.AddToClassList("disappearing");  // Add the custom disappearing state
            }

            if (nodesToAnimate.Any())
            {
                await Task.Delay(250); // Animation duration (0.25s)
            }

            // Actual removal from data model and GraphView
            foreach (var nodeEditor in nodesToAnimate)
            {
                if (nodeEditor.parent != null)
                {
                    Debug.Log("Delete Node");
                    RemoveDataForNode(nodeEditor);
                    RemoveElement(nodeEditor);
                    nodeEditor.AddToClassList("disappearing");  // Add new state to trigger its specific transition
                    await Task.Delay(250);
                }
                
            }

            foreach (var edge in edgesToRemoveDirectly)
            {
                if (edge.parent != null)
                {
                    RemoveDataForEdge(edge);  
                    RemoveElement(edge);     
                }
            }
            
            m_serialLizeObject.ApplyModifiedProperties(); // Or m_serialLizeObject.Update();
            EditorUtility.SetDirty(m_BTree);
            m_window.SetUnsavedChanges(true);
            this.MarkDirtyRepaint();
        }
    }
}