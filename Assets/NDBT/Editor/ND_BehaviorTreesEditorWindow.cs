using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using ND_BehaviourTrees;
using System.Security;
using UnityEditor.Experimental.GraphView;
using System;

namespace ND_BehaviourTrees.Editor
{
    public class ND_BehaviorTreesEditorWindow : EditorWindow
    {
        public static void Open(BehaviourTree target)
        {
            ND_BehaviorTreesEditorWindow[] windows = Resources.FindObjectsOfTypeAll<ND_BehaviorTreesEditorWindow>();
            foreach (var n in windows)
            {
                if (n.currentGraph == target)
                {
                    n.Focus();
                    return;
                }
            }
            ND_BehaviorTreesEditorWindow window = CreateWindow<ND_BehaviorTreesEditorWindow>(typeof(ND_BehaviorTreesEditorWindow), typeof(SceneView));
            window.titleContent = new GUIContent($"{target.name}", EditorGUIUtility.ObjectContent(null, typeof(BehaviourTree)).image);
            window.Load(target);
        }

        [SerializeField] private BehaviourTree m_currentGraph;
        [SerializeField] private SerializedObject m_serializeObject;
        [SerializeField] private ND_BehaviorTreesView m_currentView;

        public BehaviourTree currentGraph => m_currentGraph;

        private void OnEnable()
        {
            if (m_currentGraph != null)
            {
                DrawGraph();
            }
        }

        public void Load(BehaviourTree target)
        {   
            //LoadGraph
            m_currentGraph = target;
            DrawGraph();
            

        }
        private void DrawGraph()
        {
            m_serializeObject = new SerializedObject(m_currentGraph);
            m_currentView = new ND_BehaviorTreesView(m_serializeObject, this);
            m_currentView.graphViewChanged += OnChange;
            rootVisualElement.Add(m_currentView);

        }

        private GraphViewChange OnChange(GraphViewChange graphViewChange)
        {
            this.hasUnsavedChanges = true;
            EditorUtility.SetDirty(m_currentGraph);
            return graphViewChange;
        }

        
    }
}
