using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using NodeElements = UnityEditor.Experimental.GraphView.Node;
using System;
using System.Reflection;
using UnityEditor.UIElements;
using UnityEditor;
using UnityEngine.UIElements;

namespace ND_BehaviourTrees.Editor
{
    public class ND_NodeEditor : NodeElements
    {
        private Node m_treeNode;
        private Port m_OutputPort;
        
        private List<Port> m_Ports;
        private SerializedProperty m_serializedProperty;
        private SerializedObject m_SerializedObject;

        public Node node => m_treeNode;
        
        public List<Port> Ports => m_Ports;

        private VisualElement m_TopPortContainer;
        private VisualElement m_BottomPortContainer;
        
        public ND_NodeEditor(Node node, SerializedObject BTObject) : base(ND_BTSetting.instance.GetNodeDefaultUXMLPath())
        {
            StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/NDBT/Editor/USS/VisualElement/NodeElementUss.uss"); // Update to new USS path
            ND_BTSetting.instance.codeTest = ND_BTSetting.CodeTest.Default;
            this.styleSheets.Add(styleSheet);

            this.AddToClassList("nd-node");

            m_SerializedObject = BTObject;
            m_treeNode = node;


            m_TopPortContainer = this.Q<VisualElement>("top-port");
            m_BottomPortContainer = this.Q<VisualElement>("bottom-port");

            Type typeInfo = node.GetType();
            NodeInfoAttribute info = typeInfo.GetCustomAttribute<NodeInfoAttribute>();
            title = info.title;

            m_Ports = new List<Port>();

            string[] depthhs = info.menuItem.Split('/');
            foreach (string depth in depthhs)
            {
                this.AddToClassList(depth.ToLower().Replace(' ', '-'));
            }

            this.name = typeInfo.Name;

            if (info.HasFlowOutput)
            {
                CreateOutputPort();
            }

            if (info.HasFlowInput)
            {
                CreateInputPort();
            }

            foreach (FieldInfo property in typeInfo.GetFields())
            {
                if (property.GetCustomAttribute<ExposePropertyAttribute>() is ExposePropertyAttribute exposeProperty)
                {
                    PropertyField field = DrawProperty(property.Name);
                    //field.RegisterValueChangeCallback(OnFieldChangeCallBack);
                }
            }
            RefreshExpandedState();
            RefreshPorts();
        }

        private void OnFieldChangeCallBack(SerializedPropertyChangeEvent evt)
        {
            throw new NotImplementedException();
        }

        private void FetchSerializedProperty()
        {
            // SerializedObject = new SerializedObject(card);
            // Get the nodes
            SerializedProperty nodes = m_SerializedObject.FindProperty("m_nodes");
            if (nodes.isArray)
            {
                int size = nodes.arraySize;
                for (int i = 0; i < size; i++)
                {
                    var element = nodes.GetArrayElementAtIndex(i);
                    var elementId = element.FindPropertyRelative("m_guid");
                    if (elementId.stringValue == node.id)
                    {
                        m_serializedProperty = element;
                    }
                }
            }
        }

        private PropertyField DrawProperty(string name)
        {
            if (m_serializedProperty == null)
            {
                FetchSerializedProperty();
            }

            SerializedProperty prop = m_serializedProperty.FindPropertyRelative(name);

            PropertyField field = new PropertyField(prop);
            field.bindingPath = prop.propertyPath;
            extensionContainer.Add(field);
            return field;
        }

        

        private void CreateOutputPort()
        {
            m_OutputPort = InstantiatePort(Orientation.Vertical, Direction.Output, Port.Capacity.Single, typeof(PortType.FlowPort));
            m_OutputPort.portName = "Out";
            m_OutputPort.tooltip = "The flow OutPut";
            m_Ports.Add(m_OutputPort);
            m_BottomPortContainer.Add(m_OutputPort);
        }

        private void CreateInputPort()
        {   
            Port m_InputPort = InstantiatePort(Orientation.Vertical, Direction.Input, Port.Capacity.Single, typeof(PortType.FlowPort));
            m_InputPort.portName = "";
            m_InputPort.tooltip = "The flow input";
            m_Ports.Add(m_InputPort);
            m_TopPortContainer.Add(m_InputPort);
        }

        public void SavePosition()
        {
            m_treeNode.SetPosition(GetPosition());
        }
    }
}
