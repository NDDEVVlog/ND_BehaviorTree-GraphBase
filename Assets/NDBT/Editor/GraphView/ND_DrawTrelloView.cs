// File: ND_DrawTrelloView.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks; // For async/await
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements; // If you use PropertyField, etc.
using UnityEngine;
using UnityEngine.UIElements;

namespace ND_DrawTrello.Editor
{
    public partial class ND_DrawTrelloView : GraphView
    {
        // --- Fields ---
        private DrawTrello m_BTree; // The actual ScriptableObject data
        private SerializedObject m_serialLizeObject; // SerializedObject wrapper for m_BTree
        private ND_DrawTrelloEditorWindow m_editorWindow; // Reference to the hosting editor window
        public ND_DrawTrelloEditorWindow EditorWindow => m_editorWindow; // Public getter

        // Collections for managing graph elements
        public List<ND_NodeEditor> TreeNodes { get; private set; }
        public Dictionary<Edge, ND_BTConnection> ConnectionDictionary { get; private set; }
        public Dictionary<string, ND_NodeEditor> NodeDictionary { get; private set; }

        private ND_DrawTrelloSearchProvider m_searchProvider;
        private List<IContextualMenuCommand> m_contextualMenuCommands;

        // --- Constructor ---
        public ND_DrawTrelloView(SerializedObject serializedObject, ND_DrawTrelloEditorWindow editorWindow)
        {
            m_editorWindow = editorWindow;
            m_serialLizeObject = serializedObject;
            m_BTree = (DrawTrello)serializedObject.targetObject;

            // Initialize collections
            TreeNodes = new List<ND_NodeEditor>();
            NodeDictionary = new Dictionary<string, ND_NodeEditor>();
            ConnectionDictionary = new Dictionary<Edge, ND_BTConnection>();

            // Initialize search provider
            m_searchProvider = ScriptableObject.CreateInstance<ND_DrawTrelloSearchProvider>();
            if (m_searchProvider == null) Debug.LogError("[ND_DrawTrelloView.ctor] SearchProvider is NULL after CreateInstance!");
            else m_searchProvider.view = this; // Provide reference to this view

            // Assign node creation request callback
            this.nodeCreationRequest = OnNodeCreationRequest;

            // Initialize Contextual Menu Commands
            m_contextualMenuCommands = new List<IContextualMenuCommand>
            {
                new CreateNodeContextualCommand(),
                new DeleteAnimatedContextualCommand()
                // Future commands can be added here: new YourOtherContextualCommand()
            };

            SetupStylingAndBackground();
            SetupManipulators();
            
            DrawExistingGraphElementsFromData(); // Populate view from m_BTree

            SetupZoom(0.1f, 3.0f); // Min and Max zoom levels
            graphViewChanged += OnGraphViewInternalChange; // Callback for internal GraphView changes
            this.deleteSelection = OnDeleteSelectionKeyPressed; // Callback for Delete key press
        }

        // --- Setup Methods ---
        private void SetupStylingAndBackground()
        {
            // Load and add the main stylesheet for the graph view
            StyleSheet styleSheetAsset = AssetDatabase.LoadAssetAtPath<StyleSheet>(ND_DrawTrelloSetting.Instance.GetGraphViewStyleSheetPath());
            styleSheets.Add(styleSheetAsset);
            

            // Ensure the GraphView fills its container
            style.flexGrow = 1;
            style.width = Length.Percent(100);
            style.height = Length.Percent(100);

            // Add grid background
            GridBackground background = new GridBackground { name = "Grid" }; // Assign name for easier debugging
            Add(background);
            background.SendToBack(); // Ensure grid is behind other elements
        }

        private void SetupManipulators()
        {
            this.AddManipulator(new ContentDragger());   // Allows panning the graph
            this.AddManipulator(new SelectionDragger()); // Allows dragging selected elements
            this.AddManipulator(new RectangleSelector());// Allows selecting elements with a rectangle
            this.AddManipulator(new ClickSelector());    // Allows selecting elements by clicking
        }

        private void DrawExistingGraphElementsFromData()
        {
            DrawNodesFromData();
            DrawConnectionsFromData();
            BindSerializedObject(); // Bind after all elements are potentially added
        }

        // --- Public Methods for External Callers (like commands or search provider) ---

        /// <summary>
        /// Opens the search window for creating new nodes at the given screen position.
        /// </summary>
        public void OpenSearchWindow(Vector2 screenPosition)
        {
            SearchWindow.Open(new SearchWindowContext(screenPosition, 300, 200), m_searchProvider); 
        }
        
        /// <summary>
        /// Adds a new node (both data and visual representation) to the graph, typically called by the search provider.
        /// </summary>
        public void AddNewNodeFromSearch(Node nodeData)
        {
            if (nodeData == null) {
                Debug.LogError("[AddNewNodeFromSearch] nodeData is null. Cannot add node.");
                return;
            }

            
            Undo.RecordObject(m_serialLizeObject.targetObject, "Added Node: " + nodeData.GetType().Name);
            AssetDatabase.AddObjectToAsset(nodeData, m_BTree);
            m_BTree.nodes.Add(nodeData); // Add to the underlying ScriptableObject data

            AddNodeVisuals(nodeData, animate: true); // Add visual representation with animation
            
            BindSerializedObject(); // Re-bind to reflect new data for property fields
            EditorUtility.SetDirty(m_BTree); // Mark the ScriptableObject as changed
            AssetDatabase.SaveAssets();
            if (m_editorWindow != null) m_editorWindow.SetUnsavedChanges(true);
            // Debug.Log($"[AddNewNodeFromSearch] Added new node: {nodeData.id} of type {nodeData.GetType().Name}");
        }

        /// <summary>
        /// Initiates the animated removal of a list of graph elements.
        /// </summary>
        public async void InitiateAnimatedRemoveElements(List<GraphElement> elements)
        {
            if (elements == null || !elements.Any()) return;

            List<ND_NodeEditor> nodesToAnimateAndRemove = elements.OfType<ND_NodeEditor>().ToList();
            List<Edge> edgesToRemove = elements.OfType<Edge>().ToList();

            Undo.RecordObject(m_serialLizeObject.targetObject, "Delete Animated Elements");

            // Animate nodes before removing them
            await GraphViewAnimationHelper.AnimateAndPrepareForRemoval(nodesToAnimateAndRemove);

            // Actual removal from data model and GraphView's visual tree
            bool dataChanged = false;
            foreach (var nodeEditor in nodesToAnimateAndRemove)
            {
                if (nodeEditor.parent != null) // Check if it's still part of the graph
                {
                    RemoveDataForNode(nodeEditor); // Remove from your ScriptableObject data
                    RemoveElement(nodeEditor);    // Remove from GraphView's visual elements
                    dataChanged = true;
                }
            }
            foreach (var edge in edgesToRemove)
            {
                if (edge.parent != null)
                {
                    RemoveDataForEdge(edge);  // Remove from your ScriptableObject data
                    RemoveElement(edge);     // Remove from GraphView's visual elements
                    dataChanged = true;
                }
            }
            
            if (dataChanged)
            {
                m_serialLizeObject.ApplyModifiedProperties(); // Ensure SerializedObject reflects data changes
                EditorUtility.SetDirty(m_BTree);
                if (m_editorWindow != null) m_editorWindow.SetUnsavedChanges(true);
                this.MarkDirtyRepaint(); // Request repaint of the GraphView
                // Debug.Log("[InitiateAnimatedRemoveElements] Finished removing elements.");
            }
        }

        // --- Node Creation and Drawing ---
        private void OnNodeCreationRequest(NodeCreationContext context) 
        {
            OpenSearchWindow(context.screenMousePosition);
        }

        /// <summary>
        /// Adds the visual representation of a node to the graph.
        /// </summary>
        private void AddNodeVisuals(Node nodeData, bool animate = true)
        {
            if (nodeData == null) { Debug.LogError("[AddNodeVisuals] nodeData is null."); return; }
            if (string.IsNullOrEmpty(nodeData.typeName)) nodeData.typeName = nodeData.GetType().AssemblyQualifiedName; // Ensure typeName for deserialization if needed

            if (NodeDictionary.ContainsKey(nodeData.id))
            {
                Debug.LogWarning($"[AddNodeVisuals] Node editor for ID '{nodeData.id}' already exists. Skipping visual add.");
                return;
            }


            ND_NodeEditor nodeEditor; // Base type

            // --- TYPE CHECKING LOGIC ---
            if (nodeData is TrelloNode trelloNodeData) // Check if the data is a TrelloNode
            {
                // If you have ND_DrawTrelloSetting.Instance.GetTrelloUXMLPath()
                // ensure it's used by ND_TrelloNodeEditor's constructor
                nodeEditor = new ND_TrelloNodeEditor(trelloNodeData, m_serialLizeObject, this);
                Debug.Log($"Creating ND_TrelloNodeEditor for node: {nodeData.id}");
            }
            // Add more 'else if' blocks here for other specific node editor types
            else if (nodeData is TrelloChildNode trelloChild)
            {
                
                nodeEditor = new ND_TrelloChild(trelloChild, m_serialLizeObject, this);
            }
            else // Default case: use the generic ND_NodeEditor
            {
                // This uses the default UXML path defined in ND_NodeEditor's constructor
                nodeEditor = new ND_NodeEditor(nodeData, m_serialLizeObject, this);
                Debug.Log($"Creating generic ND_NodeEditor for node: {nodeData.id} of type {nodeData.GetType()}");
            }

            AddNode(nodeData, nodeEditor);

            if (animate)
            {
                // Choose animation type based on nodeData or a global setting if needed
                GraphViewAnimationHelper.AnimateNodeAppear(nodeEditor);
                // Example: GraphViewAnimationHelper.AnimateNodeAppearWithOvershoot_MultiStage(nodeEditor);
            }
            else
            {
                nodeEditor.AddToClassList("appeared"); // Make it appear instantly without animation
            }
        }


        private void DrawNodesFromData()
        {
            // Debug.Log($"[DrawNodesFromData] Attempting to draw {m_BTree.nodes.Count} nodes from data.");
            foreach (Node nodeData in m_BTree.nodes)
            {
                if (nodeData == null)
                {
                    Debug.LogWarning("[DrawNodesFromData] Encountered a null Node instance in m_BTree.nodes list.");
                    continue;
                }
                else
                {
                    Debug.Log($"<color=green>DragEnter</color> Node: <b>'{nodeData.GetType().ToString()}'</b>.");
                }
                AddNodeVisuals(nodeData, animate: false); // No animation on initial load
            }
        }

        // --- Connection Drawing & Data ---
        private void DrawConnectionsFromData()
        {
            // Debug.Log($"[DrawConnectionsFromData] Attempting to draw {m_BTree.connections.Count} connections from data.");
            foreach (ND_BTConnection connectionData in m_BTree.connections)
            {
                MakeConnectionVisualsOnly(connectionData);
            }
        }

        private void MakeConnectionVisualsOnly(ND_BTConnection connectionData)
        {
            ND_NodeEditor inputEditorNode = GetEditorNode(connectionData.inputPort.nodeID);
            ND_NodeEditor outputEditorNode = GetEditorNode(connectionData.outputPort.nodeID);

            if (inputEditorNode == null || outputEditorNode == null) {
                // Debug.LogWarning($"[MakeConnectionVisualsOnly] Could not find one or both node editors for connection. Input: {connectionData.inputPort.nodeID}, Output: {connectionData.outputPort.nodeID}");
                return;
            }
            if (connectionData.inputPort.portIndex >= inputEditorNode.Ports.Count || connectionData.outputPort.portIndex >= outputEditorNode.Ports.Count ||
                connectionData.inputPort.portIndex < 0 || connectionData.outputPort.portIndex < 0) { // Added negative check
                // Debug.LogWarning($"[MakeConnectionVisualsOnly] Port index out of bounds for connection. Input: {connectionData.inputPort.nodeID}[{connectionData.inputPort.portIndex}] (max {inputEditorNode.Ports.Count-1}), Output: {connectionData.outputPort.nodeID}[{connectionData.outputPort.portIndex}] (max {outputEditorNode.Ports.Count-1})");
                return;
            }

            Port inPort = inputEditorNode.Ports[connectionData.inputPort.portIndex];
            Port outPort = outputEditorNode.Ports[connectionData.outputPort.portIndex];
            
            Edge edge = inPort.ConnectTo(outPort);
            AddElement(edge);
            ConnectionDictionary.Add(edge, connectionData); // Track visual edge to data
        }
        
        private void CreateDataForEdge(Edge edge)
        {
            if (!(edge.input?.node is ND_NodeEditor inputNodeEditor) || !(edge.output?.node is ND_NodeEditor outputNodeEditor)) {
                Debug.LogError("[CreateDataForEdge] Edge has invalid input or output node editor. Cannot create data.");
                return;
            }
            int inputIndex = inputNodeEditor.Ports.IndexOf(edge.input);
            int outputIndex = outputNodeEditor.Ports.IndexOf(edge.output);

            if (inputIndex == -1 || outputIndex == -1) {
                Debug.LogError("[CreateDataForEdge] Could not find port index for the new edge. Cannot create data.");
                return;
            }

            ND_BTConnection connection = new ND_BTConnection(inputNodeEditor.node.id, inputIndex, outputNodeEditor.node.id, outputIndex);
            m_BTree.connections.Add(connection);
            ConnectionDictionary.Add(edge, connection); // Also track this new edge
            // Debug.Log($"[CreateDataForEdge] Created connection data: {outputNodeEditor.node.id}[{outputIndex}] -> {inputNodeEditor.node.id}[{inputIndex}]");
        }

        private void RemoveDataForEdge(Edge e)
        {
            if (ConnectionDictionary.TryGetValue(e, out ND_BTConnection connection))
            {
                m_BTree.connections.Remove(connection);
                ConnectionDictionary.Remove(e); // Stop tracking this visual edge
                // Debug.Log($"[RemoveDataForEdge] Removed connection data.");
            }
        }

        // --- Node Data Management ---
        private void RemoveDataForNode(ND_NodeEditor editorNode)
        {
            if (editorNode == null || editorNode.node == null) return;
            // Debug.Log($"[RemoveDataForNode] Removing data for node: {editorNode.node.id} ({editorNode.title})");
            m_BTree.nodes.Remove(editorNode.node); // Remove from SO data
            NodeDictionary.Remove(editorNode.node.id); // Remove from lookup
            TreeNodes.Remove(editorNode); // Remove from list of visual nodes
        }

        public ND_NodeEditor GetEditorNode(string nodeID)
        {
            NodeDictionary.TryGetValue(nodeID, out ND_NodeEditor node);
            return node;
        }
        
        // --- Event Handlers & Callbacks ---
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            List<Port> allPorts = new List<Port>();
            List<Port> compatiblePorts = new List<Port>();

            if (TreeNodes == null) return compatiblePorts;

            foreach (var nodeEditor in TreeNodes)
            {
                if (nodeEditor == null || nodeEditor.Ports == null) continue;
                allPorts.AddRange(nodeEditor.Ports);
            }

            foreach (Port p in allPorts)
            {
                if (p == startPort || p.node == startPort.node || startPort.direction == p.direction) continue;
                if (startPort.portType == p.portType) // Basic type compatibility
                {
                    compatiblePorts.Add(p);
                }
            }
            return compatiblePorts;
        }

        private GraphViewChange OnGraphViewInternalChange(GraphViewChange graphViewChange)
        {
            bool hasViewMadeChanges = false; // Flag to indicate if we've modified data based on this event

            // Handle moved elements
            if (graphViewChange.movedElements != null && graphViewChange.movedElements.Any())
            {
                Undo.RecordObject(m_serialLizeObject.targetObject, "Moved Graph Elements");
                foreach (ND_NodeEditor editorNode in graphViewChange.movedElements.OfType<ND_NodeEditor>())
                {
                    editorNode.SavePosition(); // Update node data with new visual position
                }
                hasViewMadeChanges = true;
            }

            // Handle elements removed by GraphView (e.g., edges if nodes they connect to are deleted by our custom logic)
            // Node removal should primarily be handled by OnDeleteSelectionKeyPressed/ContextualMenu for animation.
            if (graphViewChange.elementsToRemove != null && graphViewChange.elementsToRemove.Any())
            {
                List<Edge> edgesRemovedByGraphView = graphViewChange.elementsToRemove.OfType<Edge>().ToList();
                List<ND_NodeEditor> nodesRemovedByGraphView = graphViewChange.elementsToRemove.OfType<ND_NodeEditor>().ToList();

                if (edgesRemovedByGraphView.Any() || nodesRemovedByGraphView.Any())
                {
                    Undo.RecordObject(m_serialLizeObject.targetObject, "Graph Elements Removed by View");
                    foreach (Edge edge in edgesRemovedByGraphView) RemoveDataForEdge(edge);
                    foreach (ND_NodeEditor node in nodesRemovedByGraphView) RemoveDataForNode(node); // Non-animated
                    hasViewMadeChanges = true;
                }
            }

            // Handle edges created by GraphView (user dragging a connection)
            if (graphViewChange.edgesToCreate != null && graphViewChange.edgesToCreate.Any())
            {
                Undo.RecordObject(m_serialLizeObject.targetObject, "Created Graph Connections");
                foreach (Edge edge in graphViewChange.edgesToCreate)
                {
                    CreateDataForEdge(edge);
                }
                hasViewMadeChanges = true;
            }

            if (hasViewMadeChanges)
            {
                EditorUtility.SetDirty(m_BTree); // Mark ScriptableObject dirty
                if (m_editorWindow != null) m_editorWindow.SetUnsavedChanges(true); // Notify window
            }
            return graphViewChange; // Return the change back to GraphView
        }
        
        private void OnDeleteSelectionKeyPressed(string operationName, AskUser askUser)
        {
            List<GraphElement> elementsToDelete = selection.OfType<GraphElement>().ToList();
            if (elementsToDelete.Count > 0)
            {
                InitiateAnimatedRemoveElements(elementsToDelete);
                ClearSelection(); // Deselect after initiating delete
            }
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt); // Add default GraphView options (Cut, Copy, Paste, etc.)

            if (m_contextualMenuCommands != null)
            {
                foreach (var command in m_contextualMenuCommands)
                {
                    if (command.CanExecute(evt, this))
                    {
                        command.AddToMenu(evt, this);
                    }
                }
            }
        }
        
        private void BindSerializedObject()
        {
            if (m_serialLizeObject == null || m_serialLizeObject.targetObject == null) {
                Debug.LogWarning("[BindSerializedObject] SerializedObject or its target is null. Cannot bind.");
                return;
            }
            m_serialLizeObject.Update(); // Ensure it has latest data from SO before binding
            this.Bind(m_serialLizeObject); // Bind the GraphView itself to the SerializedObject for PropertyFields
            // Debug.Log("[BindSerializedObject] GraphView bound to SerializedObject.");
        }
    }
}