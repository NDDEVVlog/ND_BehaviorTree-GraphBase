using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;


namespace ND_BehaviourTrees.Editor
{
    public class ND_BehaviorTreesView : GraphView
    {
        private BehaviourTree m_BTree;
        private SerializedObject m_serialLizeObject;

        private ND_BehaviorTreesEditorWindow m_window;
        public ND_BehaviorTreesEditorWindow window => m_window;

        public List<ND_NodeEditor> m_treeNodes;

        public Dictionary<Edge, ND_BTConnection> m_connectionDictionary;
        public Dictionary<string, ND_NodeEditor> m_nodeDictionary;

        private ND_BehaviorTreeWindowSearchProvider m_searchProvider;

        public ND_BehaviorTreesView(SerializedObject serializedObject, ND_BehaviorTreesEditorWindow window)
        {
            m_window = window;

            m_serialLizeObject = serializedObject;
            m_BTree = (BehaviourTree)serializedObject.targetObject;

            m_treeNodes = new List<ND_NodeEditor>();
            m_nodeDictionary = new Dictionary<string, ND_NodeEditor>();
            m_connectionDictionary = new Dictionary<Edge, ND_BTConnection>();


            m_searchProvider = ScriptableObject.CreateInstance<ND_BehaviorTreeWindowSearchProvider>();
            m_searchProvider.view = this;

            this.nodeCreationRequest = ShowSearchWindow;

            StyleSheet style = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/NDBT/Editor/Resources/Styles/BehaviourTreeView/BehaviourTreeView.uss");
            styleSheets.Add(style);

            GridBackground background = new GridBackground();
            background.name = "Grid";
            Add(background);
            background.SendToBack();

            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new ClickSelector());

            DrawNodes();
            DrawConnection();

            graphViewChanged += OnGraphViewChangeEvent;
        }

        //Determine what is plug into what
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            List<Port> allPorts = new List<Port>();
            List<Port> ports = new List<Port>();

            foreach (var node in m_treeNodes)
            {
                allPorts.AddRange(node.Ports);
            }
            foreach (Port p in allPorts)
            {
                //Validate 
                if (p == startPort) continue;
                if (p.node == startPort.node) continue;
                if (startPort.direction == p.direction) continue;
                if (startPort.portType == p.portType)
                {
                    ports.Add(p);
                }
            }

            return ports;
        }

        private GraphViewChange OnGraphViewChangeEvent(GraphViewChange graphViewChange)
        {
            if (graphViewChange.movedElements != null)
            {
                Undo.RecordObject(m_serialLizeObject.targetObject, "Moved Elements");
                foreach (ND_NodeEditor editorNode in graphViewChange.movedElements.OfType<ND_NodeEditor>().ToList())
                {
                    editorNode.SavePosition();
                }

            }

            if (graphViewChange.elementsToRemove != null)
            {
                Undo.RecordObject(m_serialLizeObject.targetObject, "Removed Stuff From Graph");
                List<ND_NodeEditor> nodes = graphViewChange.elementsToRemove.OfType<ND_NodeEditor>().ToList();

                if (nodes.Count > 0)
                {

                    for (int i = nodes.Count - 1; i >= 0; i--)
                    {
                        RemoveNode(nodes[i]);
                    }
                }

                foreach (Edge edge in graphViewChange.elementsToRemove.OfType<Edge>().ToList())
                {
                    RemoveEdge(edge);
                }
            }

            if (graphViewChange.edgesToCreate != null)
            {
                Undo.RecordObject(m_serialLizeObject.targetObject, "Adding Connection");
                foreach (Edge edge in graphViewChange.edgesToCreate)
                {
                    CreateEdge(edge);
                }
            }




            return graphViewChange;
        }


        


        private void CreateEdge(Edge edge)
        {
            ND_NodeEditor inputNode = (ND_NodeEditor)edge.input.node;
            int inputIndex = inputNode.Ports.IndexOf(edge.input);

            ND_NodeEditor outputNode = (ND_NodeEditor)edge.output.node;
            int outputIndex = outputNode.Ports.IndexOf(edge.output);

            ND_BTConnection connection = new ND_BTConnection(inputNode.node.id, inputIndex, outputNode.node.id, outputIndex);
            m_BTree.connections.Add(connection);
        }

        private void RemoveEdge(Edge e)
        {
            if (m_connectionDictionary.TryGetValue(e, out ND_BTConnection connection))
            {
                m_BTree.connections.Remove(connection);
                m_connectionDictionary.Remove(e);
            }

        }

        private void RemoveNode(ND_NodeEditor editorNode)
        {

            m_BTree.nodes.Remove(editorNode.node);
            m_nodeDictionary.Remove(editorNode.node.id);
            m_treeNodes.Remove(editorNode);
            m_serialLizeObject.Update();
        }

        private void DrawNodes()
        {
            foreach (Node node in m_BTree.nodes)
            {
                AddNodeToGraph(node);
            }
            Bind();
        }

        private void DrawConnection()
        {
            if (m_BTree.connections == null) return;
            foreach (ND_BTConnection connection in m_BTree.connections)
            {
                MakeConnection(connection);
            }
        }

        private void MakeConnection(ND_BTConnection connection)
        {
            ND_NodeEditor inputNode = GetNode(connection.inputPort.nodeID);
            ND_NodeEditor outputNode = GetNode(connection.outputPort.nodeID);
            if (inputNode == null) return;
            if (outputNode == null) return;

            Port inPort = inputNode.Ports[connection.inputPort.portIndex];
            Port outPort = outputNode.Ports[connection.outputPort.portIndex];
            Edge edge = inPort.ConnectTo(outPort);
            AddElement(edge);

            m_connectionDictionary.Add(edge, connection);

        }

        private ND_NodeEditor GetNode(string nodeID)
        {
            ND_NodeEditor node = null;
            m_nodeDictionary.TryGetValue(nodeID, out node);
            return node;
        }

        private void ShowSearchWindow(NodeCreationContext obj)
        {
            m_searchProvider.target = (VisualElement)focusController.focusedElement;
            SearchWindow.Open(new SearchWindowContext(obj.screenMousePosition), m_searchProvider);
        }

        public void Add(Node node)
        {
            Undo.RecordObject(m_serialLizeObject.targetObject, "Added Node");
            m_BTree.nodes.Add(node);
            m_serialLizeObject.Update();

            AddNodeToGraph(node);
            Bind();
        }

        private void AddNodeToGraph(Node node)
        {
            node.typeName = node.GetType().AssemblyQualifiedName;

            ND_NodeEditor nodeEditor = new ND_NodeEditor(node, m_serialLizeObject);
            nodeEditor.SetPosition(node.position);
            m_treeNodes.Add(nodeEditor);
            m_nodeDictionary.Add(node.id, nodeEditor);

            AddElement(nodeEditor);
        }

        private void Bind()
        {
            m_serialLizeObject.Update();
            this.Bind(m_serialLizeObject);
        }
    }
}
