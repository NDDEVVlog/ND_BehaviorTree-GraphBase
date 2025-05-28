using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using NodeElements = UnityEditor.Experimental.GraphView.Node;
using System;
using System.Reflection;

namespace ND_BehaviourTrees.Editor
{
    public class ND_NodeEditor : NodeElements
    {
        private Node m_treeNode;
        private Port m_OutputPort;
        
        private List<Port> m_Ports;

        public Node node => m_treeNode;
        public List<Port> Ports => m_Ports;

        public ND_NodeEditor(Node node)
        {
            this.AddToClassList("nd-node");
            m_treeNode = node;
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

            if (info.HasFlowInput)
            {
                CreateInputPort();
            }
            if (info.HasFlowOutput)
            {
                CreateOutputPort();
            }
        }

        private void CreateOutputPort()
        {
            m_OutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(PortType.FlowPort));
            m_OutputPort.portName = "Out";
            m_OutputPort.tooltip = "The flow OutPut";
            m_Ports.Add(m_OutputPort);
            outputContainer.Add(m_OutputPort);
        }

        private void CreateInputPort()
        {   
            Port m_InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(PortType.FlowPort));
            m_InputPort.portName = "Idiot";
            m_InputPort.tooltip = "The flow input";
            m_Ports.Add(m_InputPort);
            inputContainer.Add(m_InputPort);
        }

        public void SavePosition()
        {
            m_treeNode.SetPosition(GetPosition());
        }
    }
}
